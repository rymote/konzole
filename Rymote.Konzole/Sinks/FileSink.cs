using Rymote.Konzole.Configuration;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks.Files;

namespace Rymote.Konzole.Sinks;

public sealed class FileSink : SinkBase<FileSinkOptions>
{
    private readonly IFileRollingStrategy _rollingStrategy;
    private readonly Func<DateTimeOffset> _clock;
    private readonly string _basePath;

    private StreamWriter? _streamWriter;
    private string _activeFilePath = string.Empty;
    private long _currentFileSize;
    private DateTimeOffset _lastFlush = DateTimeOffset.MinValue;

    public FileSink(FileSinkOptions options) : this(options, () => DateTimeOffset.UtcNow) { }

    public FileSink(FileSinkOptions options, Func<DateTimeOffset> clock) : base(options)
    {
        _clock = clock;
        _basePath = ResolveBasePath(options);
        EnsureDirectoryExists(_basePath);
        _rollingStrategy = BuildStrategy(options);
        OpenActiveWriter(_clock());
    }

    public override string Name => "File";

    protected override ILogFormatter CreateDefaultFormatter() => new JsonFormatter();

    protected override async ValueTask WriteBatchAsync(IReadOnlyList<LogEntry> batch, CancellationToken cancellationToken)
    {
        foreach (LogEntry entry in batch)
        {
            string line = Formatter.Format(entry, FormatterContext);
            int byteCount = System.Text.Encoding.UTF8.GetByteCount(line) + Environment.NewLine.Length;

            DateTimeOffset now = _clock();
            if (_rollingStrategy.ShouldRoll(_activeFilePath, _currentFileSize, byteCount, now))
            {
                await CloseWriterAsync();
                _rollingStrategy.Roll(_basePath, Options.MaxFiles, now);
                OpenActiveWriter(now);
            }

            await _streamWriter!.WriteLineAsync(line.AsMemory(), cancellationToken);
            _currentFileSize += byteCount;
        }

        if (Options.FlushInterval == TimeSpan.Zero || _clock() - _lastFlush >= Options.FlushInterval)
        {
            await _streamWriter!.FlushAsync(cancellationToken);
            _lastFlush = _clock();
        }
    }

    public override async ValueTask FlushAsync(CancellationToken cancellationToken)
    {
        await base.FlushAsync(cancellationToken);
        if (_streamWriter != null)
            await _streamWriter.FlushAsync(cancellationToken);
    }

    protected override async ValueTask DisposeResourcesAsync()
    {
        await CloseWriterAsync();
    }

    private void OpenActiveWriter(DateTimeOffset now)
    {
        _activeFilePath = _rollingStrategy.ResolveActivePath(_basePath, now);
        EnsureDirectoryExists(_activeFilePath);
        _streamWriter = new StreamWriter(_activeFilePath, append: true, System.Text.Encoding.UTF8) { AutoFlush = false };
        _currentFileSize = File.Exists(_activeFilePath) ? new FileInfo(_activeFilePath).Length : 0;
    }

    private async Task CloseWriterAsync()
    {
        if (_streamWriter == null) return;
        await _streamWriter.FlushAsync();
        await _streamWriter.DisposeAsync();
        _streamWriter = null;
    }

    private static string ResolveBasePath(FileSinkOptions options) =>
        string.IsNullOrEmpty(options.FilePath)
            ? Path.Combine(AppContext.BaseDirectory, "logs", "konzole.log")
            : options.FilePath;

    private static void EnsureDirectoryExists(string filePath)
    {
        string? directoryPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);
    }

    private static IFileRollingStrategy BuildStrategy(FileSinkOptions options) => options.RollingPolicy switch
    {
        FileRollingPolicy.DateOnly     => new DateOnlyRollingStrategy(),
        FileRollingPolicy.DateThenSize => new DateThenSizeRollingStrategy { MaxFileSize = options.MaxFileSize },
        _                              => new SizeOnlyRollingStrategy    { MaxFileSize = options.MaxFileSize }
    };
}

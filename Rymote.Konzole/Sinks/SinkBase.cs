using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Diagnostics;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Sinks;

public abstract class SinkBase<TOptions> : ISink
    where TOptions : SinkOptionsBase
{
    protected TOptions Options { get; }
    protected ILogFormatter Formatter { get; }
    protected FormatterContext FormatterContext { get; }

    private readonly Channel<LogEntry> _channel;
    private readonly Task _workerTask;
    private readonly CancellationTokenSource _shutdownTokenSource = new();
    private int _disposed;
    private int _activeBatchCount;

    protected SinkBase(TOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Formatter = options.Formatter ?? CreateDefaultFormatter();
        FormatterContext = options.BuildFormatterContext();

        _channel = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(Options.MaxQueueSize)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

        _workerTask = Task.Run(() => RunWorkerAsync(_shutdownTokenSource.Token));
    }

    public abstract string Name { get; }
    public LogLevel MinimumLevel => Options.MinimumLevel;

    protected virtual int BatchSize => 1;

    public void TryEnqueue(LogEntry entry)
    {
        if (entry.Level < Options.MinimumLevel) return;
        _channel.Writer.TryWrite(entry);
    }

    public virtual async ValueTask FlushAsync(CancellationToken cancellationToken)
    {
        while (_channel.Reader.Count > 0 || Volatile.Read(ref _activeBatchCount) > 0)
        {
            await Task.Delay(10, cancellationToken);
        }
    }

    protected abstract ILogFormatter CreateDefaultFormatter();
    protected abstract ValueTask WriteBatchAsync(IReadOnlyList<LogEntry> batch, CancellationToken cancellationToken);
    protected virtual ValueTask DisposeResourcesAsync() => ValueTask.CompletedTask;

    private async Task RunWorkerAsync(CancellationToken cancellationToken)
    {
        List<LogEntry> batchBuffer = new(BatchSize);

        try
        {
            await foreach (LogEntry entry in _channel.Reader.ReadAllAsync(cancellationToken))
            {
                batchBuffer.Add(entry);

                while (batchBuffer.Count < BatchSize && _channel.Reader.TryRead(out LogEntry? next))
                {
                    batchBuffer.Add(next);
                }

                Interlocked.Increment(ref _activeBatchCount);
                try
                {
                    await WriteBatchAsync(batchBuffer, cancellationToken);
                }
                catch (Exception writeException) when (writeException is not OperationCanceledException)
                {
                    KonzoleDiagnostics.ReportSinkError(
                        new SinkErrorEventArgs(Name, "Sink write failed", writeException, batchBuffer.Count));
                }
                finally
                {
                    Interlocked.Decrement(ref _activeBatchCount);
                }

                batchBuffer.Clear();
            }
        }
        catch (OperationCanceledException) { }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        _channel.Writer.TryComplete();

        try
        {
            await _workerTask.WaitAsync(Options.ShutdownTimeout);
        }
        catch (TimeoutException)
        {
            _shutdownTokenSource.Cancel();
            try { await _workerTask; } catch { }
        }

        await DisposeResourcesAsync();
        _shutdownTokenSource.Dispose();
    }

    void IDisposable.Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();
}

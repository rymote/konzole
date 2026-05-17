using System.Collections.Concurrent;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks;

namespace Rymote.Konzole.Tests.Infrastructure;

public sealed class FakeSink : SinkBase<FakeSinkOptions>
{
    private readonly ConcurrentQueue<LogEntry> _capturedEntries = new();

    public FakeSink(FakeSinkOptions options) : base(options) { }

    public override string Name => "Fake";

    public IReadOnlyList<LogEntry> CapturedEntries => _capturedEntries.ToArray();

    protected override ILogFormatter CreateDefaultFormatter() => new ConsoleFormatter(useEmojis: false);

    protected override async ValueTask WriteBatchAsync(
        IReadOnlyList<LogEntry> batch,
        CancellationToken cancellationToken)
    {
        if (Options.WriteDelay > TimeSpan.Zero)
            await Task.Delay(Options.WriteDelay, cancellationToken);

        if (Options.ThrowOnWrite)
            throw new InvalidOperationException("FakeSink configured to throw.");

        foreach (LogEntry entry in batch)
            _capturedEntries.Enqueue(entry);
    }
}

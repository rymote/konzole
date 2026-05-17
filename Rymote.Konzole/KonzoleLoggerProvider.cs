using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Sinks;

namespace Rymote.Konzole;

[ProviderAlias("Konzole")]
public sealed class KonzoleLoggerProvider : ILoggerProvider, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, KonzoleLogger> _loggers = new();
    private readonly IReadOnlyList<ISink> _sinks;
    private int _disposed;

    public KonzoleLoggerProvider(IEnumerable<ISink> sinks)
    {
        ArgumentNullException.ThrowIfNull(sinks);
        _sinks = sinks.ToArray();
        if (_sinks.Count == 0)
            throw new ArgumentException("At least one sink must be provided.", nameof(sinks));
    }

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, name => new KonzoleLogger(name, _sinks));

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        foreach (ISink sink in _sinks)
        {
            try { await sink.FlushAsync(CancellationToken.None); } catch { }
        }

        foreach (ISink sink in _sinks)
        {
            try { await sink.DisposeAsync(); } catch { }
        }

        _loggers.Clear();
    }

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();
}

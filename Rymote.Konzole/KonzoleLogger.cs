using Microsoft.Extensions.Logging;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks;

namespace Rymote.Konzole;

public sealed class KonzoleLogger : ILogger
{
    private readonly string _categoryName;
    private readonly IReadOnlyList<ISink> _sinks;

    public KonzoleLogger(string categoryName, IReadOnlyList<ISink> sinks)
    {
        _categoryName = categoryName;
        _sinks = sinks;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        if (state is KonzoleScopeState scopeState) return KonzoleScopeState.Push(scopeState);

        KonzoleScopeState? currentScope = KonzoleScopeState.Current;
        KonzoleScopeState wrappedScope = new()
        {
            Tag = currentScope?.Tag,
            TraceId = currentScope?.TraceId,
            SpanId = currentScope?.SpanId
        };
        return KonzoleScopeState.Push(wrappedScope);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        foreach (ISink sink in _sinks)
        {
            if (logLevel >= sink.MinimumLevel) return true;
        }
        return false;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        string message = formatter(state, exception);
        if (string.IsNullOrEmpty(message) && exception == null) return;

        KonzoleScopeState? currentScope = KonzoleScopeState.Current;

        LogEntry logEntry = new()
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = logLevel,
            Tag = currentScope?.Tag,
            Message = message,
            Category = _categoryName,
            EventId = eventId,
            Exception = exception,
            Properties = ExtractProperties(state),
            Scope = currentScope?.Tag?.ToString(),
            TraceId = currentScope?.TraceId,
            SpanId = currentScope?.SpanId
        };

        foreach (ISink sink in _sinks)
        {
            sink.TryEnqueue(logEntry);
        }
    }

    private static IReadOnlyDictionary<string, object?>? ExtractProperties<TState>(TState state)
    {
        if (state is not IReadOnlyList<KeyValuePair<string, object?>> structuredState) return null;

        Dictionary<string, object?> extracted = new(structuredState.Count);
        foreach (KeyValuePair<string, object?> pair in structuredState)
        {
            if (pair.Key == "{OriginalFormat}") continue;
            extracted[pair.Key] = pair.Value;
        }
        return extracted.Count == 0 ? null : extracted;
    }
}

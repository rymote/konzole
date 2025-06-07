using Microsoft.Extensions.Logging;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks;

namespace Rymote.Konzole;

public class KonzoleLogger : ILogger
{
    private readonly string _categoryName;
    private readonly List<ISink> _sinks;
    
    public KonzoleLogger(string categoryName, List<ISink> sinks)
    {
        _categoryName = categoryName;
        _sinks = sinks;
    }
    
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return new LogScope(state?.ToString());
    }
    
    public bool IsEnabled(LogLevel logLevel)
    {
        return _sinks.Any(sink => sink is ISink);
    }
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;
            
        string message = formatter(state, exception);
        if (string.IsNullOrEmpty(message) && exception == null)
            return;
            
        LogEntry entry = new LogEntry
        {
            Level = MapLogLevel(logLevel, eventId),
            Message = message,
            Category = _categoryName,
            EventId = eventId.Id != 0 ? eventId.Id : null,
            EventName = eventId.Name,
            Exception = exception,
            Timestamp = DateTime.UtcNow
        };
        
        if (state is IEnumerable<KeyValuePair<string, object>> properties)
        {
            entry.Properties = new Dictionary<string, object?>();
            foreach (KeyValuePair<string, object> property in properties)
            {
                if (property.Key != "{OriginalFormat}")
                {
                    entry.Properties[property.Key] = property.Value;
                }
            }
        }
        
        if (LogScope.Current != null)
        {
            entry.Scope = LogScope.Current;
        }
        
        Task[] tasks = _sinks.Select(sink => sink.WriteAsync(entry)).ToArray();
        Task.WaitAll(tasks);
    }
    
    private static KonzoleLogLevel MapLogLevel(LogLevel logLevel, EventId eventId)
    {
        return eventId.Id switch
        {
            1000 => KonzoleLogLevel.Success,
            1001 => KonzoleLogLevel.Fatal,
            1002 => KonzoleLogLevel.Pending,
            1003 => KonzoleLogLevel.Complete,
            1004 => KonzoleLogLevel.Note,
            1005 => KonzoleLogLevel.Start,
            1006 => KonzoleLogLevel.Pause,
            1007 => KonzoleLogLevel.Watch,
            _ => logLevel switch
            {
                LogLevel.Trace => KonzoleLogLevel.Trace,
                LogLevel.Debug => KonzoleLogLevel.Debug,
                LogLevel.Information => KonzoleLogLevel.Information,
                LogLevel.Warning => KonzoleLogLevel.Warning,
                LogLevel.Error => KonzoleLogLevel.Error,
                LogLevel.Critical => KonzoleLogLevel.Fatal,
                _ => KonzoleLogLevel.Information
            }
        };
    }
    
    private class LogScope : IDisposable
    {
        private readonly string? _state;
        private readonly LogScope? _parent;
        
        [ThreadStatic]
        private static LogScope? _current;
        
        public static string? Current => _current?._state;
        
        public LogScope(string? state)
        {
            _state = state;
            _parent = _current;
            _current = this;
        }
        
        public void Dispose()
        {
            _current = _parent;
        }
    }
} 
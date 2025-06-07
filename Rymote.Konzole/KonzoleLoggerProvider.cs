using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Sinks;

namespace Rymote.Konzole;

[ProviderAlias("Konzole")]
public class KonzoleLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, KonzoleLogger> _loggers = new();
    private readonly List<ISink> _sinks;
    
    public KonzoleLoggerProvider(IEnumerable<ISink> sinks)
    {
        _sinks = sinks?.ToList() ?? throw new ArgumentNullException(nameof(sinks));
        
        if (!_sinks.Any())
        {
            throw new ArgumentException("At least one sink must be provided.", nameof(sinks));
        }
    }
    
    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new KonzoleLogger(name, _sinks));
    }
    
    public void Dispose()
    {
        foreach (ISink sink in _sinks)
        {
            sink.FlushAsync().Wait();
            sink.Dispose();
        }
        
        _sinks.Clear();
        _loggers.Clear();
    }
} 
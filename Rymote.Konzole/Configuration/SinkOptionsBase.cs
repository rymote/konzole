using Microsoft.Extensions.Logging;
using Rymote.Konzole.Formatters;

namespace Rymote.Konzole.Configuration;

public abstract class SinkOptionsBase
{
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
    
    public bool ShowTimestamp { get; set; } = true;
    
    public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";
    
    public bool ShowCategory { get; set; } = true;
    
    public bool ShowEventId { get; set; } = false;
    
    public bool ShowScope { get; set; } = true;
    
    public bool ShowException { get; set; } = true;
    
    public ILogFormatter? Formatter { get; set; }
} 
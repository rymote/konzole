using Microsoft.Extensions.Logging;
using Rymote.Konzole.Formatters;

namespace Rymote.Konzole.Configuration;

public abstract class SinkOptionsBase
{
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    public bool ShowTimestamp { get; set; } = true;
    public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";
    public bool ShowCategory { get; set; } = true;
    public bool ShowEventId { get; set; }
    public bool ShowScope { get; set; } = true;
    public bool ShowException { get; set; } = true;
    public int MaxMessageLength { get; set; } = 4000;

    public int MaxQueueSize { get; set; } = 10_000;
    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(5);

    public ILogFormatter? Formatter { get; set; }

    public FormatterContext BuildFormatterContext() => new()
    {
        ShowTimestamp = ShowTimestamp,
        TimestampFormat = TimestampFormat,
        ShowCategory = ShowCategory,
        ShowEventId = ShowEventId,
        ShowScope = ShowScope,
        ShowException = ShowException,
        MaxMessageLength = MaxMessageLength
    };
}

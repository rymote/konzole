namespace Rymote.Konzole.Formatters;

public sealed class FormatterContext
{
    public bool ShowTimestamp { get; init; } = true;
    public string TimestampFormat { get; init; } = "yyyy-MM-dd HH:mm:ss.fff";
    public bool ShowCategory { get; init; } = true;
    public bool ShowEventId { get; init; }
    public bool ShowScope { get; init; } = true;
    public bool ShowException { get; init; } = true;
    public int MaxMessageLength { get; init; } = 4000;
}

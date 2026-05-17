using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Rymote.Konzole.Models;

public sealed record LogEntry
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public LogLevel Level { get; init; }
    public KonzoleTag? Tag { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? Category { get; init; }
    public EventId EventId { get; init; }

    [JsonIgnore]
    public Exception? Exception { get; init; }

    public IReadOnlyDictionary<string, object?>? Properties { get; init; }
    public string? Scope { get; init; }
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
}

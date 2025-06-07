using System.Text.Json.Serialization;

namespace Rymote.Konzole.Models;

public class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public KonzoleLogLevel Level { get; set; }
    
    public string Message { get; set; } = string.Empty;
    
    public string? Category { get; set; }
    
    public int? EventId { get; set; }
    
    public string? EventName { get; set; }
    
    public Exception? Exception { get; set; }
    
    public Dictionary<string, object?>? Properties { get; set; }
    
    public string? Scope { get; set; }
    
    [JsonIgnore]
    public ConsoleColor? Color { get; set; }
    
    public string? TraceId { get; set; }
    
    public string? SpanId { get; set; }
} 
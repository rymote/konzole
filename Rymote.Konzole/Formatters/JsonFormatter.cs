using System.Text.Json;
using System.Text.Json.Serialization;
using Rymote.Konzole.Formatters.Json;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Formatters;

public sealed class JsonFormatter : ILogFormatter
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public JsonFormatter()
    {
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                new ExceptionJsonConverter()
            }
        };
    }

    public string Format(LogEntry entry, FormatterContext context)
    {
        Dictionary<string, object?> document = new()
        {
            ["timestamp"] = context.ShowTimestamp ? entry.Timestamp.ToString(context.TimestampFormat) : null,
            ["level"] = entry.Level.ToString(),
            ["tag"] = entry.Tag?.ToString(),
            ["message"] = entry.Message,
            ["category"] = context.ShowCategory ? entry.Category : null,
            ["eventId"] = context.ShowEventId && entry.EventId.Id != 0 ? entry.EventId.Id : (int?)null,
            ["eventName"] = context.ShowEventId ? entry.EventId.Name : null,
            ["exception"] = context.ShowException ? entry.Exception : null,
            ["properties"] = entry.Properties,
            ["scope"] = context.ShowScope ? entry.Scope : null,
            ["traceId"] = entry.TraceId,
            ["spanId"] = entry.SpanId
        };

        return JsonSerializer.Serialize(document, _jsonSerializerOptions);
    }
}

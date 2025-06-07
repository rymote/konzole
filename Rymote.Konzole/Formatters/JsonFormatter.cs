using System.Text.Json;
using System.Text.Json.Serialization;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Formatters;

public class JsonFormatter : FormatterBase
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    
    public JsonFormatter(SinkOptionsBase options) : base(options)
    {
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = 
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                new ExceptionJsonConverter()
            }
        };
    }
    
    public override string Format(LogEntry entry)
    {
        object jsonEntry = new
        {
            timestamp = Options.ShowTimestamp ? entry.Timestamp.ToString(Options.TimestampFormat) : null,
            level = entry.Level.ToString(),
            message = entry.Message,
            category = Options.ShowCategory ? entry.Category : null,
            eventId = Options.ShowEventId ? entry.EventId : null,
            eventName = Options.ShowEventId ? entry.EventName : null,
            exception = Options.ShowException && entry.Exception != null ? new
            {
                type = entry.Exception.GetType().FullName,
                message = entry.Exception.Message,
                stackTrace = entry.Exception.StackTrace,
                innerException = entry.Exception.InnerException?.Message
            } : null,
            properties = entry.Properties,
            scope = Options.ShowScope ? entry.Scope : null,
            traceId = entry.TraceId,
            spanId = entry.SpanId
        };
        
        return JsonSerializer.Serialize(jsonEntry, _jsonSerializerOptions);
    }
    
    private class ExceptionJsonConverter : JsonConverter<Exception>
    {
        public override Exception? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
        
        public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("type", value.GetType().FullName);
            writer.WriteString("message", value.Message);
            writer.WriteString("stackTrace", value.StackTrace);
            
            if (value.InnerException != null)
            {
                writer.WritePropertyName("innerException");
                Write(writer, value.InnerException, options);
            }
            
            writer.WriteEndObject();
        }
    }
} 
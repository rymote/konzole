using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rymote.Konzole.Formatters.Json;

public sealed class ExceptionJsonConverter : JsonConverter<Exception>
{
    public override Exception? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException("ExceptionJsonConverter is write-only.");

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

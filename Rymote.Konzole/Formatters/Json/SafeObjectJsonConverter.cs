using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rymote.Konzole.Formatters.Json;

/// <summary>
/// Defensive <see cref="JsonConverter{T}"/> for the abstract <see cref="object"/> type so the
/// <see cref="JsonFormatter"/> can serialize the heterogeneous <c>Properties</c> dictionary
/// without crashing on values whose runtime type <c>System.Text.Json</c> refuses (e.g.
/// reflection types like <c>RuntimeMethodInfo</c>, <c>Type</c>, <c>MemberInfo</c>) or that
/// would otherwise blow up the whole log entry.
/// </summary>
/// <remarks>
/// ASP.NET Core's routing / diagnostics middleware enriches log entries with the matched
/// endpoint's <c>Metadata</c> collection, which holds reflection-backed objects. The default
/// JSON serializer rejects those for security reasons and the entire log entry is then
/// dropped with a "Sink write failed" notice. This converter intercepts each property value,
/// blocks the known-bad reflection types up front, and falls back to a string placeholder if
/// the inner serializer still throws.
/// </remarks>
public sealed class SafeObjectJsonConverter : JsonConverter<object>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException("SafeObjectJsonConverter is write-only.");

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        Type runtimeType = value.GetType();
        if (IsKnownUnserializable(runtimeType))
        {
            writer.WriteStringValue($"<unserializable: {runtimeType.FullName ?? runtimeType.Name}>");
            return;
        }

        try
        {
            JsonSerializer.Serialize(writer, value, runtimeType, options);
        }
        catch (NotSupportedException)
        {
            writer.WriteStringValue($"<unserializable: {runtimeType.FullName ?? runtimeType.Name}>");
        }
        catch (JsonException)
        {
            writer.WriteStringValue($"<unserializable: {runtimeType.FullName ?? runtimeType.Name}>");
        }
    }

    private static bool IsKnownUnserializable(Type type)
    {
        if (typeof(MemberInfo).IsAssignableFrom(type)) return true;
        if (typeof(Delegate).IsAssignableFrom(type)) return true;
        if (typeof(Assembly).IsAssignableFrom(type)) return true;
        if (typeof(Module).IsAssignableFrom(type)) return true;
        return false;
    }
}

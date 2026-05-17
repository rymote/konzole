using System.Text;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Formatters;

public sealed class ConsoleFormatter : ILogFormatter
{
    private readonly bool _useEmojis;

    public ConsoleFormatter(bool useEmojis = true)
    {
        _useEmojis = useEmojis;
    }

    public string Format(LogEntry entry, FormatterContext context)
    {
        StringBuilder stringBuilder = new();

        bool consoleSupportsUtf8 = Console.OutputEncoding.CodePage == 65001;
        bool renderEmoji = _useEmojis && consoleSupportsUtf8;

        if (renderEmoji)
        {
            string emoji = entry.Tag.HasValue
                ? LogIcon.GetIcon(entry.Tag.Value)
                : LogIcon.GetIcon(entry.Level);
            stringBuilder.Append(emoji);
            stringBuilder.Append("  ");
        }
        else
        {
            string fallback = entry.Tag.HasValue
                ? LogIcon.GetFallbackIcon(entry.Tag.Value)
                : LogIcon.GetFallbackIcon(entry.Level);
            stringBuilder.Append(fallback);
            stringBuilder.Append(' ');
        }

        FormatterHelpers.AppendTimestamp(stringBuilder, entry, context);
        FormatterHelpers.AppendCategory(stringBuilder, entry, context);
        FormatterHelpers.AppendEventId(stringBuilder, entry, context);
        FormatterHelpers.AppendScope(stringBuilder, entry, context);

        stringBuilder.Append(FormatterHelpers.TruncateMessage(entry.Message, context.MaxMessageLength));

        if (entry.Properties is { Count: > 0 })
        {
            stringBuilder.Append(" (");
            bool isFirst = true;
            foreach (KeyValuePair<string, object?> property in entry.Properties)
            {
                if (!isFirst) stringBuilder.Append(", ");
                stringBuilder.Append(property.Key);
                stringBuilder.Append(": ");
                stringBuilder.Append(property.Value?.ToString() ?? "null");
                isFirst = false;
            }
            stringBuilder.Append(')');
        }

        FormatterHelpers.AppendException(stringBuilder, entry, context);

        return stringBuilder.ToString();
    }
}

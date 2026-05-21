using System.Text;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Formatters;

public sealed class SlackFormatter : ILogFormatter
{
    public string Format(LogEntry entry, FormatterContext context)
    {
        StringBuilder stringBuilder = new();

        string icon = entry.Tag.HasValue
            ? LogIcon.GetIcon(entry.Tag.Value)
            : LogIcon.GetIcon(entry.Level);
        stringBuilder.Append(icon);

        stringBuilder.Append(" *");
        stringBuilder.Append(entry.Tag?.ToString() ?? entry.Level.ToString());
        stringBuilder.Append('*');

        if (context.ShowTimestamp)
        {
            stringBuilder.Append(" `");
            stringBuilder.Append(entry.Timestamp.ToString(context.TimestampFormat));
            stringBuilder.Append('`');
        }

        if (context.ShowCategory && !string.IsNullOrEmpty(entry.Category))
        {
            stringBuilder.Append(" [");
            stringBuilder.Append(entry.Category);
            stringBuilder.Append(']');
        }

        stringBuilder.Append('\n');
        string messagePlain = FormatterHelpers.StripStyles(entry.Message);
        stringBuilder.Append(FormatterHelpers.TruncateMessage(messagePlain, context.MaxMessageLength));

        return stringBuilder.ToString();
    }
}

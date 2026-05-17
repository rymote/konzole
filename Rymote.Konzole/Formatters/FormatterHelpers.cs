using System.Text;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Formatters;

internal static class FormatterHelpers
{
    private const int CategoryMaxLength = 30;

    public static void AppendTimestamp(StringBuilder stringBuilder, LogEntry entry, FormatterContext context)
    {
        if (!context.ShowTimestamp) return;
        stringBuilder.Append('[');
        stringBuilder.Append(entry.Timestamp.ToString(context.TimestampFormat));
        stringBuilder.Append("] ");
    }

    public static void AppendCategory(StringBuilder stringBuilder, LogEntry entry, FormatterContext context)
    {
        if (!context.ShowCategory || string.IsNullOrEmpty(entry.Category)) return;
        stringBuilder.Append('[');
        stringBuilder.Append(TruncateCategory(entry.Category));
        stringBuilder.Append("] ");
    }

    public static void AppendEventId(StringBuilder stringBuilder, LogEntry entry, FormatterContext context)
    {
        if (!context.ShowEventId || entry.EventId.Id == 0) return;
        stringBuilder.Append('[');
        stringBuilder.Append(entry.EventId.Id);
        if (!string.IsNullOrEmpty(entry.EventId.Name))
        {
            stringBuilder.Append(':');
            stringBuilder.Append(entry.EventId.Name);
        }
        stringBuilder.Append("] ");
    }

    public static void AppendScope(StringBuilder stringBuilder, LogEntry entry, FormatterContext context)
    {
        if (!context.ShowScope || string.IsNullOrEmpty(entry.Scope)) return;
        stringBuilder.Append("=> ");
        stringBuilder.Append(entry.Scope);
        stringBuilder.Append(' ');
    }

    public static void AppendException(StringBuilder stringBuilder, LogEntry entry, FormatterContext context)
    {
        if (!context.ShowException || entry.Exception == null) return;
        stringBuilder.AppendLine();
        stringBuilder.Append("    Exception: ");
        stringBuilder.AppendLine(entry.Exception.GetType().Name);
        stringBuilder.Append("    Message: ");
        stringBuilder.AppendLine(entry.Exception.Message);
        if (string.IsNullOrEmpty(entry.Exception.StackTrace)) return;
        stringBuilder.AppendLine("    Stack Trace:");
        foreach (string line in entry.Exception.StackTrace.Split('\n'))
        {
            stringBuilder.Append("      ");
            stringBuilder.AppendLine(line.Trim());
        }
    }

    public static string TruncateMessage(string message, int maxLength) =>
        message.Length <= maxLength ? message : message.Substring(0, maxLength - 3) + "...";

    private static string TruncateCategory(string category)
    {
        if (category.Length <= CategoryMaxLength) return category;

        string[] parts = category.Split('.');
        if (parts.Length == 1) return category.Substring(0, CategoryMaxLength - 3) + "...";

        StringBuilder builder = new();
        for (int index = 0; index < parts.Length - 1; index++)
        {
            builder.Append(parts[index][0]);
            builder.Append('.');
        }
        builder.Append(parts[^1]);

        return builder.Length > CategoryMaxLength
            ? builder.ToString(0, CategoryMaxLength - 3) + "..."
            : builder.ToString();
    }
}

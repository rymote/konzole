using System.Text;
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Formatters;

public sealed class ConsoleFormatter : ILogFormatter
{
    private readonly bool _useEmojis;
    private readonly bool _showIcon;
    private readonly bool _showLevelLabel;

    public ConsoleFormatter(bool useEmojis = true, bool showIcon = true, bool showLevelLabel = true)
    {
        _useEmojis = useEmojis;
        _showIcon = showIcon;
        _showLevelLabel = showLevelLabel;
    }

    public string Format(LogEntry entry, FormatterContext context)
    {
        StringBuilder stringBuilder = new();

        bool consoleSupportsUtf8 = Console.OutputEncoding.CodePage == 65001;
        bool emojiRenderable = _useEmojis && consoleSupportsUtf8;

        if (_showIcon)
        {
            if (emojiRenderable)
            {
                stringBuilder.Append("[icon]");
                stringBuilder.Append(entry.Tag.HasValue
                    ? LogIcon.GetIcon(entry.Tag.Value)
                    : LogIcon.GetIcon(entry.Level));
                stringBuilder.Append("[/]  ");
            }
            else if (!_showLevelLabel)
            {
                stringBuilder.Append("[icon]");
                stringBuilder.Append(entry.Tag.HasValue
                    ? LogIcon.GetFallbackIcon(entry.Tag.Value)
                    : LogIcon.GetFallbackIcon(entry.Level));
                stringBuilder.Append("[/] ");
            }
        }

        if (_showLevelLabel)
        {
            string labelText = entry.Tag.HasValue
                ? LogIcon.GetFallbackIcon(entry.Tag.Value)
                : LogIcon.GetFallbackIcon(entry.Level);
            stringBuilder.Append("[level-label]");
            stringBuilder.Append(labelText);
            stringBuilder.Append("[/] ");
        }

        AppendSegmentedTimestamp(stringBuilder, entry, context);
        AppendSegmentedCategory(stringBuilder, entry, context);
        AppendSegmentedEventId(stringBuilder, entry, context);
        AppendSegmentedScope(stringBuilder, entry, context);

        string messageSegment = entry.Level switch
        {
            LogLevel.Warning  => "message-warning",
            LogLevel.Error    => "message-error",
            LogLevel.Critical => "message-error",
            _                 => "message"
        };
        stringBuilder.Append('[').Append(messageSegment).Append(']');
        stringBuilder.Append(FormatterHelpers.TruncateMessage(entry.Message, context.MaxMessageLength));
        stringBuilder.Append("[/]");

        if (entry.Properties is { Count: > 0 })
        {
            stringBuilder.Append(' ').Append('(');
            bool isFirst = true;
            foreach (KeyValuePair<string, object?> property in entry.Properties)
            {
                if (!isFirst) stringBuilder.Append(", ");
                stringBuilder.Append("[property-key]").Append(property.Key).Append("[/]");
                stringBuilder.Append(": ");
                stringBuilder.Append("[property-value]").Append(property.Value?.ToString() ?? "null").Append("[/]");
                isFirst = false;
            }
            stringBuilder.Append(')');
        }

        AppendSegmentedException(stringBuilder, entry, context);

        return stringBuilder.ToString();
    }

    private static void AppendSegmentedTimestamp(StringBuilder builder, LogEntry entry, FormatterContext context)
    {
        if (!context.ShowTimestamp) return;
        builder.Append("[timestamp][");
        builder.Append(entry.Timestamp.ToString(context.TimestampFormat));
        builder.Append("][/] ");
    }

    private static void AppendSegmentedCategory(StringBuilder builder, LogEntry entry, FormatterContext context)
    {
        if (!context.ShowCategory || string.IsNullOrEmpty(entry.Category)) return;
        builder.Append("[category][");
        builder.Append(TruncateCategory(entry.Category));
        builder.Append("][/] ");
    }

    private static void AppendSegmentedEventId(StringBuilder builder, LogEntry entry, FormatterContext context)
    {
        if (!context.ShowEventId || entry.EventId.Id == 0) return;
        builder.Append("[event-id][");
        builder.Append(entry.EventId.Id);
        if (!string.IsNullOrEmpty(entry.EventId.Name))
        {
            builder.Append(':');
            builder.Append(entry.EventId.Name);
        }
        builder.Append("][/] ");
    }

    private static void AppendSegmentedScope(StringBuilder builder, LogEntry entry, FormatterContext context)
    {
        if (!context.ShowScope || string.IsNullOrEmpty(entry.Scope)) return;
        builder.Append("[scope]=> ");
        builder.Append(entry.Scope);
        builder.Append("[/] ");
    }

    private static void AppendSegmentedException(StringBuilder builder, LogEntry entry, FormatterContext context)
    {
        if (!context.ShowException || entry.Exception == null) return;
        builder.AppendLine();
        builder.Append("    [exception-label]Exception:[/] ");
        builder.Append("[exception-message]").Append(entry.Exception.GetType().Name).Append("[/]");
        builder.AppendLine();
        builder.Append("    [exception-label]Message:[/] ");
        builder.Append("[exception-message]").Append(entry.Exception.Message).Append("[/]");
        if (string.IsNullOrEmpty(entry.Exception.StackTrace)) return;
        builder.AppendLine();
        builder.Append("    [exception-label]Stack Trace:[/]");
        foreach (string line in entry.Exception.StackTrace.Split('\n'))
        {
            builder.AppendLine();
            builder.Append("      [exception-stack]").Append(line.Trim()).Append("[/]");
        }
    }

    private static string TruncateCategory(string category)
    {
        const int maximumLength = 30;
        if (category.Length <= maximumLength) return category;

        string[] parts = category.Split('.');
        if (parts.Length == 1) return category.Substring(0, maximumLength - 3) + "...";

        StringBuilder builder = new();
        for (int index = 0; index < parts.Length - 1; index++)
        {
            builder.Append(parts[index][0]);
            builder.Append('.');
        }
        builder.Append(parts[^1]);

        return builder.Length > maximumLength
            ? builder.ToString(0, maximumLength - 3) + "..."
            : builder.ToString();
    }
}

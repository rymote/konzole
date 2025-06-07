using System.Text;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Formatters;

public class ConsoleFormatter : FormatterBase
{
    private readonly ConsoleSinkOptions _consoleSinkOptions;
    
    public ConsoleFormatter(ConsoleSinkOptions options) : base(options)
    {
        _consoleSinkOptions = options;
    }
    
    public override string Format(LogEntry entry)
    {
        StringBuilder stringBuilder = new StringBuilder();
        
        if (_consoleSinkOptions.UseEmojis && Console.OutputEncoding.CodePage == 65001) // UTF-8
        {
            string emoji = LogIcon.GetIcon(entry.Level);
            stringBuilder.Append(emoji);
            
            int spacesToAdd = 2;
            for (int i = 0; i < spacesToAdd; i++)
            {
                stringBuilder.Append(' ');
            }
        }
        else
        {
            stringBuilder.Append(LogIcon.GetFallbackIcon(entry.Level));
            stringBuilder.Append(' ');
        }
        
        AppendTimestamp(stringBuilder, entry);
        AppendCategory(stringBuilder, entry);
        AppendEventId(stringBuilder, entry);
        AppendScope(stringBuilder, entry);
        
        string message = entry.Message;
        if (message.Length > _consoleSinkOptions.MaxMessageLength)
        {
            message = message.Substring(0, _consoleSinkOptions.MaxMessageLength - 3) + "...";
        }
        stringBuilder.Append(message);
        
        if (entry.Properties?.Count > 0)
        {
            stringBuilder.Append(" (");
            bool isFirst = true;
            foreach (KeyValuePair<string, object?> property in entry.Properties)
            {
                if (!isFirst)
                {
                    stringBuilder.Append(", ");
                }
                stringBuilder.Append(property.Key);
                stringBuilder.Append(": ");
                stringBuilder.Append(property.Value?.ToString() ?? "null");
                isFirst = false;
            }
            stringBuilder.Append(')');
        }
        
        AppendException(stringBuilder, entry);
        
        return stringBuilder.ToString();
    }
} 
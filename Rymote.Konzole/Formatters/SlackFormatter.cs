using System.Text;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Formatters;

public class SlackFormatter : FormatterBase
{
    public SlackFormatter(SinkOptionsBase options) : base(options) {}
    
    public override string Format(LogEntry entry)
    {
        StringBuilder stringBuilder = new StringBuilder();
        
        stringBuilder.Append(LogIcon.GetIcon(entry.Level));
        stringBuilder.Append(" *");
        stringBuilder.Append(entry.Level);
        stringBuilder.Append("*");
        
        if (Options.ShowTimestamp)
        {
            stringBuilder.Append(" `");
            stringBuilder.Append(entry.Timestamp.ToString(Options.TimestampFormat));
            stringBuilder.Append("`");
        }
        
        if (Options.ShowCategory && !string.IsNullOrEmpty(entry.Category))
        {
            stringBuilder.Append(" [");
            stringBuilder.Append(entry.Category);
            stringBuilder.Append("]");
        }
        
        stringBuilder.Append("\n");
        stringBuilder.Append(entry.Message);
        
        return stringBuilder.ToString();
    }
}
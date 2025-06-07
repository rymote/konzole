using System.Text;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Formatters;

public abstract class FormatterBase : ILogFormatter
{
    protected readonly SinkOptionsBase Options;
    
    protected FormatterBase(SinkOptionsBase options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }
    
    public abstract string Format(LogEntry entry);
    
    protected void AppendTimestamp(StringBuilder stringBuilder, LogEntry entry)
    {
        if (Options.ShowTimestamp)
        {
            stringBuilder.Append('[');
            stringBuilder.Append(entry.Timestamp.ToString(Options.TimestampFormat));
            stringBuilder.Append("] ");
        }
    }
    
    protected void AppendCategory(StringBuilder stringBuilder, LogEntry entry)
    {
        if (Options.ShowCategory && !string.IsNullOrEmpty(entry.Category))
        {
            stringBuilder.Append('[');
            stringBuilder.Append(TruncateCategory(entry.Category));
            stringBuilder.Append("] ");
        }
    }
    
    protected void AppendEventId(StringBuilder stringBuilder, LogEntry entry)
    {
        if (!Options.ShowEventId || !entry.EventId.HasValue) return;
        
        stringBuilder.Append('[');
        stringBuilder.Append(entry.EventId.Value);
        
        if (!string.IsNullOrEmpty(entry.EventName))
        {
            stringBuilder.Append(':');
            stringBuilder.Append(entry.EventName);
        }
        
        stringBuilder.Append("] ");
    }
    
    protected void AppendScope(StringBuilder stringBuilder, LogEntry entry)
    {
        if (!Options.ShowScope || string.IsNullOrEmpty(entry.Scope)) return;
        
        stringBuilder.Append("=> ");
        stringBuilder.Append(entry.Scope);
        stringBuilder.Append(' ');
    }
    
    protected void AppendException(StringBuilder stringBuilder, LogEntry entry)
    {
        if (!Options.ShowException || entry.Exception == null) return;
        
        stringBuilder.AppendLine();
        stringBuilder.Append("    Exception: ");
        stringBuilder.AppendLine(entry.Exception.GetType().Name);
        stringBuilder.Append("    Message: ");
        stringBuilder.AppendLine(entry.Exception.Message);
        
        if (string.IsNullOrEmpty(entry.Exception.StackTrace)) return;
        
        stringBuilder.AppendLine("    Stack Trace:");
        
        string[] stackLines = entry.Exception.StackTrace.Split('\n');
        foreach (string line in stackLines)
        {
            stringBuilder.Append("      ");
            stringBuilder.AppendLine(line.Trim());
        }
    }
    
    protected string TruncateCategory(string category)
    {
        const int maximumLength = 30;
        if (category.Length <= maximumLength)
            return category;
            
        string[] parts = category.Split('.');
        if (parts.Length == 1)
            return category.Substring(0, maximumLength - 3) + "...";
            
        StringBuilder result = new StringBuilder();
        for (int index = 0; index < parts.Length - 1; index++)
        {
            result.Append(parts[index][0]);
            result.Append('.');
        }
        result.Append(parts[^1]);
        
        if (result.Length > maximumLength)
            return result.ToString(0, maximumLength - 3) + "...";
            
        return result.ToString();
    }
} 
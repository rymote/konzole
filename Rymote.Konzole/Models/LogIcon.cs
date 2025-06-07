namespace Rymote.Konzole.Models;

public static class LogIcon
{
    public static string GetIcon(KonzoleLogLevel level) => level switch
    {
        KonzoleLogLevel.Trace => "🔍",
        KonzoleLogLevel.Debug => "🐛",
        KonzoleLogLevel.Information => "ℹ️",
        KonzoleLogLevel.Success => "✅",
        KonzoleLogLevel.Warning => "⚠️",
        KonzoleLogLevel.Error => "❌",
        KonzoleLogLevel.Fatal => "💀",
        KonzoleLogLevel.Pending => "⏳",
        KonzoleLogLevel.Complete => "✔️",
        KonzoleLogLevel.Note => "📝",
        KonzoleLogLevel.Start => "🚀",
        KonzoleLogLevel.Pause => "⏸️",
        KonzoleLogLevel.Watch => "👁️",
        _ => "•"
    };

    public static string GetFallbackIcon(KonzoleLogLevel level) => level switch
    {
        KonzoleLogLevel.Trace => "[TRACE]",
        KonzoleLogLevel.Debug => "[DEBUG]",
        KonzoleLogLevel.Information => "[INFO]",
        KonzoleLogLevel.Success => "[SUCCESS]",
        KonzoleLogLevel.Warning => "[WARN]",
        KonzoleLogLevel.Error => "[ERROR]",
        KonzoleLogLevel.Fatal => "[FATAL]",
        KonzoleLogLevel.Pending => "[PENDING]",
        KonzoleLogLevel.Complete => "[DONE]",
        KonzoleLogLevel.Note => "[NOTE]",
        KonzoleLogLevel.Start => "[START]",
        KonzoleLogLevel.Pause => "[PAUSE]",
        KonzoleLogLevel.Watch => "[WATCH]",
        _ => "[LOG]"
    };
} 
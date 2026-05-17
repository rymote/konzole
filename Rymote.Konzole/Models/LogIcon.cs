using Microsoft.Extensions.Logging;

namespace Rymote.Konzole.Models;

public static class LogIcon
{
    public static string GetIcon(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace       => "🔍",
        LogLevel.Debug       => "🐛",
        LogLevel.Information => "ℹ️",
        LogLevel.Warning     => "⚠️",
        LogLevel.Error       => "❌",
        LogLevel.Critical    => "💀",
        _                    => "•"
    };

    public static string GetIcon(KonzoleTag tag) => tag switch
    {
        KonzoleTag.Success  => "✅",
        KonzoleTag.Pending  => "⏳",
        KonzoleTag.Complete => "✔️",
        KonzoleTag.Note     => "📝",
        KonzoleTag.Start    => "🚀",
        KonzoleTag.Pause    => "⏸️",
        KonzoleTag.Watch    => "👁️",
        _                   => "•"
    };

    public static string GetFallbackIcon(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace       => "[TRACE]",
        LogLevel.Debug       => "[DEBUG]",
        LogLevel.Information => "[INFO]",
        LogLevel.Warning     => "[WARN]",
        LogLevel.Error       => "[ERROR]",
        LogLevel.Critical    => "[FATAL]",
        _                    => "[LOG]"
    };

    public static string GetFallbackIcon(KonzoleTag tag) => tag switch
    {
        KonzoleTag.Success  => "[SUCCESS]",
        KonzoleTag.Pending  => "[PENDING]",
        KonzoleTag.Complete => "[DONE]",
        KonzoleTag.Note     => "[NOTE]",
        KonzoleTag.Start    => "[START]",
        KonzoleTag.Pause    => "[PAUSE]",
        KonzoleTag.Watch    => "[WATCH]",
        _                   => "[LOG]"
    };
}

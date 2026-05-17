using Microsoft.Extensions.Logging;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Extensions;

public static class LoggerExtensions
{
    public static void LogSuccess(this ILogger logger, string message, params object?[] arguments) =>
        LogWithTag(logger, LogLevel.Information, KonzoleTag.Success, exception: null, message, arguments);

    public static void LogPending(this ILogger logger, string message, params object?[] arguments) =>
        LogWithTag(logger, LogLevel.Information, KonzoleTag.Pending, exception: null, message, arguments);

    public static void LogComplete(this ILogger logger, string message, params object?[] arguments) =>
        LogWithTag(logger, LogLevel.Information, KonzoleTag.Complete, exception: null, message, arguments);

    public static void LogNote(this ILogger logger, string message, params object?[] arguments) =>
        LogWithTag(logger, LogLevel.Information, KonzoleTag.Note, exception: null, message, arguments);

    public static void LogStart(this ILogger logger, string message, params object?[] arguments) =>
        LogWithTag(logger, LogLevel.Information, KonzoleTag.Start, exception: null, message, arguments);

    public static void LogPause(this ILogger logger, string message, params object?[] arguments) =>
        LogWithTag(logger, LogLevel.Information, KonzoleTag.Pause, exception: null, message, arguments);

    public static void LogWatch(this ILogger logger, string message, params object?[] arguments) =>
        LogWithTag(logger, LogLevel.Debug, KonzoleTag.Watch, exception: null, message, arguments);

    public static void LogFatal(this ILogger logger, string message, params object?[] arguments) =>
        logger.LogCritical(message, arguments);

    public static void LogFatal(this ILogger logger, Exception exception, string message, params object?[] arguments) =>
        logger.LogCritical(exception, message, arguments);

    private static void LogWithTag(
        ILogger logger,
        LogLevel logLevel,
        KonzoleTag tag,
        Exception? exception,
        string message,
        params object?[] arguments)
    {
        using (KonzoleScopeState.Push(new KonzoleScopeState { Tag = tag }))
        {
            if (exception != null) logger.Log(logLevel, exception, message, arguments);
            else logger.Log(logLevel, message, arguments);
        }
    }
}

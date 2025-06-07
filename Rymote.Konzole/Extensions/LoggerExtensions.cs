using Microsoft.Extensions.Logging;

namespace Rymote.Konzole.Extensions;

public static class LoggerExtensions
{
    private static readonly EventId SuccessEventId = new(1000, "Success");
    private static readonly EventId FatalEventId = new(1001, "Fatal");
    private static readonly EventId PendingEventId = new(1002, "Pending");
    private static readonly EventId CompleteEventId = new(1003, "Complete");
    private static readonly EventId NoteEventId = new(1004, "Note");
    private static readonly EventId StartEventId = new(1005, "Start");
    private static readonly EventId PauseEventId = new(1006, "Pause");
    private static readonly EventId WatchEventId = new(1007, "Watch");
    
    public static void LogSuccess(this ILogger logger, string message, params object[] args)
    {
        logger.LogInformation(SuccessEventId, message, args);
    }
    
    public static void LogSuccess(this ILogger logger, Exception exception, string message, params object[] args)
    {
        logger.LogInformation(SuccessEventId, exception, message, args);
    }
    
    public static void LogFatal(this ILogger logger, string message, params object[] args)
    {
        logger.LogCritical(FatalEventId, message, args);
    }
    
    public static void LogFatal(this ILogger logger, Exception exception, string message, params object[] args)
    {
        logger.LogCritical(FatalEventId, exception, message, args);
    }
    
    public static void LogPending(this ILogger logger, string message, params object[] args)
    {
        logger.LogInformation(PendingEventId, message, args);
    }
    
    public static void LogComplete(this ILogger logger, string message, params object[] args)
    {
        logger.LogInformation(CompleteEventId, message, args);
    }
    
    public static void LogNote(this ILogger logger, string message, params object[] args)
    {
        logger.LogInformation(NoteEventId, message, args);
    }
    
    public static void LogStart(this ILogger logger, string message, params object[] args)
    {
        logger.LogInformation(StartEventId, message, args);
    }
    
    public static void LogPause(this ILogger logger, string message, params object[] args)
    {
        logger.LogInformation(PauseEventId, message, args);
    }
    
    public static void LogWatch(this ILogger logger, string message, params object[] args)
    {
        logger.LogDebug(WatchEventId, message, args);
    }
    
    public static void LogSuccessWithData<T>(this ILogger logger, string message, T data)
    {
        logger.LogInformation(SuccessEventId, message + " {@Data}", data);
    }
    
    public static void LogErrorWithData<T>(this ILogger logger, Exception exception, string message, T data)
    {
        logger.LogError(exception, message + " {@Data}", data);
    }
} 
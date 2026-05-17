namespace Rymote.Konzole.Diagnostics;

public static class KonzoleDiagnostics
{
    public static event EventHandler<SinkErrorEventArgs>? SinkError;

    private static readonly Dictionary<string, DateTimeOffset> LastFallbackEmitBySink = new();
    private static readonly object FallbackEmitGate = new();
    private static readonly TimeSpan FallbackEmitMinimumInterval = TimeSpan.FromMinutes(1);

    public static void ReportSinkError(SinkErrorEventArgs eventArgs)
    {
        EventHandler<SinkErrorEventArgs>? handler = SinkError;
        if (handler != null)
        {
            handler.Invoke(null, eventArgs);
            return;
        }

        if (!ShouldEmitFallback(eventArgs.SinkName)) return;

        Console.Error.WriteLine(eventArgs.Exception != null
            ? $"[Konzole/{eventArgs.SinkName}] {eventArgs.Message}: {eventArgs.Exception.Message}"
            : $"[Konzole/{eventArgs.SinkName}] {eventArgs.Message}");
    }

    private static bool ShouldEmitFallback(string sinkName)
    {
        lock (FallbackEmitGate)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (LastFallbackEmitBySink.TryGetValue(sinkName, out DateTimeOffset previousEmit)
                && now - previousEmit < FallbackEmitMinimumInterval)
            {
                return false;
            }
            LastFallbackEmitBySink[sinkName] = now;
            return true;
        }
    }
}

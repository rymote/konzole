namespace Rymote.Konzole.Diagnostics;

public sealed class SinkErrorEventArgs : EventArgs
{
    public string SinkName { get; }
    public Exception? Exception { get; }
    public string Message { get; }
    public int DroppedEntries { get; }

    public SinkErrorEventArgs(string sinkName, string message, Exception? exception = null, int droppedEntries = 0)
    {
        SinkName = sinkName;
        Message = message;
        Exception = exception;
        DroppedEntries = droppedEntries;
    }
}

namespace Rymote.Konzole.Sinks.Files;

internal interface IFileRollingStrategy
{
    string ResolveActivePath(string basePath, DateTimeOffset now);
    bool ShouldRoll(string activePath, long currentSize, long pendingBytes, DateTimeOffset now);
    void Roll(string basePath, int maxFiles, DateTimeOffset now);
}

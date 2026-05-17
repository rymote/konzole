namespace Rymote.Konzole.Sinks.Files;

internal sealed class SizeOnlyRollingStrategy : IFileRollingStrategy
{
    public long MaxFileSize { get; init; } = 10 * 1024 * 1024;

    public string ResolveActivePath(string basePath, DateTimeOffset now) => basePath;

    public bool ShouldRoll(string activePath, long currentSize, long pendingBytes, DateTimeOffset now) =>
        currentSize + pendingBytes > MaxFileSize;

    public void Roll(string basePath, int maxFiles, DateTimeOffset now)
    {
        string directoryPath = Path.GetDirectoryName(basePath)!;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(basePath);
        string fileExtension = Path.GetExtension(basePath);

        string PathForIndex(int rotationIndex) =>
            rotationIndex == 0
                ? basePath
                : Path.Combine(directoryPath, $"{fileNameWithoutExtension}.{rotationIndex}{fileExtension}");

        int oldestIndexToDrop = maxFiles - 1;
        string oldestPath = PathForIndex(oldestIndexToDrop);
        if (File.Exists(oldestPath))
            File.Delete(oldestPath);

        for (int rotationIndex = oldestIndexToDrop - 1; rotationIndex >= 1; rotationIndex--)
        {
            string sourcePath = PathForIndex(rotationIndex);
            string destinationPath = PathForIndex(rotationIndex + 1);
            if (File.Exists(sourcePath))
                File.Move(sourcePath, destinationPath);
        }

        if (File.Exists(basePath))
            File.Move(basePath, PathForIndex(1));
    }
}

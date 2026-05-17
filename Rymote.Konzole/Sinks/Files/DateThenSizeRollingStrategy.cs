using System.Globalization;
using System.Text.RegularExpressions;

namespace Rymote.Konzole.Sinks.Files;

internal sealed class DateThenSizeRollingStrategy : IFileRollingStrategy
{
    public long MaxFileSize { get; init; } = 10 * 1024 * 1024;

    private static readonly Regex DatedFileRegex = new(@"^(?<stem>.+)-(?<date>\d{4}-\d{2}-\d{2})(?<ext>\.[^.]+)$",
        RegexOptions.Compiled);

    public string ResolveActivePath(string basePath, DateTimeOffset now)
    {
        string directoryPath = Path.GetDirectoryName(basePath)!;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(basePath);
        string fileExtension = Path.GetExtension(basePath);
        string dateStamp = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return Path.Combine(directoryPath, $"{fileNameWithoutExtension}-{dateStamp}{fileExtension}");
    }

    public bool ShouldRoll(string activePath, long currentSize, long pendingBytes, DateTimeOffset now)
    {
        DateTimeOffset? activeDate = ExtractActiveDate(activePath);
        if (activeDate != null && activeDate.Value.Date != now.Date) return true;
        return currentSize + pendingBytes > MaxFileSize;
    }

    public void Roll(string basePath, int maxFiles, DateTimeOffset now)
    {
        string directoryPath = Path.GetDirectoryName(basePath)!;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(basePath);
        string fileExtension = Path.GetExtension(basePath);
        string dateStamp = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        string PathForIndex(int rotationIndex) =>
            rotationIndex == 0
                ? Path.Combine(directoryPath, $"{fileNameWithoutExtension}-{dateStamp}{fileExtension}")
                : Path.Combine(directoryPath, $"{fileNameWithoutExtension}-{dateStamp}.{rotationIndex}{fileExtension}");

        int oldestIndexToDrop = maxFiles - 1;
        string oldestPath = PathForIndex(oldestIndexToDrop);
        if (File.Exists(oldestPath)) File.Delete(oldestPath);

        for (int rotationIndex = oldestIndexToDrop - 1; rotationIndex >= 1; rotationIndex--)
        {
            string sourcePath = PathForIndex(rotationIndex);
            string destinationPath = PathForIndex(rotationIndex + 1);
            if (File.Exists(sourcePath))
                File.Move(sourcePath, destinationPath);
        }

        string activeForToday = PathForIndex(0);
        if (File.Exists(activeForToday))
            File.Move(activeForToday, PathForIndex(1));

        DateTimeOffset cutoff = now.AddDays(-(maxFiles - 1)).Date;
        foreach (string candidatePath in Directory.EnumerateFiles(directoryPath, $"{fileNameWithoutExtension}-*{fileExtension}"))
        {
            DateTimeOffset? candidateDate = ExtractActiveDate(candidatePath);
            if (candidateDate != null && candidateDate.Value.Date < cutoff)
                File.Delete(candidatePath);
        }
    }

    private static DateTimeOffset? ExtractActiveDate(string path)
    {
        string fileName = Path.GetFileName(path);
        Match match = DatedFileRegex.Match(fileName);
        if (!match.Success) return null;
        return DateTimeOffset.ParseExact(match.Groups["date"].Value, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
    }
}

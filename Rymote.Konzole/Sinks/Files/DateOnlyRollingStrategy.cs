using System.Globalization;
using System.Text.RegularExpressions;

namespace Rymote.Konzole.Sinks.Files;

internal sealed class DateOnlyRollingStrategy : IFileRollingStrategy
{
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
        DateTimeOffset? activeDate = ExtractDate(activePath);
        if (activeDate == null) return true;
        return activeDate.Value.Date != now.Date;
    }

    public void Roll(string basePath, int maxFiles, DateTimeOffset now)
    {
        string directoryPath = Path.GetDirectoryName(basePath)!;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(basePath);
        string fileExtension = Path.GetExtension(basePath);
        DateTimeOffset cutoff = now.AddDays(-(maxFiles - 1)).Date;

        foreach (string candidatePath in Directory.EnumerateFiles(directoryPath, $"{fileNameWithoutExtension}-*{fileExtension}"))
        {
            DateTimeOffset? candidateDate = ExtractDate(candidatePath);
            if (candidateDate != null && candidateDate.Value.Date < cutoff)
            {
                File.Delete(candidatePath);
            }
        }
    }

    private static DateTimeOffset? ExtractDate(string path)
    {
        string fileName = Path.GetFileName(path);
        Match match = DatedFileRegex.Match(fileName);
        if (!match.Success) return null;
        return DateTimeOffset.ParseExact(match.Groups["date"].Value, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
    }
}

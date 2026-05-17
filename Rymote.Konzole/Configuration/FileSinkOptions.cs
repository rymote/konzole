using Rymote.Konzole.Sinks.Files;

namespace Rymote.Konzole.Configuration;

public sealed class FileSinkOptions : SinkOptionsBase
{
    public string? FilePath { get; set; }
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024;
    public int MaxFiles { get; set; } = 5;
    public FileRollingPolicy RollingPolicy { get; set; } = FileRollingPolicy.SizeOnly;
    public TimeSpan FlushInterval { get; set; } = TimeSpan.Zero;
}

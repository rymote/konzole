using Microsoft.Extensions.Logging;
using Rymote.Konzole.Formatters;

namespace Rymote.Konzole.Configuration;

public class FileSinkOptions : SinkOptionsBase
{
    public string? FilePath { get; set; }
    
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024;
    
    public int MaxFiles { get; set; } = 5;
} 
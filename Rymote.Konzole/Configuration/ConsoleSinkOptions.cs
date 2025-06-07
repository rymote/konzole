using Microsoft.Extensions.Logging;
using Rymote.Konzole.Formatters;

namespace Rymote.Konzole.Configuration;

public class ConsoleSinkOptions : SinkOptionsBase
{
    public bool UseColors { get; set; } = true;
    
    public bool UseEmojis { get; set; } = true;
    
    public int MaxMessageLength { get; set; } = 1000;
    
    public bool PrettyPrint { get; set; } = true;
} 
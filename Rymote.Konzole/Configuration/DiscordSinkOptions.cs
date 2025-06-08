using Rymote.Konzole.Models;

namespace Rymote.Konzole.Configuration;

public class DiscordSinkOptions : SinkOptionsBase
{
    public string? WebhookUrl { get; set; }
    
    public string? Username { get; set; } = "Konzole Logger";
    
    public string? AvatarUrl { get; set; }
    
    public bool UseEmbeds { get; set; } = true;
    
    public int MaxMessageLength { get; set; } = 2000;
    
    public Dictionary<KonzoleLogLevel, int> EmbedColors { get; set; } = new()
    {
        { KonzoleLogLevel.Trace, 0x808080 },
        { KonzoleLogLevel.Debug, 0x9B9B9B },
        { KonzoleLogLevel.Information, 0x00D4FF },
        { KonzoleLogLevel.Success, 0x00FF00 },
        { KonzoleLogLevel.Warning, 0xFFFF00 },
        { KonzoleLogLevel.Error, 0xFF0000 },
        { KonzoleLogLevel.Fatal, 0x8B0000 },
        { KonzoleLogLevel.Pending, 0x0000FF },
        { KonzoleLogLevel.Complete, 0x008000 },
        { KonzoleLogLevel.Note, 0xFF00FF },
        { KonzoleLogLevel.Start, 0x00CED1 },
        { KonzoleLogLevel.Pause, 0xFFD700 },
        { KonzoleLogLevel.Watch, 0x8B008B }
    };
}
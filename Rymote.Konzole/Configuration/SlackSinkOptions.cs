using Rymote.Konzole.Models;

namespace Rymote.Konzole.Configuration;

public class SlackSinkOptions : SinkOptionsBase
{
    public string? WebhookUrl { get; set; }
    
    public string? Channel { get; set; }
    
    public string? Username { get; set; } = "Konzole Logger";
    
    public string? IconEmoji { get; set; } = ":robot_face:";
    
    public string? IconUrl { get; set; }
    
    public bool UseAttachments { get; set; } = true;
    
    public int MaxMessageLength { get; set; } = 3000;
    
    public Dictionary<KonzoleLogLevel, string> AttachmentColors { get; set; } = new()
    {
        { KonzoleLogLevel.Trace, "#808080" },
        { KonzoleLogLevel.Debug, "#9B9B9B" },
        { KonzoleLogLevel.Information, "#00D4FF" },
        { KonzoleLogLevel.Success, "#00FF00" },
        { KonzoleLogLevel.Warning, "#FFFF00" },
        { KonzoleLogLevel.Error, "#FF0000" },
        { KonzoleLogLevel.Fatal, "#8B0000" },
        { KonzoleLogLevel.Pending, "#0000FF" },
        { KonzoleLogLevel.Complete, "#008000" },
        { KonzoleLogLevel.Note, "#FF00FF" },
        { KonzoleLogLevel.Start, "#00CED1" },
        { KonzoleLogLevel.Pause, "#FFD700" },
        { KonzoleLogLevel.Watch, "#8B008B" }
    };
}
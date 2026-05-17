using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks.Http;

namespace Rymote.Konzole.Configuration;

public sealed class SlackSinkOptions : HttpSinkOptionsBase
{
    public string? WebhookUrl { get; set; }
    public string? Channel { get; set; }
    public string? Username { get; set; } = "Konzole Logger";
    public string? IconEmoji { get; set; } = ":robot_face:";
    public string? IconUrl { get; set; }
    public bool UseAttachments { get; set; } = true;

    public IReadOnlyDictionary<KonzoleTag, string> TagAttachmentColors { get; init; } = new Dictionary<KonzoleTag, string>
    {
        [KonzoleTag.Success]  = "#00FF00",
        [KonzoleTag.Pending]  = "#0000FF",
        [KonzoleTag.Complete] = "#008000",
        [KonzoleTag.Note]     = "#FF00FF",
        [KonzoleTag.Start]    = "#00CED1",
        [KonzoleTag.Pause]    = "#FFD700",
        [KonzoleTag.Watch]    = "#8B008B"
    };

    public IReadOnlyDictionary<Microsoft.Extensions.Logging.LogLevel, string> LevelAttachmentColors { get; init; }
        = new Dictionary<Microsoft.Extensions.Logging.LogLevel, string>
    {
        [Microsoft.Extensions.Logging.LogLevel.Trace]       = "#808080",
        [Microsoft.Extensions.Logging.LogLevel.Debug]       = "#9B9B9B",
        [Microsoft.Extensions.Logging.LogLevel.Information] = "#00D4FF",
        [Microsoft.Extensions.Logging.LogLevel.Warning]     = "#FFFF00",
        [Microsoft.Extensions.Logging.LogLevel.Error]       = "#FF0000",
        [Microsoft.Extensions.Logging.LogLevel.Critical]    = "#8B0000"
    };

    public SlackSinkOptions()
    {
        HttpClientName = "Konzole.Slack";
        BatchSize = 1;
        MaxMessageLength = 3000;
    }
}

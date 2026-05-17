using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks.Http;

namespace Rymote.Konzole.Configuration;

public sealed class DiscordSinkOptions : HttpSinkOptionsBase
{
    public string? WebhookUrl { get; set; }
    public string? Username { get; set; } = "Konzole Logger";
    public string? AvatarUrl { get; set; }
    public bool UseEmbeds { get; set; } = true;

    public IReadOnlyDictionary<KonzoleTag, int> TagEmbedColors { get; init; } = new Dictionary<KonzoleTag, int>
    {
        [KonzoleTag.Success]  = 0x00FF00,
        [KonzoleTag.Pending]  = 0x0000FF,
        [KonzoleTag.Complete] = 0x008000,
        [KonzoleTag.Note]     = 0xFF00FF,
        [KonzoleTag.Start]    = 0x00CED1,
        [KonzoleTag.Pause]    = 0xFFD700,
        [KonzoleTag.Watch]    = 0x8B008B
    };

    public IReadOnlyDictionary<Microsoft.Extensions.Logging.LogLevel, int> LevelEmbedColors { get; init; }
        = new Dictionary<Microsoft.Extensions.Logging.LogLevel, int>
    {
        [Microsoft.Extensions.Logging.LogLevel.Trace]       = 0x808080,
        [Microsoft.Extensions.Logging.LogLevel.Debug]       = 0x9B9B9B,
        [Microsoft.Extensions.Logging.LogLevel.Information] = 0x00D4FF,
        [Microsoft.Extensions.Logging.LogLevel.Warning]     = 0xFFFF00,
        [Microsoft.Extensions.Logging.LogLevel.Error]       = 0xFF0000,
        [Microsoft.Extensions.Logging.LogLevel.Critical]    = 0x8B0000
    };

    public DiscordSinkOptions()
    {
        HttpClientName = "Konzole.Discord";
        BatchSize = 1;
        MaxMessageLength = 2000;
    }
}

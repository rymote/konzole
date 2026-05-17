using System.Net.Http;
using System.Text;
using System.Text.Json;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks.Http;

namespace Rymote.Konzole.Sinks;

public sealed class DiscordSink : HttpSinkBase<DiscordSinkOptions>
{
    public DiscordSink(DiscordSinkOptions options, System.Net.Http.IHttpClientFactory httpClientFactory)
        : base(options, httpClientFactory.CreateClient(options.HttpClientName))
    {
        if (string.IsNullOrEmpty(options.WebhookUrl))
            throw new ArgumentException("Discord webhook URL is required.", nameof(options));
    }

    public override string Name => "Discord";

    protected override ILogFormatter CreateDefaultFormatter() => new DiscordFormatter();

    protected override HttpRequestMessage BuildRequest(IReadOnlyList<LogEntry> batch)
    {
        LogEntry entry = batch[0];
        object payload = Options.UseEmbeds ? BuildEmbedPayload(entry) : BuildPlainPayload(entry);
        string jsonPayload = JsonSerializer.Serialize(payload);
        return new HttpRequestMessage(HttpMethod.Post, Options.WebhookUrl)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };
    }

    private object BuildPlainPayload(LogEntry entry) => new
    {
        username = Options.Username,
        avatar_url = Options.AvatarUrl,
        content = FormatterHelpers.TruncateMessage(Formatter.Format(entry, FormatterContext), Options.MaxMessageLength)
    };

    private object BuildEmbedPayload(LogEntry entry)
    {
        int embedColor = entry.Tag.HasValue
            ? Options.TagEmbedColors.GetValueOrDefault(entry.Tag.Value, 0x808080)
            : Options.LevelEmbedColors.GetValueOrDefault(entry.Level, 0x808080);

        string title = entry.Tag.HasValue
            ? $"{LogIcon.GetIcon(entry.Tag.Value)} {entry.Tag.Value}"
            : $"{LogIcon.GetIcon(entry.Level)} {entry.Level}";

        return new
        {
            username = Options.Username,
            avatar_url = Options.AvatarUrl,
            embeds = new[]
            {
                new
                {
                    title,
                    description = FormatterHelpers.TruncateMessage(entry.Message, Options.MaxMessageLength),
                    color = embedColor,
                    timestamp = entry.Timestamp.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK"),
                    footer = new { text = Environment.MachineName }
                }
            }
        };
    }
}

using System.Net.Http;
using System.Text;
using System.Text.Json;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks.Http;

namespace Rymote.Konzole.Sinks;

public sealed class SlackSink : HttpSinkBase<SlackSinkOptions>
{
    public SlackSink(SlackSinkOptions options, System.Net.Http.IHttpClientFactory httpClientFactory)
        : base(options, httpClientFactory.CreateClient(options.HttpClientName))
    {
        if (string.IsNullOrEmpty(options.WebhookUrl))
            throw new ArgumentException("Slack webhook URL is required.", nameof(options));
    }

    public override string Name => "Slack";

    protected override ILogFormatter CreateDefaultFormatter() => new SlackFormatter();

    protected override HttpRequestMessage BuildRequest(IReadOnlyList<LogEntry> batch)
    {
        LogEntry entry = batch[0];
        object payload = Options.UseAttachments ? BuildAttachmentPayload(entry) : BuildPlainPayload(entry);
        string jsonPayload = JsonSerializer.Serialize(payload);
        return new HttpRequestMessage(HttpMethod.Post, Options.WebhookUrl)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };
    }

    private object BuildPlainPayload(LogEntry entry) => new
    {
        channel = Options.Channel,
        username = Options.Username,
        icon_emoji = Options.IconEmoji,
        icon_url = Options.IconUrl,
        text = FormatterHelpers.TruncateMessage(Formatter.Format(entry, FormatterContext), Options.MaxMessageLength)
    };

    private object BuildAttachmentPayload(LogEntry entry)
    {
        string attachmentColor = entry.Tag.HasValue
            ? Options.TagAttachmentColors.GetValueOrDefault(entry.Tag.Value, "#808080")
            : Options.LevelAttachmentColors.GetValueOrDefault(entry.Level, "#808080");

        string label = entry.Tag?.ToString() ?? entry.Level.ToString();
        string icon = entry.Tag.HasValue ? LogIcon.GetIcon(entry.Tag.Value) : LogIcon.GetIcon(entry.Level);

        return new
        {
            channel = Options.Channel,
            username = Options.Username,
            icon_emoji = Options.IconEmoji,
            icon_url = Options.IconUrl,
            attachments = new[]
            {
                new
                {
                    fallback = $"{label}: {entry.Message}",
                    color = attachmentColor,
                    pretext = $"{icon} *{label}*",
                    text = FormatterHelpers.TruncateMessage(entry.Message, Options.MaxMessageLength),
                    ts = entry.Timestamp.ToUnixTimeSeconds(),
                    footer = Environment.MachineName,
                    mrkdwn_in = new[] { "pretext", "text" }
                }
            }
        };
    }
}

using System.Text;
using System.Text.Json;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Sinks;

public class SlackSink : SinkBase<SlackSinkOptions>
{
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    
    public override string Name => "Slack";
    
    public SlackSink(SlackSinkOptions options, HttpClient? httpClient = null) : base(options)
    {
        _httpClient = httpClient ?? new HttpClient();
        
        if (string.IsNullOrEmpty(options.WebhookUrl))
        {
            throw new ArgumentException("Slack webhook URL is required", nameof(options));
        }
    }
    
    public override async Task WriteAsync(LogEntry entry)
    {
        if (!ShouldLog(entry))
            return;
            
        await _semaphoreSlim.WaitAsync();
        try
        {
            object payload;
            
            if (Options.UseAttachments)
            {
                payload = CreateAttachmentPayload(entry);
            }
            else
            {
                payload = CreateSimplePayload(entry);
            }
            
            string jsonContent = JsonSerializer.Serialize(payload);
            StringContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            HttpResponseMessage response = await _httpClient.PostAsync(Options.WebhookUrl, httpContent);
            
            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"Failed to send log to Slack: {response.StatusCode}");
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Error sending log to Slack: {exception.Message}");
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }
    
    protected override ILogFormatter CreateDefaultFormatter()
    {
        return new SlackFormatter(Options);
    }
    
    private object CreateAttachmentPayload(LogEntry entry)
    {
        List<object> fields = new List<object>();
        
        if (Options.ShowCategory && !string.IsNullOrEmpty(entry.Category))
        {
            fields.Add(new { title = "Category", value = entry.Category, @short = true });
        }
        
        if (Options.ShowEventId && entry.EventId.HasValue)
        {
            string eventValue = entry.EventId.Value.ToString();
            if (!string.IsNullOrEmpty(entry.EventName))
            {
                eventValue += $" ({entry.EventName})";
            }
            fields.Add(new { title = "Event", value = eventValue, @short = true });
        }
        
        if (Options.ShowScope && !string.IsNullOrEmpty(entry.Scope))
        {
            fields.Add(new { title = "Scope", value = entry.Scope, @short = true });
        }
        
        if (entry.Properties?.Count > 0)
        {
            foreach (KeyValuePair<string, object?> property in entry.Properties)
            {
                fields.Add(new { title = property.Key, value = property.Value?.ToString() ?? "null", @short = true });
            }
        }
        
        List<object> attachments = new List<object>();
        
        string text = entry.Message;
        if (text.Length > Options.MaxMessageLength)
        {
            text = text.Substring(0, Options.MaxMessageLength - 3) + "...";
        }
        
        attachments.Add(new
        {
            fallback = $"{entry.Level}: {entry.Message}",
            color = Options.AttachmentColors.GetValueOrDefault(entry.Level, "#808080"),
            pretext = $"{LogIcon.GetIcon(entry.Level)} *{entry.Level}*",
            text = text,
            fields = fields,
            ts = new DateTimeOffset(entry.Timestamp).ToUnixTimeSeconds(),
            footer = Environment.MachineName,
            mrkdwn_in = new[] { "pretext", "text" }
        });
        
        if (Options.ShowException && entry.Exception != null)
        {
            string exceptionText = $"*{entry.Exception.GetType().Name}*: {entry.Exception.Message}";
            if (!string.IsNullOrEmpty(entry.Exception.StackTrace))
            {
                exceptionText += $"\n```{entry.Exception.StackTrace}```";
            }
            
            if (exceptionText.Length > Options.MaxMessageLength)
            {
                exceptionText = exceptionText.Substring(0, Options.MaxMessageLength - 3) + "...";
            }
            
            attachments.Add(new
            {
                fallback = $"Exception: {entry.Exception.Message}",
                color = "danger",
                title = "Exception Details",
                text = exceptionText,
                mrkdwn_in = new[] { "text" }
            });
        }
        
        object payload = new
        {
            channel = Options.Channel,
            username = Options.Username,
            icon_emoji = Options.IconEmoji,
            icon_url = Options.IconUrl,
            attachments = attachments
        };
        
        return payload;
    }
    
    private object CreateSimplePayload(LogEntry entry)
    {
        string message = Formatter.Format(entry);
        if (message.Length > Options.MaxMessageLength)
        {
            message = message.Substring(0, Options.MaxMessageLength - 3) + "...";
        }
        
        return new
        {
            channel = Options.Channel,
            username = Options.Username,
            icon_emoji = Options.IconEmoji,
            icon_url = Options.IconUrl,
            text = message
        };
    }
    
    public override void Dispose()
    {
        base.Dispose();
        _httpClient?.Dispose();
        _semaphoreSlim?.Dispose();
    }
}
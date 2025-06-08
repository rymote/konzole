using System.Text;
using System.Text.Json;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Sinks;

public class DiscordSink : SinkBase<DiscordSinkOptions>
{
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    
    public override string Name => "Discord";
    
    public DiscordSink(DiscordSinkOptions options, HttpClient? httpClient = null) : base(options)
    {
        _httpClient = httpClient ?? new HttpClient();
        
        if (string.IsNullOrEmpty(options.WebhookUrl))
        {
            throw new ArgumentException("Discord webhook URL is required", nameof(options));
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
            
            if (Options.UseEmbeds)
            {
                payload = CreateEmbedPayload(entry);
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
                Console.Error.WriteLine($"Failed to send log to Discord: {response.StatusCode}");
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Error sending log to Discord: {exception.Message}");
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }
    
    protected override ILogFormatter CreateDefaultFormatter()
    {
        return new DiscordFormatter(Options);
    }
    
    private object CreateEmbedPayload(LogEntry entry)
    {
        List<object> fields = new List<object>();
        
        if (Options.ShowCategory && !string.IsNullOrEmpty(entry.Category))
        {
            fields.Add(new { name = "Category", value = entry.Category, inline = true });
        }
        
        if (Options.ShowEventId && entry.EventId.HasValue)
        {
            string eventValue = entry.EventId.Value.ToString();
            if (!string.IsNullOrEmpty(entry.EventName))
            {
                eventValue += $" ({entry.EventName})";
            }
            fields.Add(new { name = "Event", value = eventValue, inline = true });
        }
        
        if (Options.ShowScope && !string.IsNullOrEmpty(entry.Scope))
        {
            fields.Add(new { name = "Scope", value = entry.Scope, inline = true });
        }
        
        if (entry.Properties?.Count > 0)
        {
            foreach (KeyValuePair<string, object?> property in entry.Properties)
            {
                fields.Add(new { name = property.Key, value = property.Value?.ToString() ?? "null", inline = true });
            }
        }
        
        if (Options.ShowException && entry.Exception != null)
        {
            string exceptionText = $"**{entry.Exception.GetType().Name}**: {entry.Exception.Message}";
            if (exceptionText.Length > 1024)
            {
                exceptionText = exceptionText.Substring(0, 1021) + "...";
            }
            fields.Add(new { name = "Exception", value = exceptionText, inline = false });
            
            if (!string.IsNullOrEmpty(entry.Exception.StackTrace))
            {
                string stackTrace = entry.Exception.StackTrace;
                if (stackTrace.Length > 1024)
                {
                    stackTrace = stackTrace.Substring(0, 1021) + "...";
                }
                fields.Add(new { name = "Stack Trace", value = $"```\n{stackTrace}\n```", inline = false });
            }
        }
        
        string description = entry.Message;
        if (description.Length > Options.MaxMessageLength)
        {
            description = description.Substring(0, Options.MaxMessageLength - 3) + "...";
        }
        
        return new
        {
            username = Options.Username,
            avatar_url = Options.AvatarUrl,
            embeds = new[]
            {
                new
                {
                    title = $"{LogIcon.GetIcon(entry.Level)} {entry.Level}",
                    description = description,
                    color = Options.EmbedColors.GetValueOrDefault(entry.Level, 0x808080),
                    fields = fields,
                    timestamp = entry.Timestamp.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK"),
                    footer = new
                    {
                        text = Environment.MachineName
                    }
                }
            }
        };
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
            username = Options.Username,
            avatar_url = Options.AvatarUrl,
            content = message
        };
    }
    
    public override void Dispose()
    {
        base.Dispose();
        _httpClient?.Dispose();
        _semaphoreSlim?.Dispose();
    }
}
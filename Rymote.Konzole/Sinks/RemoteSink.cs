using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Sinks;

public class RemoteSink : SinkBase<RemoteSinkOptions>
{
    private readonly HttpClient _httpClient;
    private readonly ConcurrentQueue<string> _messageQueue = new();
    private readonly Timer _flushTimer;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    
    public override string Name => "Remote";
    
    public RemoteSink(RemoteSinkOptions options, HttpClient? httpClient = null) : base(options)
    {
        _httpClient = httpClient ?? new HttpClient();
        
        if (!string.IsNullOrEmpty(options.RemoteApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", options.RemoteApiKey);
        }
        
        _flushTimer = new Timer(_ => FlushAsync().Wait(), null, 
            options.RemoteFlushInterval, options.RemoteFlushInterval);
    }
    
    public override async Task WriteAsync(LogEntry entry)
    {
        if (!ShouldLog(entry))
            return;
            
        string formattedMessage = Formatter.Format(entry);
        _messageQueue.Enqueue(formattedMessage);
        
        if (_messageQueue.Count >= Options.RemoteBatchSize)
        {
            await FlushAsync();
        }
    }
    
    public override async Task FlushAsync()
    {
        if (string.IsNullOrEmpty(Options.RemoteEndpoint))
            return;
            
        await _semaphoreSlim.WaitAsync();
        try
        {
            List<string> entries = new List<string>();
            while (_messageQueue.TryDequeue(out string? entry) && entries.Count < Options.RemoteBatchSize)
            {
                entries.Add(entry);
            }
            
            if (entries.Count == 0)
                return;
                
            await SendBatchAsync(entries);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }
    
    protected override ILogFormatter CreateDefaultFormatter()
    {
        return new JsonFormatter(Options);
    }
    
    private async Task SendBatchAsync(List<string> entries)
    {
        try
        {
            object batch = new
            {
                timestamp = DateTime.UtcNow,
                source = Environment.MachineName,
                entries = entries.Select(entry => JsonSerializer.Deserialize<JsonElement>(entry))
            };
            
            string jsonContent = JsonSerializer.Serialize(batch);
            StringContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            HttpResponseMessage response = await _httpClient.PostAsync(Options.RemoteEndpoint, httpContent);
            
            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"Failed to send logs to remote endpoint: {response.StatusCode}");
                
                foreach (string entry in entries)
                {
                    _messageQueue.Enqueue(entry);
                }
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Error sending logs to remote endpoint: {exception.Message}");
            
            foreach (string entry in entries)
            {
                _messageQueue.Enqueue(entry);
            }
        }
    }
    
    public override void Dispose()
    {
        _flushTimer?.Dispose();
        base.Dispose();
        _httpClient?.Dispose();
        _semaphoreSlim?.Dispose();
    }
} 
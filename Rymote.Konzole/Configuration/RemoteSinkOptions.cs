using Microsoft.Extensions.Logging;
using Rymote.Konzole.Formatters;

namespace Rymote.Konzole.Configuration;

public class RemoteSinkOptions : SinkOptionsBase
{
    public string? RemoteEndpoint { get; set; }
    
    public string? RemoteApiKey { get; set; }
    
    public int RemoteBatchSize { get; set; } = 100;
    
    public TimeSpan RemoteFlushInterval { get; set; } = TimeSpan.FromSeconds(5);
} 
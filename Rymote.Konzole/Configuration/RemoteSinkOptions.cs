using Rymote.Konzole.Sinks.Http;

namespace Rymote.Konzole.Configuration;

public sealed class RemoteSinkOptions : HttpSinkOptionsBase
{
    public string? RemoteEndpoint { get; set; }
    public string? RemoteApiKey { get; set; }

    public RemoteSinkOptions()
    {
        HttpClientName = "Konzole.Remote";
    }
}

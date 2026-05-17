using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks.Http;

namespace Rymote.Konzole.Sinks;

public sealed class RemoteSink : HttpSinkBase<RemoteSinkOptions>
{
    public RemoteSink(RemoteSinkOptions options, System.Net.Http.IHttpClientFactory httpClientFactory)
        : base(options, httpClientFactory.CreateClient(options.HttpClientName))
    {
        if (!string.IsNullOrEmpty(options.RemoteApiKey))
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.RemoteApiKey);
    }

    public override string Name => "Remote";

    protected override ILogFormatter CreateDefaultFormatter() => new JsonFormatter();

    protected override HttpRequestMessage BuildRequest(IReadOnlyList<LogEntry> batch)
    {
        object batchEnvelope = new
        {
            timestamp = DateTimeOffset.UtcNow,
            source = Environment.MachineName,
            entries = batch.Select(entry => JsonSerializer.Deserialize<JsonElement>(Formatter.Format(entry, FormatterContext)))
        };

        string jsonPayload = JsonSerializer.Serialize(batchEnvelope);
        return new HttpRequestMessage(HttpMethod.Post, Options.RemoteEndpoint)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };
    }
}

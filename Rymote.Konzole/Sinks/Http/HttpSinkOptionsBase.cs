using Rymote.Konzole.Configuration;

namespace Rymote.Konzole.Sinks.Http;

public abstract class HttpSinkOptionsBase : SinkOptionsBase
{
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
    public int BatchSize { get; set; } = 100;
    public string HttpClientName { get; set; } = "Konzole.Default";
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

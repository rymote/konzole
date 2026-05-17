using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Diagnostics;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks.Http;
using Rymote.Konzole.Tests.Infrastructure;
using Xunit;

namespace Rymote.Konzole.Tests.Sinks;

public class HttpSinkRetryTests
{
    private sealed class ProbeSinkOptions : HttpSinkOptionsBase { }

    private sealed class ProbeHttpSink : HttpSinkBase<ProbeSinkOptions>
    {
        public string Endpoint { get; }
        public ProbeHttpSink(ProbeSinkOptions options, HttpClient httpClient, string endpoint)
            : base(options, httpClient) { Endpoint = endpoint; }
        public override string Name => "Probe";
        protected override ILogFormatter CreateDefaultFormatter() => new JsonFormatter();
        protected override HttpRequestMessage BuildRequest(IReadOnlyList<LogEntry> batch)
            => new(HttpMethod.Post, Endpoint) { Content = new StringContent($"{batch.Count}") };
    }

    [Fact]
    public async Task TransientFailures_AreRetriedUpToMaxAttempts_ThenSucceed()
    {
        FakeHttpMessageHandler handler = new();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK));

        using HttpClient httpClient = new(handler);
        ProbeSinkOptions options = new()
        {
            MaxRetryAttempts = 3,
            BaseRetryDelay = TimeSpan.FromMilliseconds(1),
            MaxRetryDelay = TimeSpan.FromMilliseconds(5),
            ShutdownTimeout = TimeSpan.FromSeconds(2),
            BatchSize = 1
        };
        await using ProbeHttpSink sink = new(options, httpClient, "https://example.test/log");

        sink.TryEnqueue(new LogEntry { Level = LogLevel.Information, Message = "retry-me" });
        await sink.FlushAsync(CancellationToken.None);

        Assert.Equal(3, handler.RecordedRequests.Count);
    }

    [Fact]
    public async Task NonRetryable4xx_DropsBatchImmediately()
    {
        FakeHttpMessageHandler handler = new();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.BadRequest));

        using HttpClient httpClient = new(handler);
        ProbeSinkOptions options = new()
        {
            MaxRetryAttempts = 5,
            BaseRetryDelay = TimeSpan.FromMilliseconds(1),
            MaxRetryDelay = TimeSpan.FromMilliseconds(5),
            ShutdownTimeout = TimeSpan.FromSeconds(2),
            BatchSize = 1
        };

        int reportedDropped = 0;
        void ErrorHandler(object? sender, SinkErrorEventArgs args) => reportedDropped += args.DroppedEntries;
        KonzoleDiagnostics.SinkError += ErrorHandler;

        try
        {
            await using ProbeHttpSink sink = new(options, httpClient, "https://example.test/log");
            sink.TryEnqueue(new LogEntry { Level = LogLevel.Information, Message = "bad" });
            await sink.FlushAsync(CancellationToken.None);
        }
        finally
        {
            KonzoleDiagnostics.SinkError -= ErrorHandler;
        }

        Assert.Equal(1, handler.RecordedRequests.Count);
        Assert.Equal(1, reportedDropped);
    }

    [Fact]
    public async Task TooManyRequests_HonorsRetryAfterHeader()
    {
        FakeHttpMessageHandler handler = new();
        HttpResponseMessage throttled = new(HttpStatusCode.TooManyRequests);
        throttled.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromMilliseconds(50));
        handler.EnqueueResponse(throttled);
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK));

        using HttpClient httpClient = new(handler);
        ProbeSinkOptions options = new()
        {
            MaxRetryAttempts = 3,
            BaseRetryDelay = TimeSpan.FromMilliseconds(1),
            MaxRetryDelay = TimeSpan.FromMilliseconds(5),
            ShutdownTimeout = TimeSpan.FromSeconds(2),
            BatchSize = 1
        };
        await using ProbeHttpSink sink = new(options, httpClient, "https://example.test/log");

        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        sink.TryEnqueue(new LogEntry { Level = LogLevel.Information, Message = "throttled" });
        await sink.FlushAsync(CancellationToken.None);
        TimeSpan elapsed = DateTimeOffset.UtcNow - startedAt;

        Assert.Equal(2, handler.RecordedRequests.Count);
        Assert.True(elapsed >= TimeSpan.FromMilliseconds(40), $"Expected to wait Retry-After; elapsed={elapsed}");
    }
}

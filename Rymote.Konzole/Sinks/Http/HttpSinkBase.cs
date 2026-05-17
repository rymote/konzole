using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Diagnostics;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks;

namespace Rymote.Konzole.Sinks.Http;

public abstract class HttpSinkBase<TOptions> : SinkBase<TOptions>
    where TOptions : HttpSinkOptionsBase
{
    protected HttpClient HttpClient { get; }

    protected override int BatchSize => Options.BatchSize;

    protected HttpSinkBase(TOptions options, HttpClient httpClient) : base(options)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        if (HttpClient.Timeout == System.Threading.Timeout.InfiniteTimeSpan || HttpClient.Timeout == TimeSpan.Zero)
            HttpClient.Timeout = options.RequestTimeout;
    }

    protected abstract HttpRequestMessage BuildRequest(IReadOnlyList<LogEntry> batch);

    protected sealed override async ValueTask WriteBatchAsync(IReadOnlyList<LogEntry> batch, CancellationToken cancellationToken)
    {
        int attemptIndex = 0;

        while (true)
        {
            HttpRequestMessage request = BuildRequest(batch);
            HttpResponseMessage? response = null;
            Exception? transientException = null;

            try
            {
                response = await HttpClient.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException requestException) { transientException = requestException; }
            catch (TaskCanceledException taskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                transientException = taskCanceledException;
            }

            if (response != null && response.IsSuccessStatusCode)
            {
                response.Dispose();
                return;
            }

            bool isRetryable = transientException != null
                || (response != null && IsRetryableStatus(response.StatusCode));

            if (!isRetryable)
            {
                int statusCode = response != null ? (int)response.StatusCode : 0;
                response?.Dispose();
                KonzoleDiagnostics.ReportSinkError(new SinkErrorEventArgs(
                    Name,
                    $"Non-retryable response (status={statusCode}); dropped {batch.Count} entries.",
                    droppedEntries: batch.Count));
                return;
            }

            if (attemptIndex >= Options.MaxRetryAttempts)
            {
                int statusCode = response != null ? (int)response.StatusCode : 0;
                response?.Dispose();
                KonzoleDiagnostics.ReportSinkError(new SinkErrorEventArgs(
                    Name,
                    $"Exhausted {Options.MaxRetryAttempts} retries (last status={statusCode}); dropped {batch.Count} entries.",
                    transientException,
                    droppedEntries: batch.Count));
                return;
            }

            TimeSpan delay = ComputeBackoffDelay(attemptIndex, response);
            response?.Dispose();
            await Task.Delay(delay, cancellationToken);
            attemptIndex++;
        }
    }

    private static bool IsRetryableStatus(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.RequestTimeout
        || statusCode == HttpStatusCode.TooManyRequests
        || (int)statusCode >= 500;

    private TimeSpan ComputeBackoffDelay(int attemptIndex, HttpResponseMessage? response)
    {
        if (response?.StatusCode == HttpStatusCode.TooManyRequests
            && response.Headers.RetryAfter?.Delta is TimeSpan retryAfter)
        {
            return retryAfter;
        }

        double multiplier = Math.Pow(2, attemptIndex);
        TimeSpan exponentialDelay = TimeSpan.FromMilliseconds(Options.BaseRetryDelay.TotalMilliseconds * multiplier);
        return exponentialDelay > Options.MaxRetryDelay ? Options.MaxRetryDelay : exponentialDelay;
    }
}

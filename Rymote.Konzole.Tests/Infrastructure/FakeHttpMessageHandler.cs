using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;

namespace Rymote.Konzole.Tests.Infrastructure;

public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly ConcurrentQueue<Func<HttpRequestMessage, HttpResponseMessage>> _responseFactories = new();
    private readonly ConcurrentQueue<HttpRequestMessage> _recordedRequests = new();

    public IReadOnlyList<HttpRequestMessage> RecordedRequests => _recordedRequests.ToArray();

    public void EnqueueResponse(HttpResponseMessage response) =>
        _responseFactories.Enqueue(_ => response);

    public void EnqueueResponse(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) =>
        _responseFactories.Enqueue(responseFactory);

    public void EnqueueException(Exception exception) =>
        _responseFactories.Enqueue(_ => throw exception);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _recordedRequests.Enqueue(request);

        if (!_responseFactories.TryDequeue(out Func<HttpRequestMessage, HttpResponseMessage>? factory))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }

        return Task.FromResult(factory(request));
    }
}

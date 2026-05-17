using System.Net;
using System.Net.Http;
using Xunit;

namespace Rymote.Konzole.Tests.Infrastructure;

public class FakeHttpMessageHandlerTests
{
    [Fact]
    public async Task QueuedResponses_AreReturnedInOrder_AndRequestsRecorded()
    {
        FakeHttpMessageHandler handler = new();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK));
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        using HttpClient httpClient = new(handler);

        HttpResponseMessage firstResponse = await httpClient.GetAsync("https://example.test/first");
        HttpResponseMessage secondResponse = await httpClient.GetAsync("https://example.test/second");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);
        Assert.Equal(2, handler.RecordedRequests.Count);
        Assert.Equal("https://example.test/first", handler.RecordedRequests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task EnqueuedException_IsThrownOnNextSend()
    {
        FakeHttpMessageHandler handler = new();
        handler.EnqueueException(new HttpRequestException("network down"));

        using HttpClient httpClient = new(handler);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => httpClient.GetAsync("https://example.test/x"));
    }
}

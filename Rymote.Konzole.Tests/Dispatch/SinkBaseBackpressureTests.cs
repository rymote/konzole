using Microsoft.Extensions.Logging;
using Rymote.Konzole.Models;
using Rymote.Konzole.Tests.Infrastructure;
using Xunit;

namespace Rymote.Konzole.Tests.Dispatch;

public class SinkBaseBackpressureTests
{
    [Fact]
    public async Task TryEnqueue_DeliversEntriesToWorker_AndFlushDrains()
    {
        FakeSinkOptions sinkOptions = new() { MaxQueueSize = 100, ShutdownTimeout = TimeSpan.FromSeconds(2) };
        await using FakeSink fakeSink = new(sinkOptions);

        for (int index = 0; index < 25; index++)
        {
            fakeSink.TryEnqueue(new LogEntry { Level = LogLevel.Information, Message = $"entry-{index}" });
        }

        await fakeSink.FlushAsync(CancellationToken.None);

        Assert.Equal(25, fakeSink.CapturedEntries.Count);
    }

    [Fact]
    public async Task TryEnqueue_FiltersBelowMinimumLevel()
    {
        FakeSinkOptions sinkOptions = new() { MinimumLevel = LogLevel.Warning };
        await using FakeSink fakeSink = new(sinkOptions);

        fakeSink.TryEnqueue(new LogEntry { Level = LogLevel.Information, Message = "ignored" });
        fakeSink.TryEnqueue(new LogEntry { Level = LogLevel.Warning, Message = "kept" });

        await fakeSink.FlushAsync(CancellationToken.None);

        Assert.Single(fakeSink.CapturedEntries);
        Assert.Equal("kept", fakeSink.CapturedEntries[0].Message);
    }

    [Fact]
    public async Task TryEnqueue_DropsOldest_WhenQueueFull()
    {
        FakeSinkOptions sinkOptions = new()
        {
            MaxQueueSize = 4,
            WriteDelay = TimeSpan.FromMilliseconds(50)
        };
        await using FakeSink fakeSink = new(sinkOptions);

        for (int index = 0; index < 200; index++)
        {
            fakeSink.TryEnqueue(new LogEntry { Level = LogLevel.Information, Message = $"entry-{index}" });
        }

        await fakeSink.FlushAsync(CancellationToken.None);

        Assert.True(fakeSink.CapturedEntries.Count < 200, "DropOldest should have shed entries under pressure.");
    }
}

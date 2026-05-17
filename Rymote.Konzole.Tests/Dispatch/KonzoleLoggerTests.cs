using Microsoft.Extensions.Logging;
using Rymote.Konzole;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks;
using Rymote.Konzole.Tests.Infrastructure;
using Xunit;

namespace Rymote.Konzole.Tests.Dispatch;

public class KonzoleLoggerTests
{
    [Fact]
    public async Task IsEnabled_ReturnsTrue_OnlyWhenSomeSinkAcceptsLevel()
    {
        await using FakeSink informationSink = new(new FakeSinkOptions { MinimumLevel = LogLevel.Information });
        await using FakeSink errorOnlySink = new(new FakeSinkOptions { MinimumLevel = LogLevel.Error });

        KonzoleLogger logger = new("Test.Category", new ISink[] { errorOnlySink });
        Assert.False(logger.IsEnabled(LogLevel.Information));
        Assert.True (logger.IsEnabled(LogLevel.Error));

        KonzoleLogger broader = new("Test.Category", new ISink[] { informationSink, errorOnlySink });
        Assert.True (broader.IsEnabled(LogLevel.Information));
        Assert.True (broader.IsEnabled(LogLevel.Error));
    }

    [Fact]
    public async Task Log_DispatchesToAllSinks_WithTagFromScope()
    {
        await using FakeSink sink = new(new FakeSinkOptions { MinimumLevel = LogLevel.Trace });
        KonzoleLogger logger = new("Test.Category", new ISink[] { sink });

        using (KonzoleScopeState.Push(new KonzoleScopeState { Tag = KonzoleTag.Success }))
        {
            logger.LogInformation("scoped");
        }

        await sink.FlushAsync(CancellationToken.None);

        Assert.Single(sink.CapturedEntries);
        Assert.Equal(KonzoleTag.Success, sink.CapturedEntries[0].Tag);
        Assert.Equal("scoped", sink.CapturedEntries[0].Message);
    }

    [Fact]
    public async Task Log_ExtractsStructuredProperties_FromState()
    {
        await using FakeSink sink = new(new FakeSinkOptions { MinimumLevel = LogLevel.Trace });
        KonzoleLogger logger = new("Test.Category", new ISink[] { sink });

        logger.LogInformation("user {UserId} did {Action}", 42, "login");
        await sink.FlushAsync(CancellationToken.None);

        Assert.Single(sink.CapturedEntries);
        LogEntry captured = sink.CapturedEntries[0];
        Assert.NotNull(captured.Properties);
        Assert.Equal(42, captured.Properties!["UserId"]);
        Assert.Equal("login", captured.Properties["Action"]);
    }
}

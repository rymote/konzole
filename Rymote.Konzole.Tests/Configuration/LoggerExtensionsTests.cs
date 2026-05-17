using Microsoft.Extensions.Logging;
using Rymote.Konzole;
using Rymote.Konzole.Extensions;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks;
using Rymote.Konzole.Tests.Infrastructure;
using Xunit;

namespace Rymote.Konzole.Tests.Configuration;

public class LoggerExtensionsTests
{
    [Fact]
    public async Task LogSuccess_TagsEntryWithSuccess_AndLogsInformation()
    {
        await using FakeSink sink = new(new FakeSinkOptions { MinimumLevel = LogLevel.Trace });
        KonzoleLogger logger = new("Test.Category", new ISink[] { sink });

        logger.LogSuccess("operation done");
        await sink.FlushAsync(CancellationToken.None);

        Assert.Single(sink.CapturedEntries);
        Assert.Equal(LogLevel.Information, sink.CapturedEntries[0].Level);
        Assert.Equal(KonzoleTag.Success, sink.CapturedEntries[0].Tag);
    }

    [Fact]
    public async Task LogFatal_MapsToCritical_WithoutTag()
    {
        await using FakeSink sink = new(new FakeSinkOptions { MinimumLevel = LogLevel.Trace });
        KonzoleLogger logger = new("Test.Category", new ISink[] { sink });

        logger.LogFatal(new InvalidOperationException("boom"), "fatal");
        await sink.FlushAsync(CancellationToken.None);

        Assert.Single(sink.CapturedEntries);
        Assert.Equal(LogLevel.Critical, sink.CapturedEntries[0].Level);
        Assert.Null(sink.CapturedEntries[0].Tag);
    }
}

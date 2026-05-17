using Microsoft.Extensions.Logging;
using Rymote.Konzole;
using Rymote.Konzole.Sinks;
using Rymote.Konzole.Tests.Infrastructure;
using Xunit;

namespace Rymote.Konzole.Tests.Dispatch;

public class GracefulShutdownTests
{
    [Fact]
    public async Task DisposeAsync_FlushesAllSinks_BeforeReturning()
    {
        FakeSink sink = new(new FakeSinkOptions
        {
            ShutdownTimeout = TimeSpan.FromSeconds(2),
            WriteDelay = TimeSpan.FromMilliseconds(10)
        });

        KonzoleLoggerProvider provider = new(new ISink[] { sink });
        ILogger logger = provider.CreateLogger("Test.Category");

        for (int index = 0; index < 50; index++)
        {
            logger.LogInformation("entry-{Index}", index);
        }

        await provider.DisposeAsync();

        Assert.Equal(50, sink.CapturedEntries.Count);
    }
}

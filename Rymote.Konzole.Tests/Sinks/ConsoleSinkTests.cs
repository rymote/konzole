using Microsoft.Extensions.Logging;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks;
using Xunit;

namespace Rymote.Konzole.Tests.Sinks;

public class ConsoleSinkTests
{
    [Fact]
    public async Task WritesToStandardOut_ForInformation()
    {
        StringWriter capturedStandardOut = new();
        TextWriter originalStandardOut = Console.Out;
        Console.SetOut(capturedStandardOut);

        try
        {
            ConsoleSinkOptions options = new() { UseColors = false, UseEmojis = false, ShowTimestamp = false, ShowCategory = false };
            await using ConsoleSink sink = new(options);

            sink.TryEnqueue(new LogEntry { Level = LogLevel.Information, Message = "hello-stdout" });
            await sink.FlushAsync(CancellationToken.None);
        }
        finally
        {
            Console.SetOut(originalStandardOut);
        }

        Assert.Contains("hello-stdout", capturedStandardOut.ToString());
    }

    [Fact]
    public async Task WritesToStandardError_ForErrorLevel()
    {
        StringWriter capturedStandardError = new();
        TextWriter originalStandardError = Console.Error;
        Console.SetError(capturedStandardError);

        try
        {
            ConsoleSinkOptions options = new() { UseColors = false, UseEmojis = false, ShowTimestamp = false, ShowCategory = false };
            await using ConsoleSink sink = new(options);

            sink.TryEnqueue(new LogEntry { Level = LogLevel.Error, Message = "hello-stderr" });
            await sink.FlushAsync(CancellationToken.None);
        }
        finally
        {
            Console.SetError(originalStandardError);
        }

        Assert.Contains("hello-stderr", capturedStandardError.ToString());
    }
}

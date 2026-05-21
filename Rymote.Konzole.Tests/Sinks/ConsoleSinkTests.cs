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

    [Fact]
    public async Task EmitsAnsiEscapes_ForInformationLevel()
    {
        StringWriter capturedStandardOut = new();
        TextWriter originalStandardOut = Console.Out;
        Console.SetOut(capturedStandardOut);

        try
        {
            ConsoleSinkOptions options = new() { UseColors = true, UseEmojis = false, ShowTimestamp = false, ShowCategory = false };
            await using ConsoleSink sink = new(options);

            sink.TryEnqueue(new LogEntry { Level = LogLevel.Information, Message = "hello" });
            await sink.FlushAsync(CancellationToken.None);
        }
        finally
        {
            Console.SetOut(originalStandardOut);
        }

        string output = capturedStandardOut.ToString();
        Assert.Contains("\x1b[", output);
        Assert.Contains("hello", output);
        Assert.DoesNotContain("[message]", output);
    }

    [Fact]
    public async Task EmitsAnsiEscapes_ForUserInlineMarkup()
    {
        StringWriter capturedStandardOut = new();
        TextWriter originalStandardOut = Console.Out;
        Console.SetOut(capturedStandardOut);

        try
        {
            ConsoleSinkOptions options = new() { UseColors = true, UseEmojis = false, ShowTimestamp = false, ShowCategory = false };
            await using ConsoleSink sink = new(options);

            sink.TryEnqueue(new LogEntry { Level = LogLevel.Information, Message = "User [bold]Alice[/] in" });
            await sink.FlushAsync(CancellationToken.None);
        }
        finally
        {
            Console.SetOut(originalStandardOut);
        }

        string output = capturedStandardOut.ToString();
        Assert.Contains("\x1b[1m", output);
        Assert.Contains("Alice", output);
        Assert.DoesNotContain("[bold]", output);
        Assert.DoesNotContain("[/]", output);
    }

    [Fact]
    public async Task EmitsAnsiEscapes_ForLevelLabel()
    {
        StringWriter capturedStandardError = new();
        TextWriter originalStandardError = Console.Error;
        Console.SetError(capturedStandardError);

        try
        {
            ConsoleSinkOptions options = new()
            {
                UseColors = true,
                UseEmojis = false,
                ShowIcon = false,
                ShowLevelLabel = true,
                ShowTimestamp = false,
                ShowCategory = false
            };
            await using ConsoleSink sink = new(options);

            sink.TryEnqueue(new LogEntry { Level = LogLevel.Error, Message = "boom" });
            await sink.FlushAsync(CancellationToken.None);
        }
        finally
        {
            Console.SetError(originalStandardError);
        }

        string output = capturedStandardError.ToString();
        Assert.True(output.Contains("\x1b[1m") || output.Contains("\x1b[1;"),
            $"Expected bold ANSI sequence in output. Actual: {output}");
        Assert.Contains("[ERROR]", output);
        Assert.DoesNotContain("[level-label]", output);
    }
}

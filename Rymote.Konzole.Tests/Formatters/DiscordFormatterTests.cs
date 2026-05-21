using Microsoft.Extensions.Logging;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;
using Xunit;

namespace Rymote.Konzole.Tests.Formatters;

public class DiscordFormatterTests
{
    private static readonly FormatterContext PlainContext = new()
    {
        ShowTimestamp = false,
        ShowCategory = false,
        ShowScope = false,
        ShowException = false
    };

    [Fact]
    public void Format_RendersLevelInBold_AndMessageOnNewLine()
    {
        DiscordFormatter formatter = new();
        LogEntry entry = new() { Level = LogLevel.Warning, Message = "watch out" };

        string rendered = formatter.Format(entry, PlainContext);

        Assert.Contains("**Warning**", rendered);
        Assert.Contains("watch out", rendered);
    }

    [Fact]
    public void Format_UsesTagIcon_WhenTagSet()
    {
        DiscordFormatter formatter = new();
        LogEntry entry = new() { Level = LogLevel.Information, Tag = KonzoleTag.Success, Message = "done" };

        string rendered = formatter.Format(entry, PlainContext);

        Assert.StartsWith("✅", rendered);
    }

    [Fact]
    public void Format_StripsStyleMarkup_FromMessage()
    {
        DiscordFormatter formatter = new();
        LogEntry entry = new() { Level = LogLevel.Warning, Message = "[red]careful[/]" };
        string rendered = formatter.Format(entry, PlainContext);
        Assert.DoesNotContain("[red]", rendered);
        Assert.DoesNotContain("[/]", rendered);
        Assert.Contains("careful", rendered);
    }
}

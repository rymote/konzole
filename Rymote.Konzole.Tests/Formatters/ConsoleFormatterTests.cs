using Microsoft.Extensions.Logging;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;
using Xunit;

namespace Rymote.Konzole.Tests.Formatters;

public class ConsoleFormatterTests
{
    private static readonly FormatterContext PlainContext = new()
    {
        ShowTimestamp = false,
        ShowCategory = false,
        ShowScope = false,
        ShowException = false
    };

    [Fact]
    public void Format_UsesLevelFallbackIcon_WhenNoTag()
    {
        ConsoleFormatter formatter = new(useEmojis: false);
        LogEntry entry = new() { Level = LogLevel.Information, Message = "hello" };

        string rendered = formatter.Format(entry, PlainContext);

        Assert.Contains("[INFO]", rendered);
        Assert.Contains("hello", rendered);
    }

    [Fact]
    public void Format_UsesTagFallbackIcon_WhenTagSet()
    {
        ConsoleFormatter formatter = new(useEmojis: false);
        LogEntry entry = new() { Level = LogLevel.Information, Tag = KonzoleTag.Success, Message = "ok" };

        string rendered = formatter.Format(entry, PlainContext);

        Assert.Contains("[SUCCESS]", rendered);
        Assert.DoesNotContain("[INFO]", rendered);
    }

    [Fact]
    public void Format_TruncatesMessage_AtMaxMessageLength()
    {
        ConsoleFormatter formatter = new(useEmojis: false);
        FormatterContext context = new() { MaxMessageLength = 10, ShowTimestamp = false, ShowCategory = false };
        LogEntry entry = new() { Level = LogLevel.Information, Message = "abcdefghijklmnop" };

        string rendered = formatter.Format(entry, context);

        Assert.Contains("abcdefg...", rendered);
    }

    [Fact]
    public void Format_RendersProperties_InParens()
    {
        ConsoleFormatter formatter = new(useEmojis: false);
        Dictionary<string, object?> properties = new() { ["userId"] = 42, ["action"] = "login" };
        LogEntry entry = new()
        {
            Level = LogLevel.Information,
            Message = "event",
            Properties = properties
        };

        string rendered = formatter.Format(entry, PlainContext);

        Assert.Contains("[property-key]userId[/]: [property-value]42[/]", rendered);
        Assert.Contains("[property-key]action[/]: [property-value]login[/]", rendered);
    }
}

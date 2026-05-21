using Microsoft.Extensions.Logging;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;
using Xunit;

namespace Rymote.Konzole.Tests.Formatters;

public class ConsoleFormatterStyledTests
{
    private static readonly FormatterContext PlainContext = new()
    {
        ShowTimestamp = false,
        ShowCategory = false,
        ShowScope = false,
        ShowException = false
    };

    [Fact]
    public void Format_InformationMessage_WrapsInMessageSegment()
    {
        ConsoleFormatter formatter = new(useEmojis: false);
        LogEntry entry = new() { Level = LogLevel.Information, Message = "hello" };

        string rendered = formatter.Format(entry, PlainContext);

        Assert.Contains("[message]hello[/]", rendered);
    }

    [Fact]
    public void Format_WarningMessage_WrapsInMessageWarningSegment()
    {
        ConsoleFormatter formatter = new(useEmojis: false);
        LogEntry entry = new() { Level = LogLevel.Warning, Message = "watch out" };
        string rendered = formatter.Format(entry, PlainContext);
        Assert.Contains("[message-warning]watch out[/]", rendered);
    }

    [Fact]
    public void Format_ErrorMessage_WrapsInMessageErrorSegment()
    {
        ConsoleFormatter formatter = new(useEmojis: false);
        LogEntry entry = new() { Level = LogLevel.Error, Message = "boom" };
        string rendered = formatter.Format(entry, PlainContext);
        Assert.Contains("[message-error]boom[/]", rendered);
    }

    [Fact]
    public void Format_PreservesUserInlineMarkup_InsideMessageSegment()
    {
        ConsoleFormatter formatter = new(useEmojis: false);
        LogEntry entry = new() { Level = LogLevel.Information, Message = "User [bold]Alice[/] joined" };
        string rendered = formatter.Format(entry, PlainContext);
        Assert.Contains("[message]User [bold]Alice[/] joined[/]", rendered);
    }

    [Fact]
    public void Format_WithTimestamp_WrapsTimestampInTimestampSegment()
    {
        ConsoleFormatter formatter = new(useEmojis: false);
        FormatterContext context = new() { ShowTimestamp = true, ShowCategory = false, ShowScope = false, ShowException = false };
        LogEntry entry = new()
        {
            Level = LogLevel.Information,
            Timestamp = new DateTimeOffset(2026, 5, 21, 12, 0, 0, TimeSpan.Zero),
            Message = "x"
        };
        string rendered = formatter.Format(entry, context);
        Assert.Contains("[timestamp]", rendered);
        Assert.Contains("2026-05-21 12:00:00", rendered);
    }

    [Fact]
    public void Format_WithCategory_WrapsInCategorySegment()
    {
        ConsoleFormatter formatter = new(useEmojis: false);
        FormatterContext context = new() { ShowTimestamp = false, ShowCategory = true, ShowScope = false, ShowException = false };
        LogEntry entry = new() { Level = LogLevel.Information, Category = "App.Service", Message = "x" };
        string rendered = formatter.Format(entry, context);
        Assert.Contains("[category]", rendered);
        Assert.Contains("App.Service", rendered);
    }

    [Fact]
    public void Format_WithShowLevelLabelTrue_EmitsLevelLabelSegment()
    {
        ConsoleFormatter formatter = new(useEmojis: false, showIcon: false, showLevelLabel: true);
        LogEntry entry = new() { Level = LogLevel.Information, Message = "x" };
        string rendered = formatter.Format(entry, PlainContext);
        Assert.Contains("[level-label][INFO][/]", rendered);
    }

    [Fact]
    public void Format_WithShowLevelLabelFalse_OmitsLevelLabelSegment()
    {
        ConsoleFormatter formatter = new(useEmojis: false, showIcon: true, showLevelLabel: false);
        LogEntry entry = new() { Level = LogLevel.Information, Message = "x" };
        string rendered = formatter.Format(entry, PlainContext);
        Assert.DoesNotContain("[level-label]", rendered);
    }

    [Fact]
    public void Format_WithShowIconFalse_OmitsIconSegment()
    {
        ConsoleFormatter formatter = new(useEmojis: true, showIcon: false, showLevelLabel: true);
        LogEntry entry = new() { Level = LogLevel.Information, Message = "x" };
        string rendered = formatter.Format(entry, PlainContext);
        Assert.DoesNotContain("[icon]", rendered);
    }

    [Fact]
    public void Format_LevelLabel_UsesTagText_WhenTagSet()
    {
        ConsoleFormatter formatter = new(useEmojis: false, showIcon: false, showLevelLabel: true);
        LogEntry entry = new() { Level = LogLevel.Information, Tag = KonzoleTag.Success, Message = "x" };
        string rendered = formatter.Format(entry, PlainContext);
        Assert.Contains("[level-label][SUCCESS][/]", rendered);
        Assert.DoesNotContain("[INFO]", rendered);
    }

    [Fact]
    public void Format_BothShowIconAndShowLevelLabelFalse_EmitsNeither()
    {
        ConsoleFormatter formatter = new(useEmojis: true, showIcon: false, showLevelLabel: false);
        LogEntry entry = new() { Level = LogLevel.Information, Message = "x" };
        string rendered = formatter.Format(entry, PlainContext);
        Assert.DoesNotContain("[icon]", rendered);
        Assert.DoesNotContain("[level-label]", rendered);
    }
}

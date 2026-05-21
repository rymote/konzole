using Rymote.Konzole.Styling;
using Xunit;

namespace Rymote.Konzole.Tests.Styling;

public class StyleMarkupTests
{
    [Fact]
    public void ToAnsi_NoOptions_ConvertsBasicTag()
    {
        Assert.Equal("\x1b[31merror\x1b[0m\x1b[0m", StyleMarkup.ToAnsi("[red]error[/]"));
    }

    [Fact]
    public void ToAnsi_WithBaseStyle_AppliesAround()
    {
        AnsiOptions options = new() { BaseStyle = new AnsiStyle { Foreground = AnsiColor.Named(AnsiNamedColor.Cyan) } };
        string result = StyleMarkup.ToAnsi("hello", options);
        Assert.StartsWith("\x1b[36m", result);
        Assert.EndsWith("\x1b[0m", result);
        Assert.Contains("hello", result);
    }

    [Fact]
    public void ToAnsi_WithSegmentPalette_ResolvesNamedSegment()
    {
        AnsiOptions options = new()
        {
            SegmentPalette = new Dictionary<string, AnsiStyle>(StringComparer.OrdinalIgnoreCase)
            {
                ["timestamp"] = new AnsiStyle { Foreground = AnsiColor.Named(AnsiNamedColor.BrightBlack) }
            }
        };
        string result = StyleMarkup.ToAnsi("[timestamp]12:00:00[/]", options);
        Assert.Contains("\x1b[90m", result);
        Assert.Contains("12:00:00", result);
    }

    [Fact]
    public void Strip_RemovesTagsAndKeepsLiterals()
    {
        Assert.Equal("[2026-05-21] error", StyleMarkup.Strip("[2026-05-21] [red]error[/]"));
    }

    [Fact]
    public void Strip_LeavesPlainText_Untouched()
    {
        Assert.Equal("plain text", StyleMarkup.Strip("plain text"));
    }
}

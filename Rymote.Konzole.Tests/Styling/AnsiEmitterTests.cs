using Rymote.Konzole.Styling;
using Xunit;

namespace Rymote.Konzole.Tests.Styling;

public class AnsiEmitterTests
{
    private const string Reset = "\x1b[0m";

    private static string Emit(string input, AnsiEmitContext? context = null) =>
        AnsiEmitter.Emit(StyleParser.Tokenize(input), context ?? new AnsiEmitContext());

    [Fact]
    public void Red_WrapsTextWithSGR31_AndReset()
    {
        Assert.Equal("\x1b[31merror" + Reset + Reset, Emit("[red]error[/]"));
    }

    [Fact]
    public void BoldRed_CombinesSGRCodes_BoldFirst()
    {
        Assert.Equal("\x1b[1;31mcritical" + Reset + Reset, Emit("[bold red]critical[/]"));
    }

    [Fact]
    public void Truecolor_UsesSGR38_2()
    {
        Assert.Equal("\x1b[38;2;255;136;0mwarn" + Reset + Reset, Emit("[#ff8800]warn[/]"));
    }

    [Fact]
    public void Color256_UsesSGR38_5()
    {
        Assert.Equal("\x1b[38;5;208mwarn" + Reset + Reset, Emit("[color:208]warn[/]"));
    }

    [Fact]
    public void NamedBackground_UsesSGR43()
    {
        Assert.Equal("\x1b[43mwarn" + Reset + Reset, Emit("[bg:yellow]warn[/]"));
    }

    [Fact]
    public void HexBackground_UsesSGR48_2()
    {
        Assert.Equal("\x1b[48;2;255;136;0mwarn" + Reset + Reset, Emit("[bg:#ff8800]warn[/]"));
    }

    [Fact]
    public void Italic_Underline_Dim_Reverse_AllMapToTheirSGR()
    {
        Assert.Contains("\x1b[3m", Emit("[italic]x[/]"));
        Assert.Contains("\x1b[4m", Emit("[underline]x[/]"));
        Assert.Contains("\x1b[2m", Emit("[dim]x[/]"));
        Assert.Contains("\x1b[7m", Emit("[reverse]x[/]"));
    }

    [Fact]
    public void UnknownTag_PassesThroughAsLiteral()
    {
        Assert.Equal("[Login] complete", Emit("[Login] complete"));
    }

    [Fact]
    public void UnknownTag_WithCloseAfter_StillLiteral_BothBracketsAndCloseShown()
    {
        Assert.Equal("[Login] complete", Emit("[Login] complete[/]"));
    }

    [Fact]
    public void BaseStyle_Applied_AndRestoredAfterCloseTag()
    {
        AnsiEmitContext context = new()
        {
            BaseStyle = new AnsiStyle { Foreground = AnsiColor.Named(AnsiNamedColor.Cyan) }
        };
        string result = AnsiEmitter.Emit(StyleParser.Tokenize("hello [bold]you[/] there"), context);
        Assert.StartsWith("\x1b[36m", result);
        Assert.Contains("\x1b[1;36m", result);
        Assert.EndsWith(Reset, result);
    }

    [Fact]
    public void SegmentPalette_ResolvesTagBody_BeforeStyleGrammar()
    {
        AnsiEmitContext context = new()
        {
            SegmentPalette = new Dictionary<string, AnsiStyle>(StringComparer.OrdinalIgnoreCase)
            {
                ["timestamp"] = new AnsiStyle { Foreground = AnsiColor.Named(AnsiNamedColor.BrightBlack) }
            }
        };
        string result = AnsiEmitter.Emit(StyleParser.Tokenize("[timestamp]12:00:00[/]"), context);
        Assert.Contains("\x1b[90m", result);
        Assert.Contains("12:00:00", result);
    }

    [Fact]
    public void DoubleBracketEscape_ResultsInLiteralOpenBracket()
    {
        Assert.Equal("[red] text", Emit("[[red] text"));
    }

    [Fact]
    public void CaseInsensitive_TagRecognition()
    {
        Assert.Equal("\x1b[31mx" + Reset + Reset, Emit("[RED]x[/]"));
        Assert.Equal("\x1b[1;31mx" + Reset + Reset, Emit("[Bold Red]x[/]"));
    }

    [Fact]
    public void UnclosedTag_AtEndOfInput_StillResetsAtEnd()
    {
        string result = Emit("[red]unclosed");
        Assert.StartsWith("\x1b[31m", result);
        Assert.EndsWith(Reset, result);
    }
}

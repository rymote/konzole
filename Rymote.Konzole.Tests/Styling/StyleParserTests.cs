using Rymote.Konzole.Styling;
using Xunit;

namespace Rymote.Konzole.Tests.Styling;

public class StyleParserTests
{
    [Fact]
    public void Tokenize_PlainText_ProducesSingleTextToken()
    {
        IReadOnlyList<StyleToken> tokens = StyleParser.Tokenize("hello");
        Assert.Single(tokens);
        Assert.Equal(StyleTokenKind.Text, tokens[0].Kind);
        Assert.Equal("hello", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_OpenTag_ExtractsBody()
    {
        IReadOnlyList<StyleToken> tokens = StyleParser.Tokenize("[red]error[/]");
        Assert.Equal(3, tokens.Count);
        Assert.Equal(StyleTokenKind.OpenTag, tokens[0].Kind);
        Assert.Equal("red", tokens[0].Text);
        Assert.Equal(StyleTokenKind.Text, tokens[1].Kind);
        Assert.Equal("error", tokens[1].Text);
        Assert.Equal(StyleTokenKind.CloseTag, tokens[2].Kind);
    }

    [Fact]
    public void Tokenize_MultipleStyleTokensInOneTag_PreservesWhitespace()
    {
        IReadOnlyList<StyleToken> tokens = StyleParser.Tokenize("[bold red]x[/]");
        Assert.Equal(StyleTokenKind.OpenTag, tokens[0].Kind);
        Assert.Equal("bold red", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_DoubleBracket_EscapesToLiteralOpenBracket()
    {
        IReadOnlyList<StyleToken> tokens = StyleParser.Tokenize("[[red]");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(StyleTokenKind.Text, tokens[0].Kind);
        Assert.Equal("[", tokens[0].Text);
        Assert.Equal(StyleTokenKind.Text, tokens[1].Kind);
        Assert.Equal("red]", tokens[1].Text);
    }

    [Fact]
    public void Tokenize_NestedTags_ProducesOpenOpenTextCloseClose()
    {
        IReadOnlyList<StyleToken> tokens = StyleParser.Tokenize("[red][bold]x[/][/]");
        Assert.Equal(5, tokens.Count);
        Assert.Equal(StyleTokenKind.OpenTag, tokens[0].Kind);
        Assert.Equal("red", tokens[0].Text);
        Assert.Equal(StyleTokenKind.OpenTag, tokens[1].Kind);
        Assert.Equal("bold", tokens[1].Text);
        Assert.Equal(StyleTokenKind.Text, tokens[2].Kind);
        Assert.Equal("x", tokens[2].Text);
        Assert.Equal(StyleTokenKind.CloseTag, tokens[3].Kind);
        Assert.Equal(StyleTokenKind.CloseTag, tokens[4].Kind);
    }

    [Fact]
    public void Tokenize_BareCloseTag_ProducesCloseToken()
    {
        IReadOnlyList<StyleToken> tokens = StyleParser.Tokenize("[/]");
        Assert.Single(tokens);
        Assert.Equal(StyleTokenKind.CloseTag, tokens[0].Kind);
    }

    [Fact]
    public void Tokenize_UnterminatedOpenBracket_TreatedAsLiteralText()
    {
        IReadOnlyList<StyleToken> tokens = StyleParser.Tokenize("[red");
        Assert.Single(tokens);
        Assert.Equal(StyleTokenKind.Text, tokens[0].Kind);
        Assert.Equal("[red", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_EmptyBrackets_TreatedAsLiteralText()
    {
        IReadOnlyList<StyleToken> tokens = StyleParser.Tokenize("[] hello");
        Assert.Single(tokens);
        Assert.Equal(StyleTokenKind.Text, tokens[0].Kind);
        Assert.Equal("[] hello", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_BracketsAroundContent_ProducesOpenTagRegardlessOfBodyValidity()
    {
        IReadOnlyList<StyleToken> tokens = StyleParser.Tokenize("[2026-05-21] message");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(StyleTokenKind.OpenTag, tokens[0].Kind);
        Assert.Equal("2026-05-21", tokens[0].Text);
        Assert.Equal(StyleTokenKind.Text, tokens[1].Kind);
        Assert.Equal(" message", tokens[1].Text);
    }
}

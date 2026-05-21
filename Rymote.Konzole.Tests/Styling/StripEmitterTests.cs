using Rymote.Konzole.Styling;
using Xunit;

namespace Rymote.Konzole.Tests.Styling;

public class StripEmitterTests
{
    private static string Strip(string input) => StripEmitter.Emit(StyleParser.Tokenize(input));

    [Fact] public void RemovesValidTags() => Assert.Equal("error", Strip("[red]error[/]"));

    [Fact] public void KeepsUnknownTagsAsLiteral_OutsideValidGrammar()
        => Assert.Equal("[Login] complete", Strip("[Login] complete"));

    [Fact] public void KeepsDateBrackets_AsLiteral()
        => Assert.Equal("[2026-05-21] message", Strip("[2026-05-21] message"));

    [Fact] public void ResolvesDoubleBracketEscape_ToLiteralBracket()
        => Assert.Equal("[red] text", Strip("[[red] text"));

    [Fact] public void DropsBareCloseTag() => Assert.Equal("text", Strip("text[/]"));

    [Fact] public void NestedValidTags_FullyStripped()
        => Assert.Equal("error CRITICAL details", Strip("[red]error [bold]CRITICAL[/] details[/]"));

    [Fact] public void HexAndBackgroundTags_Stripped()
        => Assert.Equal("warn", Strip("[bg:#ff8800 white]warn[/]"));
}

using Rymote.Konzole.Styling;
using Xunit;

namespace Rymote.Konzole.Tests.Styling;

public class AnsiColorTests
{
    [Fact]
    public void Named_ProducesNamedKind()
    {
        AnsiColor color = AnsiColor.Named(AnsiNamedColor.Red);
        Assert.Equal(AnsiColorKind.Named, color.Kind);
        Assert.Equal(AnsiNamedColor.Red, color.NamedColor);
    }

    [Fact]
    public void Palette_ProducesPaletteKind()
    {
        AnsiColor color = AnsiColor.Palette(208);
        Assert.Equal(AnsiColorKind.Palette, color.Kind);
        Assert.Equal((byte)208, color.PaletteIndex);
    }

    [Fact]
    public void Truecolor_ProducesTruecolorKind()
    {
        AnsiColor color = AnsiColor.Truecolor(255, 136, 0);
        Assert.Equal(AnsiColorKind.Truecolor, color.Kind);
        Assert.Equal((byte)255, color.Red);
        Assert.Equal((byte)136, color.Green);
        Assert.Equal((byte)0, color.Blue);
    }

    [Theory]
    [InlineData(ConsoleColor.Black,       AnsiNamedColor.Black)]
    [InlineData(ConsoleColor.DarkRed,     AnsiNamedColor.Red)]
    [InlineData(ConsoleColor.DarkGreen,   AnsiNamedColor.Green)]
    [InlineData(ConsoleColor.DarkYellow,  AnsiNamedColor.Yellow)]
    [InlineData(ConsoleColor.DarkBlue,    AnsiNamedColor.Blue)]
    [InlineData(ConsoleColor.DarkMagenta, AnsiNamedColor.Magenta)]
    [InlineData(ConsoleColor.DarkCyan,    AnsiNamedColor.Cyan)]
    [InlineData(ConsoleColor.Gray,        AnsiNamedColor.White)]
    [InlineData(ConsoleColor.DarkGray,    AnsiNamedColor.BrightBlack)]
    [InlineData(ConsoleColor.Red,         AnsiNamedColor.BrightRed)]
    [InlineData(ConsoleColor.Green,       AnsiNamedColor.BrightGreen)]
    [InlineData(ConsoleColor.Yellow,      AnsiNamedColor.BrightYellow)]
    [InlineData(ConsoleColor.Blue,        AnsiNamedColor.BrightBlue)]
    [InlineData(ConsoleColor.Magenta,     AnsiNamedColor.BrightMagenta)]
    [InlineData(ConsoleColor.Cyan,        AnsiNamedColor.BrightCyan)]
    [InlineData(ConsoleColor.White,       AnsiNamedColor.BrightWhite)]
    public void ImplicitConversion_FromConsoleColor_MapsCorrectly(ConsoleColor input, AnsiNamedColor expected)
    {
        AnsiColor color = input;
        Assert.Equal(AnsiColorKind.Named, color.Kind);
        Assert.Equal(expected, color.NamedColor);
    }
}

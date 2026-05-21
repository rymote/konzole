namespace Rymote.Konzole.Styling;

public readonly record struct AnsiColor
{
    public AnsiColorKind Kind { get; init; }
    public byte Red { get; init; }
    public byte Green { get; init; }
    public byte Blue { get; init; }
    public byte PaletteIndex { get; init; }
    public AnsiNamedColor NamedColor { get; init; }

    public static AnsiColor Named(AnsiNamedColor namedColor) => new()
    {
        Kind = AnsiColorKind.Named,
        NamedColor = namedColor
    };

    public static AnsiColor Palette(byte paletteIndex) => new()
    {
        Kind = AnsiColorKind.Palette,
        PaletteIndex = paletteIndex
    };

    public static AnsiColor Truecolor(byte red, byte green, byte blue) => new()
    {
        Kind = AnsiColorKind.Truecolor,
        Red = red,
        Green = green,
        Blue = blue
    };

    public static implicit operator AnsiColor(ConsoleColor consoleColor) => consoleColor switch
    {
        ConsoleColor.Black       => Named(AnsiNamedColor.Black),
        ConsoleColor.DarkRed     => Named(AnsiNamedColor.Red),
        ConsoleColor.DarkGreen   => Named(AnsiNamedColor.Green),
        ConsoleColor.DarkYellow  => Named(AnsiNamedColor.Yellow),
        ConsoleColor.DarkBlue    => Named(AnsiNamedColor.Blue),
        ConsoleColor.DarkMagenta => Named(AnsiNamedColor.Magenta),
        ConsoleColor.DarkCyan    => Named(AnsiNamedColor.Cyan),
        ConsoleColor.Gray        => Named(AnsiNamedColor.White),
        ConsoleColor.DarkGray    => Named(AnsiNamedColor.BrightBlack),
        ConsoleColor.Red         => Named(AnsiNamedColor.BrightRed),
        ConsoleColor.Green       => Named(AnsiNamedColor.BrightGreen),
        ConsoleColor.Yellow      => Named(AnsiNamedColor.BrightYellow),
        ConsoleColor.Blue        => Named(AnsiNamedColor.BrightBlue),
        ConsoleColor.Magenta     => Named(AnsiNamedColor.BrightMagenta),
        ConsoleColor.Cyan        => Named(AnsiNamedColor.BrightCyan),
        ConsoleColor.White       => Named(AnsiNamedColor.BrightWhite),
        _                        => Named(AnsiNamedColor.White)
    };
}

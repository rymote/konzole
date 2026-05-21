namespace Rymote.Konzole.Styling;

public readonly record struct AnsiStyle
{
    public AnsiColor? Foreground { get; init; }
    public AnsiColor? Background { get; init; }
    public AnsiTextDecoration Decoration { get; init; }

    public static AnsiStyle Empty => default;

    public AnsiStyle MergeWith(AnsiStyle other) => new()
    {
        Foreground = other.Foreground ?? Foreground,
        Background = other.Background ?? Background,
        Decoration = Decoration | other.Decoration
    };
}

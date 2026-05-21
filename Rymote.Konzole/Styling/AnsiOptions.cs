namespace Rymote.Konzole.Styling;

public sealed class AnsiOptions
{
    public AnsiStyle? BaseStyle { get; init; }
    public IReadOnlyDictionary<string, AnsiStyle>? SegmentPalette { get; init; }
    public bool AppendFinalReset { get; init; } = true;
}

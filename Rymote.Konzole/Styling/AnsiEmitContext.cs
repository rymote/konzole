namespace Rymote.Konzole.Styling;

internal sealed class AnsiEmitContext
{
    public IReadOnlyDictionary<string, AnsiStyle>? SegmentPalette { get; init; }
    public AnsiStyle BaseStyle { get; init; } = AnsiStyle.Empty;
    public bool AppendFinalReset { get; init; } = true;
}

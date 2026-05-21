namespace Rymote.Konzole.Styling;

public static class StyleMarkup
{
    public static string ToAnsi(string input)
    {
        return AnsiEmitter.Emit(StyleParser.Tokenize(input), new AnsiEmitContext());
    }

    public static string ToAnsi(string input, AnsiOptions options)
    {
        AnsiEmitContext context = new()
        {
            BaseStyle = options.BaseStyle ?? AnsiStyle.Empty,
            SegmentPalette = options.SegmentPalette,
            AppendFinalReset = options.AppendFinalReset
        };
        return AnsiEmitter.Emit(StyleParser.Tokenize(input), context);
    }

    public static string Strip(string input)
    {
        return StripEmitter.Emit(StyleParser.Tokenize(input));
    }
}

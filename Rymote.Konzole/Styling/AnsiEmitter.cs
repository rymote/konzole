using System.Globalization;
using System.Text;

namespace Rymote.Konzole.Styling;

internal static class AnsiEmitter
{
    private const string Esc = "\x1b[";
    private const string Reset = "\x1b[0m";

    public static string Emit(IReadOnlyList<StyleToken> tokens, AnsiEmitContext context)
    {
        StringBuilder output = new();
        Stack<AnsiStyle> styleStack = new();
        styleStack.Push(context.BaseStyle);

        bool anyStyleEmitted = false;

        if (!IsEmpty(context.BaseStyle))
        {
            output.Append(EncodeSgr(context.BaseStyle));
            anyStyleEmitted = true;
        }

        foreach (StyleToken token in tokens)
        {
            switch (token.Kind)
            {
                case StyleTokenKind.Text:
                    output.Append(token.Text);
                    break;

                case StyleTokenKind.OpenTag:
                    if (!TryResolveStyle(token.Text, context.SegmentPalette, out AnsiStyle resolved))
                    {
                        output.Append('[').Append(token.Text).Append(']');
                        break;
                    }
                    AnsiStyle previous = styleStack.Peek();
                    AnsiStyle effective = previous.MergeWith(resolved);
                    styleStack.Push(effective);
                    if (!IsEmpty(previous)) output.Append(Reset);
                    output.Append(EncodeSgr(effective));
                    anyStyleEmitted = true;
                    break;

                case StyleTokenKind.CloseTag:
                    if (styleStack.Count <= 1) break;
                    styleStack.Pop();
                    AnsiStyle restored = styleStack.Peek();
                    output.Append(Reset);
                    if (!IsEmpty(restored)) output.Append(EncodeSgr(restored));
                    anyStyleEmitted = true;
                    break;
            }
        }

        if (context.AppendFinalReset && anyStyleEmitted) output.Append(Reset);
        return output.ToString();
    }

    private static bool IsEmpty(AnsiStyle style) =>
        style.Foreground == null && style.Background == null && style.Decoration == AnsiTextDecoration.None;

    private static string EncodeSgr(AnsiStyle style)
    {
        List<string> codes = new();

        if ((style.Decoration & AnsiTextDecoration.Bold) != 0) codes.Add("1");
        if ((style.Decoration & AnsiTextDecoration.Dim) != 0) codes.Add("2");
        if ((style.Decoration & AnsiTextDecoration.Italic) != 0) codes.Add("3");
        if ((style.Decoration & AnsiTextDecoration.Underline) != 0) codes.Add("4");
        if ((style.Decoration & AnsiTextDecoration.Reverse) != 0) codes.Add("7");

        if (style.Foreground.HasValue) codes.Add(EncodeColor(style.Foreground.Value, isBackground: false));
        if (style.Background.HasValue) codes.Add(EncodeColor(style.Background.Value, isBackground: true));

        return codes.Count == 0 ? string.Empty : Esc + string.Join(';', codes) + "m";
    }

    private static string EncodeColor(AnsiColor color, bool isBackground)
    {
        int baseCode = isBackground ? 40 : 30;
        int brightCode = isBackground ? 100 : 90;
        int extendedCode = isBackground ? 48 : 38;

        return color.Kind switch
        {
            AnsiColorKind.Named => color.NamedColor switch
            {
                AnsiNamedColor.Black         => (baseCode + 0).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.Red           => (baseCode + 1).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.Green         => (baseCode + 2).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.Yellow        => (baseCode + 3).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.Blue          => (baseCode + 4).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.Magenta       => (baseCode + 5).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.Cyan          => (baseCode + 6).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.White         => (baseCode + 7).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.BrightBlack   => (brightCode + 0).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.BrightRed     => (brightCode + 1).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.BrightGreen   => (brightCode + 2).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.BrightYellow  => (brightCode + 3).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.BrightBlue    => (brightCode + 4).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.BrightMagenta => (brightCode + 5).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.BrightCyan    => (brightCode + 6).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.BrightWhite   => (brightCode + 7).ToString(CultureInfo.InvariantCulture),
                _                            => (baseCode + 7).ToString(CultureInfo.InvariantCulture)
            },
            AnsiColorKind.Palette   => $"{extendedCode};5;{color.PaletteIndex}",
            AnsiColorKind.Truecolor => $"{extendedCode};2;{color.Red};{color.Green};{color.Blue}",
            _                        => string.Empty
        };
    }

    private static bool TryResolveStyle(
        string tagBody,
        IReadOnlyDictionary<string, AnsiStyle>? segmentPalette,
        out AnsiStyle style)
    {
        if (segmentPalette != null && segmentPalette.TryGetValue(tagBody, out style))
            return true;

        return StyleGrammar.TryParse(tagBody, out style);
    }
}

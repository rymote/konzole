using System.Globalization;

namespace Rymote.Konzole.Styling;

internal static class StyleGrammar
{
    public static bool TryParse(string tagBody, out AnsiStyle style)
    {
        style = AnsiStyle.Empty;
        if (string.IsNullOrWhiteSpace(tagBody)) return false;

        string[] tokens = tagBody.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        AnsiColor? foreground = null;
        AnsiColor? background = null;
        AnsiTextDecoration decoration = AnsiTextDecoration.None;

        foreach (string raw in tokens)
        {
            string token = raw.ToLowerInvariant();

            if (TryParseDecoration(token, out AnsiTextDecoration parsedDecoration))
            {
                decoration |= parsedDecoration;
                continue;
            }

            bool isBackground = token.StartsWith("bg:", StringComparison.Ordinal);
            string colorToken = isBackground ? token.Substring(3) : token;

            if (!TryParseColor(colorToken, out AnsiColor color))
                return false;

            if (isBackground) background = color;
            else foreground = color;
        }

        style = new AnsiStyle { Foreground = foreground, Background = background, Decoration = decoration };
        return foreground != null || background != null || decoration != AnsiTextDecoration.None;
    }

    private static bool TryParseDecoration(string token, out AnsiTextDecoration decoration)
    {
        switch (token)
        {
            case "bold":      decoration = AnsiTextDecoration.Bold;      return true;
            case "italic":    decoration = AnsiTextDecoration.Italic;    return true;
            case "underline": decoration = AnsiTextDecoration.Underline; return true;
            case "dim":       decoration = AnsiTextDecoration.Dim;       return true;
            case "reverse":   decoration = AnsiTextDecoration.Reverse;   return true;
            default:          decoration = AnsiTextDecoration.None;      return false;
        }
    }

    private static bool TryParseColor(string token, out AnsiColor color)
    {
        if (TryParseNamed(token, out AnsiNamedColor named))
        {
            color = AnsiColor.Named(named);
            return true;
        }

        if (token.StartsWith("color:", StringComparison.Ordinal))
        {
            if (byte.TryParse(token.AsSpan(6), NumberStyles.Integer, CultureInfo.InvariantCulture, out byte paletteIndex))
            {
                color = AnsiColor.Palette(paletteIndex);
                return true;
            }
        }

        if (token.Length == 7 && token[0] == '#'
            && byte.TryParse(token.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte red)
            && byte.TryParse(token.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte green)
            && byte.TryParse(token.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte blue))
        {
            color = AnsiColor.Truecolor(red, green, blue);
            return true;
        }

        color = default;
        return false;
    }

    private static bool TryParseNamed(string token, out AnsiNamedColor namedColor)
    {
        switch (token)
        {
            case "black":          namedColor = AnsiNamedColor.Black;         return true;
            case "red":            namedColor = AnsiNamedColor.Red;           return true;
            case "green":          namedColor = AnsiNamedColor.Green;         return true;
            case "yellow":         namedColor = AnsiNamedColor.Yellow;        return true;
            case "blue":           namedColor = AnsiNamedColor.Blue;          return true;
            case "magenta":        namedColor = AnsiNamedColor.Magenta;       return true;
            case "cyan":           namedColor = AnsiNamedColor.Cyan;          return true;
            case "white":          namedColor = AnsiNamedColor.White;         return true;
            case "bright_black":   namedColor = AnsiNamedColor.BrightBlack;   return true;
            case "bright_red":     namedColor = AnsiNamedColor.BrightRed;     return true;
            case "bright_green":   namedColor = AnsiNamedColor.BrightGreen;   return true;
            case "bright_yellow":  namedColor = AnsiNamedColor.BrightYellow;  return true;
            case "bright_blue":    namedColor = AnsiNamedColor.BrightBlue;    return true;
            case "bright_magenta": namedColor = AnsiNamedColor.BrightMagenta; return true;
            case "bright_cyan":    namedColor = AnsiNamedColor.BrightCyan;    return true;
            case "bright_white":   namedColor = AnsiNamedColor.BrightWhite;   return true;
            default:               namedColor = default;                      return false;
        }
    }
}

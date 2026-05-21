namespace Rymote.Konzole.Styling;

internal static class StyleParser
{
    public static IReadOnlyList<StyleToken> Tokenize(ReadOnlySpan<char> input)
    {
        List<StyleToken> tokens = new();
        int position = 0;
        int textRunStart = 0;

        while (position < input.Length)
        {
            char current = input[position];

            if (current != '[')
            {
                position++;
                continue;
            }

            if (position + 1 < input.Length && input[position + 1] == '[')
            {
                FlushTextRun(input, textRunStart, position, tokens);
                tokens.Add(StyleToken.TextRun("["));
                position += 2;
                textRunStart = position;
                continue;
            }

            if (position + 2 < input.Length && input[position + 1] == '/' && input[position + 2] == ']')
            {
                FlushTextRun(input, textRunStart, position, tokens);
                tokens.Add(StyleToken.Close());
                position += 3;
                textRunStart = position;
                continue;
            }

            int closeIndex = input.Slice(position + 1).IndexOf(']');
            if (closeIndex < 0)
            {
                position++;
                continue;
            }

            int bodyEnd = position + 1 + closeIndex;
            int bodyLength = bodyEnd - (position + 1);
            if (bodyLength == 0)
            {
                position++;
                continue;
            }

            ReadOnlySpan<char> body = input.Slice(position + 1, bodyLength);
            if (body.IndexOf('[') >= 0)
            {
                position++;
                continue;
            }

            FlushTextRun(input, textRunStart, position, tokens);
            tokens.Add(StyleToken.Open(body.ToString()));
            position = bodyEnd + 1;
            textRunStart = position;
        }

        FlushTextRun(input, textRunStart, input.Length, tokens);
        return tokens;
    }

    private static void FlushTextRun(ReadOnlySpan<char> input, int start, int end, List<StyleToken> tokens)
    {
        if (end <= start) return;
        tokens.Add(StyleToken.TextRun(input.Slice(start, end - start).ToString()));
    }
}

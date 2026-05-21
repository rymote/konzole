using System.Text;

namespace Rymote.Konzole.Styling;

internal static class StripEmitter
{
    public static string Emit(IReadOnlyList<StyleToken> tokens)
    {
        StringBuilder output = new();

        foreach (StyleToken token in tokens)
        {
            switch (token.Kind)
            {
                case StyleTokenKind.Text:
                    output.Append(token.Text);
                    break;

                case StyleTokenKind.OpenTag:
                    if (StyleGrammar.TryParse(token.Text, out _)) break;
                    output.Append('[').Append(token.Text).Append(']');
                    break;

                case StyleTokenKind.CloseTag:
                    break;
            }
        }

        return output.ToString();
    }
}

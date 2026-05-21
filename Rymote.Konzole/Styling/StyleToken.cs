namespace Rymote.Konzole.Styling;

internal readonly record struct StyleToken
{
    public StyleTokenKind Kind { get; init; }
    public string Text { get; init; }

    public static StyleToken TextRun(string text) => new() { Kind = StyleTokenKind.Text, Text = text };
    public static StyleToken Open(string body)    => new() { Kind = StyleTokenKind.OpenTag, Text = body };
    public static StyleToken Close()              => new() { Kind = StyleTokenKind.CloseTag, Text = string.Empty };
}

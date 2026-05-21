namespace Rymote.Konzole.Styling;

[Flags]
public enum AnsiTextDecoration
{
    None      = 0,
    Bold      = 1 << 0,
    Italic    = 1 << 1,
    Underline = 1 << 2,
    Dim       = 1 << 3,
    Reverse   = 1 << 4
}

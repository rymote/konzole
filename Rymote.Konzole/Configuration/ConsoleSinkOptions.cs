using Microsoft.Extensions.Logging;
using Rymote.Konzole.Models;
using Rymote.Konzole.Styling;

namespace Rymote.Konzole.Configuration;

public sealed class ConsoleSinkOptions : SinkOptionsBase
{
    public bool UseColors { get; set; } = true;
    public bool UseEmojis { get; set; } = true;
    public bool ShowIcon { get; set; } = true;
    public bool ShowLevelLabel { get; set; } = true;

    public IReadOnlyDictionary<LogLevel, AnsiColor> LevelColors { get; init; } = new Dictionary<LogLevel, AnsiColor>
    {
        [LogLevel.Trace]       = AnsiColor.Named(AnsiNamedColor.BrightBlack),
        [LogLevel.Debug]       = AnsiColor.Named(AnsiNamedColor.White),
        [LogLevel.Information] = AnsiColor.Named(AnsiNamedColor.BrightCyan),
        [LogLevel.Warning]     = AnsiColor.Named(AnsiNamedColor.BrightYellow),
        [LogLevel.Error]       = AnsiColor.Named(AnsiNamedColor.BrightRed),
        [LogLevel.Critical]    = AnsiColor.Named(AnsiNamedColor.BrightWhite)
    };

    public IReadOnlyDictionary<KonzoleTag, AnsiColor> TagColors { get; init; } = new Dictionary<KonzoleTag, AnsiColor>
    {
        [KonzoleTag.Success]  = AnsiColor.Named(AnsiNamedColor.BrightGreen),
        [KonzoleTag.Pending]  = AnsiColor.Named(AnsiNamedColor.BrightBlue),
        [KonzoleTag.Complete] = AnsiColor.Named(AnsiNamedColor.Green),
        [KonzoleTag.Note]     = AnsiColor.Named(AnsiNamedColor.BrightMagenta),
        [KonzoleTag.Start]    = AnsiColor.Named(AnsiNamedColor.Cyan),
        [KonzoleTag.Pause]    = AnsiColor.Named(AnsiNamedColor.Yellow),
        [KonzoleTag.Watch]    = AnsiColor.Named(AnsiNamedColor.Magenta)
    };

    public AnsiColor CriticalBackgroundColor { get; init; } = AnsiColor.Named(AnsiNamedColor.Red);

    public IReadOnlyDictionary<ConsoleSegment, AnsiStyle> SegmentStyles { get; init; } = new Dictionary<ConsoleSegment, AnsiStyle>
    {
        [ConsoleSegment.Timestamp]        = new() { Foreground = AnsiColor.Named(AnsiNamedColor.BrightBlack) },
        [ConsoleSegment.Category]         = new() { Foreground = AnsiColor.Named(AnsiNamedColor.Cyan) },
        [ConsoleSegment.EventId]          = new() { Foreground = AnsiColor.Named(AnsiNamedColor.BrightBlack) },
        [ConsoleSegment.Scope]            = new() { Foreground = AnsiColor.Named(AnsiNamedColor.BrightBlack), Decoration = AnsiTextDecoration.Italic },
        [ConsoleSegment.Message]          = AnsiStyle.Empty,
        [ConsoleSegment.MessageWarning]   = new() { Foreground = AnsiColor.Named(AnsiNamedColor.BrightYellow) },
        [ConsoleSegment.MessageError]     = new() { Foreground = AnsiColor.Named(AnsiNamedColor.BrightRed), Decoration = AnsiTextDecoration.Bold },
        [ConsoleSegment.LevelLabel]       = new() { Decoration = AnsiTextDecoration.Bold },
        [ConsoleSegment.PropertyKey]      = new() { Foreground = AnsiColor.Named(AnsiNamedColor.BrightBlack) },
        [ConsoleSegment.PropertyValue]    = new() { Foreground = AnsiColor.Named(AnsiNamedColor.BrightWhite) },
        [ConsoleSegment.ExceptionLabel]   = new() { Foreground = AnsiColor.Named(AnsiNamedColor.Red), Decoration = AnsiTextDecoration.Bold },
        [ConsoleSegment.ExceptionMessage] = new() { Foreground = AnsiColor.Named(AnsiNamedColor.Red) },
        [ConsoleSegment.ExceptionStack]   = new() { Foreground = AnsiColor.Named(AnsiNamedColor.BrightBlack) }
    };
}

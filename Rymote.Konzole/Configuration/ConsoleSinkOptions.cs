using Microsoft.Extensions.Logging;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Configuration;

public sealed class ConsoleSinkOptions : SinkOptionsBase
{
    public bool UseColors { get; set; } = true;
    public bool UseEmojis { get; set; } = true;

    public IReadOnlyDictionary<LogLevel, ConsoleColor> LevelColors { get; init; } = new Dictionary<LogLevel, ConsoleColor>
    {
        [LogLevel.Trace]       = ConsoleColor.DarkGray,
        [LogLevel.Debug]       = ConsoleColor.Gray,
        [LogLevel.Information] = ConsoleColor.Cyan,
        [LogLevel.Warning]     = ConsoleColor.Yellow,
        [LogLevel.Error]       = ConsoleColor.Red,
        [LogLevel.Critical]    = ConsoleColor.White
    };

    public IReadOnlyDictionary<KonzoleTag, ConsoleColor> TagColors { get; init; } = new Dictionary<KonzoleTag, ConsoleColor>
    {
        [KonzoleTag.Success]  = ConsoleColor.Green,
        [KonzoleTag.Pending]  = ConsoleColor.Blue,
        [KonzoleTag.Complete] = ConsoleColor.DarkGreen,
        [KonzoleTag.Note]     = ConsoleColor.Magenta,
        [KonzoleTag.Start]    = ConsoleColor.DarkCyan,
        [KonzoleTag.Pause]    = ConsoleColor.DarkYellow,
        [KonzoleTag.Watch]    = ConsoleColor.DarkMagenta
    };

    public ConsoleColor CriticalBackgroundColor { get; init; } = ConsoleColor.DarkRed;
}

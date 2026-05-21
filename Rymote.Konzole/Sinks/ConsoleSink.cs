using Microsoft.Extensions.Logging;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;
using Rymote.Konzole.Styling;

namespace Rymote.Konzole.Sinks;

public sealed class ConsoleSink : SinkBase<ConsoleSinkOptions>
{
    private readonly Lock _consoleGate = new();
    private readonly IReadOnlyDictionary<string, AnsiStyle> _segmentPalette;

    public ConsoleSink(ConsoleSinkOptions options) : base(options)
    {
        _segmentPalette = BuildSegmentPalette(options.SegmentStyles);
    }

    public override string Name => "Console";

    protected override ILogFormatter CreateDefaultFormatter() =>
        new ConsoleFormatter(Options.UseEmojis, Options.ShowIcon, Options.ShowLevelLabel);

    protected override ValueTask WriteBatchAsync(IReadOnlyList<LogEntry> batch, CancellationToken cancellationToken)
    {
        foreach (LogEntry entry in batch)
        {
            string rendered = Formatter.Format(entry, FormatterContext);

            if (!Options.UseColors)
            {
                string plain = StyleMarkup.Strip(rendered);
                TextWriter plainDestination = entry.Level >= LogLevel.Error ? Console.Error : Console.Out;
                lock (_consoleGate) { plainDestination.WriteLine(plain); }
                continue;
            }

            AnsiOptions ansiOptions = new()
            {
                BaseStyle = AnsiStyle.Empty,
                SegmentPalette = ExtendPaletteWithDynamicSegments(entry)
            };
            string ansiOutput = StyleMarkup.ToAnsi(rendered, ansiOptions);

            TextWriter destination = entry.Level >= LogLevel.Error ? Console.Error : Console.Out;
            lock (_consoleGate)
            {
                destination.WriteLine(ansiOutput);
            }
        }

        return ValueTask.CompletedTask;
    }

    private IReadOnlyDictionary<string, AnsiStyle> ExtendPaletteWithDynamicSegments(LogEntry entry)
    {
        Dictionary<string, AnsiStyle> palette = new(_segmentPalette, StringComparer.OrdinalIgnoreCase);
        AnsiColor? dynamicColor = ResolveDynamicColor(entry);

        palette["icon"] = palette.TryGetValue("icon", out AnsiStyle iconBase)
            ? iconBase with { Foreground = dynamicColor }
            : new AnsiStyle { Foreground = dynamicColor };

        palette["level-label"] = palette.TryGetValue("level-label", out AnsiStyle labelBase)
            ? labelBase with { Foreground = dynamicColor }
            : new AnsiStyle { Foreground = dynamicColor, Decoration = AnsiTextDecoration.Bold };

        if (entry.Level == LogLevel.Critical)
        {
            AnsiStyle messageErrorStyle = palette.TryGetValue("message-error", out AnsiStyle existing) ? existing : AnsiStyle.Empty;
            palette["message-error"] = messageErrorStyle with { Background = Options.CriticalBackgroundColor };
        }

        return palette;
    }

    private AnsiColor? ResolveDynamicColor(LogEntry entry)
    {
        if (entry.Tag.HasValue && Options.TagColors.TryGetValue(entry.Tag.Value, out AnsiColor tagColor))
            return tagColor;

        return Options.LevelColors.TryGetValue(entry.Level, out AnsiColor levelColor) ? levelColor : null;
    }

    private static IReadOnlyDictionary<string, AnsiStyle> BuildSegmentPalette(IReadOnlyDictionary<ConsoleSegment, AnsiStyle> source)
    {
        Dictionary<string, AnsiStyle> palette = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<ConsoleSegment, AnsiStyle> entry in source)
        {
            palette[SegmentToMarkupKey(entry.Key)] = entry.Value;
        }
        return palette;
    }

    private static string SegmentToMarkupKey(ConsoleSegment segment) => segment switch
    {
        ConsoleSegment.Icon             => "icon",
        ConsoleSegment.Timestamp        => "timestamp",
        ConsoleSegment.Category         => "category",
        ConsoleSegment.EventId          => "event-id",
        ConsoleSegment.Scope            => "scope",
        ConsoleSegment.Message          => "message",
        ConsoleSegment.MessageWarning   => "message-warning",
        ConsoleSegment.MessageError     => "message-error",
        ConsoleSegment.LevelLabel       => "level-label",
        ConsoleSegment.PropertyKey      => "property-key",
        ConsoleSegment.PropertyValue    => "property-value",
        ConsoleSegment.ExceptionLabel   => "exception-label",
        ConsoleSegment.ExceptionMessage => "exception-message",
        ConsoleSegment.ExceptionStack   => "exception-stack",
        _                               => segment.ToString().ToLowerInvariant()
    };
}

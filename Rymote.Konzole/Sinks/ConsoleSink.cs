using Microsoft.Extensions.Logging;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Sinks;

public sealed class ConsoleSink : SinkBase<ConsoleSinkOptions>
{
    private readonly Lock _consoleGate = new();

    public ConsoleSink(ConsoleSinkOptions options) : base(options) { }

    public override string Name => "Console";

    protected override ILogFormatter CreateDefaultFormatter() => new ConsoleFormatter(Options.UseEmojis);

    protected override ValueTask WriteBatchAsync(IReadOnlyList<LogEntry> batch, CancellationToken cancellationToken)
    {
        foreach (LogEntry entry in batch)
        {
            string rendered = Formatter.Format(entry, FormatterContext);
            TextWriter destination = entry.Level >= LogLevel.Error ? Console.Error : Console.Out;

            lock (_consoleGate)
            {
                if (!Options.UseColors)
                {
                    destination.WriteLine(rendered);
                    continue;
                }

                ConsoleColor originalForeground = Console.ForegroundColor;
                ConsoleColor originalBackground = Console.BackgroundColor;
                try
                {
                    ApplyColor(entry);
                    destination.WriteLine(rendered);
                }
                finally
                {
                    Console.ForegroundColor = originalForeground;
                    Console.BackgroundColor = originalBackground;
                }
            }
        }

        return ValueTask.CompletedTask;
    }

    private void ApplyColor(LogEntry entry)
    {
        if (entry.Tag.HasValue && Options.TagColors.TryGetValue(entry.Tag.Value, out ConsoleColor tagColor))
        {
            Console.ForegroundColor = tagColor;
            return;
        }

        if (Options.LevelColors.TryGetValue(entry.Level, out ConsoleColor levelColor))
        {
            Console.ForegroundColor = levelColor;
        }

        if (entry.Level == LogLevel.Critical)
        {
            Console.BackgroundColor = Options.CriticalBackgroundColor;
        }
    }
}

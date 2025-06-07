using Rymote.Konzole.Configuration;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Sinks;

public class ConsoleSink : SinkBase<ConsoleSinkOptions>
{
    private readonly object _lockObject = new();
    
    public override string Name => "Console";
    
    public ConsoleSink(ConsoleSinkOptions options) : base(options)
    {
        if (!options.UseEmojis) return;
        
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        }
        catch
        {
            // Fallback if encoding change fails
        }
    }
    
    public override async Task WriteAsync(LogEntry entry)
    {
        if (!ShouldLog(entry))
            return;
            
        await Task.Run(() =>
        {
            lock (_lockObject)
            {
                ConsoleColor originalForegroundColor = Console.ForegroundColor;
                ConsoleColor originalBackgroundColor = Console.BackgroundColor;
                
                try
                {
                    if (Options.UseColors)
                    {
                        SetConsoleColor(entry.Level);
                    }
                    
                    string formattedMessage = Formatter.Format(entry);
                    Console.WriteLine(formattedMessage);
                }
                finally
                {
                    Console.ForegroundColor = originalForegroundColor;
                    Console.BackgroundColor = originalBackgroundColor;
                }
            }
        });
    }
    
    protected override ILogFormatter CreateDefaultFormatter()
    {
        return new ConsoleFormatter(Options);
    }
    
    private void SetConsoleColor(KonzoleLogLevel level)
    {
        switch (level)
        {
            case KonzoleLogLevel.Trace:
                Console.ForegroundColor = ConsoleColor.DarkGray;
                break;
            case KonzoleLogLevel.Debug:
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case KonzoleLogLevel.Information:
                Console.ForegroundColor = ConsoleColor.Cyan;
                break;
            case KonzoleLogLevel.Success:
                Console.ForegroundColor = ConsoleColor.Green;
                break;
            case KonzoleLogLevel.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            case KonzoleLogLevel.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                break;
            case KonzoleLogLevel.Fatal:
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.DarkRed;
                break;
            case KonzoleLogLevel.Pending:
                Console.ForegroundColor = ConsoleColor.Blue;
                break;
            case KonzoleLogLevel.Complete:
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                break;
            case KonzoleLogLevel.Note:
                Console.ForegroundColor = ConsoleColor.Magenta;
                break;
            case KonzoleLogLevel.Start:
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                break;
            case KonzoleLogLevel.Pause:
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                break;
            case KonzoleLogLevel.Watch:
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                break;
        }
    }
} 
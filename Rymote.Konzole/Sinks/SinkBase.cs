using Rymote.Konzole.Configuration;
using Rymote.Konzole.Models;
using Rymote.Konzole.Formatters;

namespace Rymote.Konzole.Sinks;

public abstract class SinkBase<TOptions> : ISink where TOptions : SinkOptionsBase
{
    protected readonly TOptions Options;
    protected readonly ILogFormatter Formatter;
    
    protected SinkBase(TOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Formatter = options.Formatter ?? CreateDefaultFormatter();
    }
    
    public abstract string Name { get; }
    
    public abstract Task WriteAsync(LogEntry entry);
    
    public virtual Task FlushAsync() => Task.CompletedTask;
    
    public virtual void Dispose() 
    {
        FlushAsync().Wait();
    }
    
    protected bool ShouldLog(LogEntry entry)
    {
        Microsoft.Extensions.Logging.LogLevel logLevel = ConvertToLogLevel(entry.Level);
        return logLevel >= Options.MinimumLevel;
    }
    
    protected abstract ILogFormatter CreateDefaultFormatter();
    
    private Microsoft.Extensions.Logging.LogLevel ConvertToLogLevel(KonzoleLogLevel level)
    {
        return level switch
        {
            KonzoleLogLevel.Trace => Microsoft.Extensions.Logging.LogLevel.Trace,
            KonzoleLogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
            KonzoleLogLevel.Information => Microsoft.Extensions.Logging.LogLevel.Information,
            KonzoleLogLevel.Success => Microsoft.Extensions.Logging.LogLevel.Information,
            KonzoleLogLevel.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
            KonzoleLogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
            KonzoleLogLevel.Fatal => Microsoft.Extensions.Logging.LogLevel.Critical,
            KonzoleLogLevel.Pending => Microsoft.Extensions.Logging.LogLevel.Information,
            KonzoleLogLevel.Complete => Microsoft.Extensions.Logging.LogLevel.Information,
            KonzoleLogLevel.Note => Microsoft.Extensions.Logging.LogLevel.Information,
            KonzoleLogLevel.Start => Microsoft.Extensions.Logging.LogLevel.Information,
            KonzoleLogLevel.Pause => Microsoft.Extensions.Logging.LogLevel.Information,
            KonzoleLogLevel.Watch => Microsoft.Extensions.Logging.LogLevel.Debug,
            _ => Microsoft.Extensions.Logging.LogLevel.Information
        };
    }
} 
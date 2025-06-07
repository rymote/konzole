using Rymote.Konzole.Models;

namespace Rymote.Konzole.Sinks;

public interface ISink : IDisposable
{
    string Name { get; }
    Task WriteAsync(LogEntry entry);
    Task FlushAsync();
} 
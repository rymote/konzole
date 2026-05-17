using Microsoft.Extensions.Logging;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Sinks;

public interface ISink : IAsyncDisposable, IDisposable
{
    string Name { get; }
    LogLevel MinimumLevel { get; }

    void TryEnqueue(LogEntry entry);
    ValueTask FlushAsync(CancellationToken cancellationToken);
}

using Rymote.Konzole.Models;

namespace Rymote.Konzole.Formatters;

public interface ILogFormatter
{
    string Format(LogEntry entry);
} 
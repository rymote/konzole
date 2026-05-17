using Rymote.Konzole.Configuration;

namespace Rymote.Konzole.Tests.Infrastructure;

public sealed class FakeSinkOptions : SinkOptionsBase
{
    public TimeSpan WriteDelay { get; init; } = TimeSpan.Zero;
    public bool ThrowOnWrite { get; init; }
}

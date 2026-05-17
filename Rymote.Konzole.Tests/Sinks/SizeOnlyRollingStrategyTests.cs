using Rymote.Konzole.Sinks.Files;
using Xunit;

namespace Rymote.Konzole.Tests.Sinks;

public class SizeOnlyRollingStrategyTests : IDisposable
{
    private readonly string _temporaryDirectory;
    private readonly DateTimeOffset _now = new(2026, 5, 17, 12, 0, 0, TimeSpan.Zero);

    public SizeOnlyRollingStrategyTests()
    {
        _temporaryDirectory = Path.Combine(Path.GetTempPath(), $"konzole-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_temporaryDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
            Directory.Delete(_temporaryDirectory, recursive: true);
    }

    [Fact]
    public void ResolveActivePath_AlwaysReturnsBasePath()
    {
        SizeOnlyRollingStrategy strategy = new();
        string basePath = Path.Combine(_temporaryDirectory, "app.log");

        Assert.Equal(basePath, strategy.ResolveActivePath(basePath, _now));
    }

    [Fact]
    public void ShouldRoll_TrueWhenPendingExceedsCap()
    {
        SizeOnlyRollingStrategy strategy = new() { MaxFileSize = 100 };
        string activePath = Path.Combine(_temporaryDirectory, "app.log");

        Assert.False(strategy.ShouldRoll(activePath, currentSize: 50, pendingBytes: 40, _now));
        Assert.True(strategy.ShouldRoll(activePath, currentSize: 50, pendingBytes: 60, _now));
    }

    [Fact]
    public void Roll_ShiftsExistingFiles_AndDropsBeyondMaxFiles()
    {
        SizeOnlyRollingStrategy strategy = new() { MaxFileSize = 100 };
        string basePath = Path.Combine(_temporaryDirectory, "app.log");

        File.WriteAllText(basePath, "active");
        File.WriteAllText(Path.Combine(_temporaryDirectory, "app.1.log"), "rotation-1");
        File.WriteAllText(Path.Combine(_temporaryDirectory, "app.2.log"), "rotation-2");
        File.WriteAllText(Path.Combine(_temporaryDirectory, "app.3.log"), "rotation-3");
        File.WriteAllText(Path.Combine(_temporaryDirectory, "app.4.log"), "rotation-4");

        strategy.Roll(basePath, maxFiles: 5, _now);

        Assert.False(File.Exists(basePath));
        Assert.Equal("active",     File.ReadAllText(Path.Combine(_temporaryDirectory, "app.1.log")));
        Assert.Equal("rotation-1", File.ReadAllText(Path.Combine(_temporaryDirectory, "app.2.log")));
        Assert.Equal("rotation-2", File.ReadAllText(Path.Combine(_temporaryDirectory, "app.3.log")));
        Assert.Equal("rotation-3", File.ReadAllText(Path.Combine(_temporaryDirectory, "app.4.log")));
        Assert.False(File.Exists(Path.Combine(_temporaryDirectory, "app.5.log")));
    }
}

using Rymote.Konzole.Sinks.Files;
using Xunit;

namespace Rymote.Konzole.Tests.Sinks;

public class DateOnlyRollingStrategyTests : IDisposable
{
    private readonly string _temporaryDirectory;

    public DateOnlyRollingStrategyTests()
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
    public void ResolveActivePath_EmbedsDate()
    {
        DateOnlyRollingStrategy strategy = new();
        string basePath = Path.Combine(_temporaryDirectory, "app.log");
        DateTimeOffset moment = new(2026, 5, 17, 12, 0, 0, TimeSpan.Zero);

        string activePath = strategy.ResolveActivePath(basePath, moment);

        Assert.Equal(Path.Combine(_temporaryDirectory, "app-2026-05-17.log"), activePath);
    }

    [Fact]
    public void ShouldRoll_TrueWhenDateDiffersFromActivePath()
    {
        DateOnlyRollingStrategy strategy = new();
        string activePath = Path.Combine(_temporaryDirectory, "app-2026-05-17.log");

        Assert.False(strategy.ShouldRoll(activePath, 0, 0, new DateTimeOffset(2026, 5, 17, 23, 59, 0, TimeSpan.Zero)));
        Assert.True(strategy.ShouldRoll(activePath, 0, 0,  new DateTimeOffset(2026, 5, 18,  0,  0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void Roll_DeletesFilesOlderThanMaxFilesDays()
    {
        DateOnlyRollingStrategy strategy = new();
        string basePath = Path.Combine(_temporaryDirectory, "app.log");
        File.WriteAllText(Path.Combine(_temporaryDirectory, "app-2026-05-10.log"), "old");
        File.WriteAllText(Path.Combine(_temporaryDirectory, "app-2026-05-15.log"), "recent");
        File.WriteAllText(Path.Combine(_temporaryDirectory, "app-2026-05-16.log"), "yesterday");

        strategy.Roll(basePath, maxFiles: 3, new DateTimeOffset(2026, 5, 17, 12, 0, 0, TimeSpan.Zero));

        Assert.False(File.Exists(Path.Combine(_temporaryDirectory, "app-2026-05-10.log")));
        Assert.True (File.Exists(Path.Combine(_temporaryDirectory, "app-2026-05-15.log")));
        Assert.True (File.Exists(Path.Combine(_temporaryDirectory, "app-2026-05-16.log")));
    }
}

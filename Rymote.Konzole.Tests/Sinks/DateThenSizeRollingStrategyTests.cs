using Rymote.Konzole.Sinks.Files;
using Xunit;

namespace Rymote.Konzole.Tests.Sinks;

public class DateThenSizeRollingStrategyTests : IDisposable
{
    private readonly string _temporaryDirectory;

    public DateThenSizeRollingStrategyTests()
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
        DateThenSizeRollingStrategy strategy = new() { MaxFileSize = 100 };
        string basePath = Path.Combine(_temporaryDirectory, "app.log");
        DateTimeOffset moment = new(2026, 5, 17, 12, 0, 0, TimeSpan.Zero);

        Assert.Equal(Path.Combine(_temporaryDirectory, "app-2026-05-17.log"),
            strategy.ResolveActivePath(basePath, moment));
    }

    [Fact]
    public void ShouldRoll_TrueOnSizeCap_EvenWithinSameDay()
    {
        DateThenSizeRollingStrategy strategy = new() { MaxFileSize = 100 };
        string activePath = Path.Combine(_temporaryDirectory, "app-2026-05-17.log");
        DateTimeOffset sameDayMoment = new(2026, 5, 17, 14, 0, 0, TimeSpan.Zero);

        Assert.True(strategy.ShouldRoll(activePath, currentSize: 90, pendingBytes: 20, sameDayMoment));
    }

    [Fact]
    public void Roll_OnSizeCap_ShiftsSameDayFiles()
    {
        DateThenSizeRollingStrategy strategy = new() { MaxFileSize = 100 };
        string basePath = Path.Combine(_temporaryDirectory, "app.log");
        DateTimeOffset moment = new(2026, 5, 17, 14, 0, 0, TimeSpan.Zero);

        File.WriteAllText(Path.Combine(_temporaryDirectory, "app-2026-05-17.log"), "active-day");
        File.WriteAllText(Path.Combine(_temporaryDirectory, "app-2026-05-17.1.log"), "first-rotation");

        strategy.Roll(basePath, maxFiles: 5, moment);

        Assert.False(File.Exists(Path.Combine(_temporaryDirectory, "app-2026-05-17.log")));
        Assert.Equal("active-day",     File.ReadAllText(Path.Combine(_temporaryDirectory, "app-2026-05-17.1.log")));
        Assert.Equal("first-rotation", File.ReadAllText(Path.Combine(_temporaryDirectory, "app-2026-05-17.2.log")));
    }
}

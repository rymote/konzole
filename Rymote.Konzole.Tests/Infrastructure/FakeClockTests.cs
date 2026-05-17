using Xunit;

namespace Rymote.Konzole.Tests.Infrastructure;

public class FakeClockTests
{
    [Fact]
    public void Now_ReturnsInitialTime_UntilAdvanced()
    {
        DateTimeOffset initialTime = new(2026, 5, 17, 12, 0, 0, TimeSpan.Zero);
        FakeClock clock = new(initialTime);

        Assert.Equal(initialTime, clock.Now());

        clock.Advance(TimeSpan.FromHours(2));

        Assert.Equal(initialTime.AddHours(2), clock.Now());
    }
}

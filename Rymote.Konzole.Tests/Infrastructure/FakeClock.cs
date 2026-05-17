namespace Rymote.Konzole.Tests.Infrastructure;

public sealed class FakeClock
{
    private DateTimeOffset _currentTime;

    public FakeClock(DateTimeOffset initialTime)
    {
        _currentTime = initialTime;
    }

    public DateTimeOffset Now() => _currentTime;

    public void Advance(TimeSpan duration) => _currentTime = _currentTime.Add(duration);

    public void SetTo(DateTimeOffset moment) => _currentTime = moment;
}

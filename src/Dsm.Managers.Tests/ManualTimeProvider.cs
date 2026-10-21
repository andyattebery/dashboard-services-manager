namespace Dsm.Managers.Tests;

/// <summary>
/// A <see cref="TimeProvider"/> whose timestamps move only when a test calls <see cref="Advance"/>.
/// Only the monotonic timestamp API is controlled, because that is what the manager measures
/// durations with. Starts at an arbitrary non-zero value, like real timestamps, so a production
/// sentinel of 0 can't pass by accident.
/// </summary>
public sealed class ManualTimeProvider : TimeProvider
{
    private long _timestamp = TimeSpan.FromDays(1).Ticks;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp() => _timestamp;

    public void Advance(TimeSpan by) => _timestamp += by.Ticks;
}

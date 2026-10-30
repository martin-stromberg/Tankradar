namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Zeitquelle für Tests mit von Hand gesteuerter Uhr.
/// </summary>
public sealed class ManualTimeProvider : TimeProvider
{
    private readonly List<ManualTimer> _timers = [];

    /// <summary>
    /// Erstellt die Zeitquelle mit dem Startzeitpunkt 2026-10-05 12:00 UTC.
    /// </summary>
    public ManualTimeProvider()
    {
        Now = new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
    }

    /// <summary>
    /// Der aktuelle Zeitpunkt.
    /// </summary>
    public DateTimeOffset Now { get; set; }

    /// <summary>
    /// Der aktuelle Zeitpunkt als UTC-<see cref="DateTime"/>.
    /// </summary>
    public DateTime UtcNow => Now.UtcDateTime;

    /// <summary>
    /// Stellt die Uhr vor und löst die dabei fälligen Zeitgeber synchron aus.
    /// </summary>
    /// <param name="by">Die Dauer.</param>
    public void Advance(TimeSpan by)
    {
        Now += by;
        foreach (var timer in _timers.ToList())
        {
            timer.FireDue(Now);
        }
    }

    /// <inheritdoc />
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state);
        timer.Change(dueTime, period);
        _timers.Add(timer);
        return timer;
    }

    /// <inheritdoc />
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    /// <inheritdoc />
    public override long GetTimestamp()
    {
        return Now.UtcTicks;
    }

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow()
    {
        return Now;
    }

    private sealed class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state) : ITimer
    {
        private DateTimeOffset? _due;
        private TimeSpan _period = Timeout.InfiniteTimeSpan;

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            _due = dueTime == Timeout.InfiniteTimeSpan ? null : owner.Now + dueTime;
            _period = period;
            return true;
        }

        public void FireDue(DateTimeOffset now)
        {
            while (_due is { } due && due <= now)
            {
                _due = _period > TimeSpan.Zero && _period != Timeout.InfiniteTimeSpan ? due + _period : null;
                callback(state);
            }
        }

        public void Dispose()
        {
            _due = null;
            owner._timers.Remove(this);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}

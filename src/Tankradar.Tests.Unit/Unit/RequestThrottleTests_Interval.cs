using Tankradar.MAUI.Services.Pricing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Drosselung der Anfragen an den Preisdienst.
/// </summary>
public class RequestThrottleTests_Interval : BaseTest
{
    private readonly RecordingDelay _delay = new();
    private readonly ManualTimeProvider _clock = new();

    /// <summary>
    /// Prüft, dass ein Sprung der Systemzeit (Wanduhr) den Mindestabstand nicht verändert; maßgeblich ist die monotone Uhr.
    /// </summary>
    [Fact]
    public async Task WaitAsync_WallClockJump_DoesNotChangeInterval()
    {
        var clock = new SplitClock();
        var throttle = new RequestThrottle(TimeSpan.FromSeconds(1), _delay, clock);

        await throttle.WaitAsync(CancellationToken.None);
        clock.Monotonic += TimeSpan.FromMilliseconds(400).Ticks;
        clock.Wall = clock.Wall.AddHours(5);
        await throttle.WaitAsync(CancellationToken.None);

        Assert.Equal([TimeSpan.FromMilliseconds(600)], _delay.Delays);
    }

    private sealed class SplitClock : TimeProvider
    {
        public long Monotonic { get; set; }

        public DateTimeOffset Wall { get; set; } = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp()
        {
            return Monotonic;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return Wall;
        }
    }

    /// <summary>
    /// Prüft, dass die erste Anfrage sofort und die zweite erst nach dem Mindestabstand erfolgt.
    /// </summary>
    [Fact]
    public async Task WaitAsync_SecondRequestWithinInterval_Waits()
    {
        var throttle = new RequestThrottle(TimeSpan.FromSeconds(1), _delay, _clock);

        await throttle.WaitAsync(CancellationToken.None);
        _clock.Advance(TimeSpan.FromMilliseconds(300));
        await throttle.WaitAsync(CancellationToken.None);

        Assert.Equal([TimeSpan.FromMilliseconds(700)], _delay.Delays);
    }

    /// <summary>
    /// Prüft, dass nach Ablauf des Mindestabstands nicht gewartet wird.
    /// </summary>
    [Fact]
    public async Task WaitAsync_AfterInterval_DoesNotWait()
    {
        var throttle = new RequestThrottle(TimeSpan.FromSeconds(1), _delay, _clock);

        await throttle.WaitAsync(CancellationToken.None);
        _clock.Advance(TimeSpan.FromSeconds(2));
        await throttle.WaitAsync(CancellationToken.None);

        Assert.Empty(_delay.Delays);
    }

    /// <summary>
    /// Prüft, dass die echte Verzögerung bei null sofort zurückkehrt.
    /// </summary>
    [Fact]
    public async Task TaskDelay_ZeroDelay_CompletesImmediately()
    {
        await new TaskDelay().DelayAsync(TimeSpan.Zero, CancellationToken.None);
    }
}

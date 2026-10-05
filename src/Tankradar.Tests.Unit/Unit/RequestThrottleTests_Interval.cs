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

using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass <see cref="TransientWait"/> UIA-Timeouts als „noch nicht erfüllt“ wertet und bis zur Frist weiterwartet, andere Fehler aber nie verdeckt.
/// </summary>
public class TransientWaitTests : BaseTest
{
    private TimeSpan _now = TimeSpan.Zero;

    private void Sleep(TimeSpan pause)
    {
        _now += pause;
    }

    private TimeSpan Elapsed()
    {
        return _now;
    }

    /// <summary>
    /// Prüft, dass eine sofort erfüllte Bedingung ohne Warten zurückkehrt.
    /// </summary>
    [Fact]
    public void Until_ConditionTrue_ReturnsImmediately()
    {
        var result = TransientWait.Until(() => true, TimeSpan.FromSeconds(10), out var lastTimeout, sleep: Sleep, elapsed: Elapsed);

        Assert.True(result);
        Assert.Null(lastTimeout);
        Assert.Equal(TimeSpan.Zero, _now);
    }

    /// <summary>
    /// Prüft die Regression: Mehr als drei UIA-Timeouts hintereinander beenden das Warten nicht, solange die Frist nicht abgelaufen ist.
    /// </summary>
    [Fact]
    public void Until_ManyTimeoutsThenSuccess_KeepsWaiting()
    {
        var calls = 0;

        var result = TransientWait.Until(
            () => ++calls < 8 ? throw new TimeoutException("UIA") : true,
            TimeSpan.FromSeconds(30),
            out var lastTimeout,
            poll: TimeSpan.FromSeconds(1),
            sleep: Sleep,
            elapsed: Elapsed);

        Assert.True(result);
        Assert.Equal(8, calls);
        Assert.NotNull(lastTimeout);
    }

    /// <summary>
    /// Prüft, dass bei dauerhaften Timeouts bis zur Frist gewartet wird und der letzte Timeout gemeldet wird.
    /// </summary>
    [Fact]
    public void Until_OnlyTimeouts_WaitsUntilDeadlineAndReportsLast()
    {
        var calls = 0;

        var result = TransientWait.Until(
            () => throw new TimeoutException($"UIA {++calls}"),
            TimeSpan.FromSeconds(10),
            out var lastTimeout,
            poll: TimeSpan.FromSeconds(1),
            sleep: Sleep,
            elapsed: Elapsed);

        Assert.False(result);
        Assert.Equal(10, calls);
        Assert.Equal("UIA 10", lastTimeout!.Message);
        Assert.Equal(TimeSpan.FromSeconds(10), _now);
    }

    /// <summary>
    /// Prüft, dass eine nie erfüllte Bedingung ohne Timeout false ergibt und keinen Timeout meldet.
    /// </summary>
    [Fact]
    public void Until_NeverTrue_ReturnsFalseWithoutTimeout()
    {
        var result = TransientWait.Until(() => false, TimeSpan.FromSeconds(2), out var lastTimeout, poll: TimeSpan.FromSeconds(1), sleep: Sleep, elapsed: Elapsed);

        Assert.False(result);
        Assert.Null(lastTimeout);
    }

    /// <summary>
    /// Prüft, dass andere Ausnahmen sofort sichtbar werden.
    /// </summary>
    [Fact]
    public void Until_OtherException_IsNotSwallowed()
    {
        var calls = 0;

        Assert.Throws<InvalidOperationException>(() => TransientWait.Until(
            () =>
            {
                calls++;
                throw new InvalidOperationException("Programmfehler");
            },
            TimeSpan.FromSeconds(10),
            out _,
            sleep: Sleep,
            elapsed: Elapsed));
        Assert.Equal(1, calls);
    }
}

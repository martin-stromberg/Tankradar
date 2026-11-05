using FlaUI.Core.Exceptions;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass <see cref="TransientWait"/> ein nicht mehr verfügbares Element der UI-Automation wie einen UIA-Timeout als „noch nicht erfüllt“ wertet
/// und die in solchen Fehlern verbrachte Zeit ausweist, damit Zeitgrenzen auf die Messzeit ohne UIA-Wartezeit bezogen werden können.
/// </summary>
public class TransientWaitTests_ElementNotAvailable : BaseTest
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
    /// Prüft, dass ein nicht mehr verfügbares Element als „noch nicht erfüllt“ gilt und bis zum Erfolg weitergewartet wird.
    /// </summary>
    [Fact]
    public void Until_ElementNotAvailableThenSuccess_KeepsWaiting()
    {
        var calls = 0;

        var result = TransientWait.Until(
            () => ++calls < 4 ? throw new ElementNotAvailableException("Element neu aufgebaut") : true,
            TimeSpan.FromSeconds(30),
            out var lastTimeout,
            poll: TimeSpan.FromSeconds(1),
            sleep: Sleep,
            elapsed: Elapsed);

        Assert.True(result);
        Assert.Equal(4, calls);
        Assert.IsType<ElementNotAvailableException>(lastTimeout);
    }

    /// <summary>
    /// Prüft, dass das Element auch als innere Ausnahme erkannt wird.
    /// </summary>
    [Fact]
    public void Until_ElementNotAvailableAsInnerException_IsTransient()
    {
        var calls = 0;

        var result = TransientWait.Until(
            () => ++calls < 2 ? throw new InvalidOperationException("Suche fehlgeschlagen", new ElementNotAvailableException("weg")) : true,
            TimeSpan.FromSeconds(10),
            out _,
            poll: TimeSpan.FromSeconds(1),
            sleep: Sleep,
            elapsed: Elapsed);

        Assert.True(result);
        Assert.Equal(2, calls);
    }

    /// <summary>
    /// Prüft, dass bei dauerhaft nicht verfügbarem Element bis zur Frist gewartet und der letzte Fehler gemeldet wird.
    /// </summary>
    [Fact]
    public void Until_AlwaysElementNotAvailable_WaitsUntilDeadline()
    {
        var result = TransientWait.Until(
            () => throw new ElementNotAvailableException("weg"),
            TimeSpan.FromSeconds(5),
            out var lastTimeout,
            poll: TimeSpan.FromSeconds(1),
            sleep: Sleep,
            elapsed: Elapsed);

        Assert.False(result);
        Assert.NotNull(lastTimeout);
        Assert.True(_now >= TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Prüft, dass andere Ausnahmen weiterhin nicht verdeckt werden.
    /// </summary>
    [Fact]
    public void Until_OtherException_IsNotHidden()
    {
        Assert.Throws<InvalidOperationException>(() => TransientWait.Until(
            () => throw new InvalidOperationException("Programmfehler"),
            TimeSpan.FromSeconds(5),
            out _,
            sleep: Sleep,
            elapsed: Elapsed));
    }

    /// <summary>
    /// Prüft, dass die in vorübergehenden Fehlern verbrachte Zeit summiert und von der übrigen Zeit getrennt ausgewiesen wird.
    /// </summary>
    [Fact]
    public void Until_ReportsTimeSpentInTransientFailures()
    {
        var calls = 0;

        var result = TransientWait.Until(
            () =>
            {
                calls++;
                if (calls <= 2)
                {
                    _now += TimeSpan.FromSeconds(4);
                    throw new TimeoutException("UIA");
                }

                if (calls == 3)
                {
                    _now += TimeSpan.FromSeconds(1);
                    return false;
                }

                return true;
            },
            TimeSpan.FromSeconds(60),
            out var lastTimeout,
            out var timeoutTime,
            poll: TimeSpan.FromSeconds(1),
            sleep: Sleep,
            elapsed: Elapsed);

        Assert.True(result);
        Assert.NotNull(lastTimeout);
        Assert.Equal(TimeSpan.FromSeconds(8), timeoutTime);
        Assert.Equal(TimeSpan.FromSeconds(12), _now);
    }

    /// <summary>
    /// Prüft, dass ohne vorübergehende Fehler keine Timeoutzeit ausgewiesen wird.
    /// </summary>
    [Fact]
    public void Until_NoTransientFailures_ReportsZeroTimeoutTime()
    {
        var result = TransientWait.Until(() => true, TimeSpan.FromSeconds(5), out var lastTimeout, out var timeoutTime, sleep: Sleep, elapsed: Elapsed);

        Assert.True(result);
        Assert.Null(lastTimeout);
        Assert.Equal(TimeSpan.Zero, timeoutTime);
    }

    /// <summary>
    /// Prüft die Erkennung vorübergehender Fehler.
    /// </summary>
    [Fact]
    public void IsTransient_RecognizesTimeoutAndElementNotAvailableOnly()
    {
        Assert.True(TransientWait.IsTransient(new TimeoutException()));
        Assert.True(TransientWait.IsTransient(new ElementNotAvailableException("weg")));
        Assert.False(TransientWait.IsTransient(new InvalidOperationException()));
        Assert.False(TransientWait.IsTransient(new ArgumentException()));
    }
}

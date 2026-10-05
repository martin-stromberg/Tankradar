using System.Runtime.InteropServices;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass die E2E-Testbasis UIA-Timeouts begrenzt wiederholt, echte Fehler aber nie verdeckt.
/// </summary>
public class TransientRetryTests_Behavior : BaseTest
{
    private readonly List<TimeSpan> _sleeps = [];

    private void Sleep(TimeSpan pause)
    {
        _sleeps.Add(pause);
    }

    /// <summary>
    /// Prüft, dass ein sofortiger Erfolg nicht wiederholt wird.
    /// </summary>
    [Fact]
    public void Run_Success_ReturnsWithoutRetry()
    {
        var calls = 0;

        var result = TransientRetry.Run(() => ++calls, "Test", sleep: Sleep);

        Assert.Equal(1, result);
        Assert.Empty(_sleeps);
    }

    /// <summary>
    /// Prüft, dass ein vorübergehender Timeout wiederholt wird und danach Erfolg möglich ist.
    /// </summary>
    [Fact]
    public void Run_TimeoutThenSuccess_Retries()
    {
        var calls = 0;

        var result = TransientRetry.Run(
            () => ++calls < 3 ? throw new TimeoutException("UIA Timeout") : "ok",
            "Test",
            pause: TimeSpan.FromSeconds(1),
            sleep: Sleep);

        Assert.Equal("ok", result);
        Assert.Equal(3, calls);
        Assert.Equal([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)], _sleeps);
    }

    /// <summary>
    /// Prüft den COM-Fehler 0x80131505 (UIA-Timeout), auch als innere Ausnahme.
    /// </summary>
    [Fact]
    public void Run_UiaComTimeoutWrappedInOtherException_IsRetried()
    {
        var calls = 0;

        TransientRetry.Run(
            () =>
            {
                if (++calls == 1)
                {
                    throw new InvalidOperationException("außen", new COMException("UIA Timeout", TransientRetry.UiaTimeoutHResult));
                }
            },
            "Test",
            sleep: Sleep);

        Assert.Equal(2, calls);
    }

    /// <summary>
    /// Prüft, dass nach allen Versuchen eine klare Meldung mit dem ursprünglichen Fehler als innere Ausnahme entsteht.
    /// </summary>
    [Fact]
    public void Run_AlwaysTimeout_ThrowsClearMessageAfterMaxAttempts()
    {
        var calls = 0;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransientRetry.Run<int>(() =>
            {
                calls++;
                throw new TimeoutException("UIA Timeout");
            },
            "Aufbau der UI-Automation",
            maxAttempts: 3,
            sleep: Sleep));

        Assert.Equal(3, calls);
        Assert.Equal(2, _sleeps.Count);
        Assert.Contains("Aufbau der UI-Automation", ex.Message);
        Assert.Contains("3 Versuchen", ex.Message);
        Assert.Contains("0x80131505", ex.Message);
        Assert.IsType<TimeoutException>(ex.InnerException);
    }

    /// <summary>
    /// Prüft, dass echte Fehler (Assertions, andere COM-Fehler, Programmfehler) sofort und unverändert weitergereicht werden.
    /// </summary>
    [Fact]
    public void Run_NonTransientErrors_PropagateImmediately()
    {
        var calls = 0;

        Assert.Throws<InvalidOperationException>(() => TransientRetry.Run<int>(() =>
        {
            calls++;
            throw new InvalidOperationException("echter Fehler");
        }, "Test", sleep: Sleep));
        Assert.Throws<COMException>(() => TransientRetry.Run<int>(() =>
        {
            calls++;
            throw new COMException("anderer Fehler", unchecked((int)0x80004005));
        }, "Test", sleep: Sleep));

        Assert.Equal(2, calls);
        Assert.Empty(_sleeps);
    }

    /// <summary>
    /// Prüft die Erkennung vorübergehender Fehler.
    /// </summary>
    [Fact]
    public void IsTransient_DistinguishesTimeoutsFromOtherErrors()
    {
        Assert.True(TransientRetry.IsTransient(new TimeoutException()));
        Assert.True(TransientRetry.IsTransient(new COMException("t", TransientRetry.UiaTimeoutHResult)));
        Assert.False(TransientRetry.IsTransient(new InvalidOperationException()));
        Assert.False(TransientRetry.IsTransient(new COMException("x", unchecked((int)0x80004005))));
    }

    /// <summary>
    /// Prüft die Variante ohne Rückgabewert und die Untergrenze von einem Versuch.
    /// </summary>
    [Fact]
    public void Run_Action_AtLeastOneAttempt()
    {
        var calls = 0;

        TransientRetry.Run(() => calls++, "Test", maxAttempts: 0, sleep: Sleep);

        Assert.Equal(1, calls);
    }
}

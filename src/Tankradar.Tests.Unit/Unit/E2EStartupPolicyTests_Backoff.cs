using System.ComponentModel;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Backoff, Gesamtzeitlimit und Meldung der Startwiederholungen der E2E-Testbasis (UIA-Timeouts auf langsamen Runnern).
/// </summary>
public class E2EStartupPolicyTests_Backoff : BaseTest
{
    private readonly List<TimeSpan> _sleeps = [];

    private void Sleep(TimeSpan pause)
    {
        _sleeps.Add(pause);
    }

    /// <summary>
    /// Prüft, dass die Pausen zwischen den Versuchen wachsen und auf das Maximum begrenzt sind.
    /// </summary>
    [Fact]
    public void Run_WithBackoff_PausesGrowUpToMaximum()
    {
        Assert.Throws<InvalidOperationException>(() => TransientRetry.Run<int>(
            () => throw new TimeoutException("UIA"),
            "Test",
            maxAttempts: 6,
            pause: TimeSpan.FromSeconds(2),
            sleep: Sleep,
            backoffFactor: 2.0,
            maxPause: TimeSpan.FromSeconds(10)));

        Assert.Equal(
            [TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10)],
            _sleeps);
    }

    /// <summary>
    /// Prüft, dass das Gesamtzeitlimit weitere Versuche beendet und die Meldung Versuche und Wartezeit nennt.
    /// </summary>
    [Fact]
    public void Run_TotalLimitExceeded_StopsEarlyAndReportsAttemptsAndWaitTime()
    {
        var calls = 0;
        var elapsed = TimeSpan.Zero;

        var ex = Assert.Throws<InvalidOperationException>(() => TransientRetry.Run<int>(
            () =>
            {
                calls++;
                throw new TimeoutException("UIA");
            },
            "Start",
            maxAttempts: 10,
            pause: TimeSpan.FromSeconds(5),
            sleep: p =>
            {
                elapsed += p;
                Sleep(p);
            },
            backoffFactor: 2.0,
            totalLimit: TimeSpan.FromSeconds(20),
            elapsed: () => elapsed));

        // 5 s + 10 s gewartet; die nächste Pause (20 s) würde das Limit sprengen.
        Assert.Equal(3, calls);
        Assert.Contains("3 Versuchen", ex.Message);
        Assert.Contains("15", ex.Message);
        Assert.Contains("Gesamtzeitlimit", ex.Message);
    }

    /// <summary>
    /// Prüft, dass ein Timeout in einem Start einen begrenzten Neustart auslöst und nach dem Neustart Erfolg möglich ist.
    /// </summary>
    [Fact]
    public void StartWithRestart_TimeoutThenSuccess_RestartsOnceAndCleansUp()
    {
        var starts = 0;
        var cleanups = 0;

        var result = E2EStartupPolicy.StartWithRestart(
            () => ++starts == 1 ? throw new InvalidOperationException("außen", new Win32Exception(1460)) : "ok",
            () => cleanups++,
            maxStarts: 2,
            "Start",
            Sleep);

        Assert.Equal("ok", result);
        Assert.Equal(2, starts);
        Assert.Equal(1, cleanups);
    }

    /// <summary>
    /// Prüft, dass die Neustarts begrenzt sind und die Meldung die Zahl der Starts nennt.
    /// </summary>
    [Fact]
    public void StartWithRestart_AlwaysTimeout_StopsAfterMaxStarts()
    {
        var starts = 0;

        var ex = Assert.Throws<InvalidOperationException>(() => E2EStartupPolicy.StartWithRestart<int>(
            () =>
            {
                starts++;
                throw new TimeoutException("UIA");
            },
            () => { },
            maxStarts: 2,
            "Start",
            Sleep));

        Assert.Equal(2, starts);
        Assert.Contains("2 Starts", ex.Message);
        Assert.IsType<TimeoutException>(ex.InnerException);
    }

    /// <summary>
    /// Prüft, dass echte Fehler weder einen Neustart auslösen noch verdeckt werden.
    /// </summary>
    [Fact]
    public void StartWithRestart_NonTransientError_PropagatesWithoutRestart()
    {
        var starts = 0;

        Assert.Throws<InvalidOperationException>(() => E2EStartupPolicy.StartWithRestart<int>(
            () =>
            {
                starts++;
                throw new InvalidOperationException("Hauptfenster fehlt");
            },
            () => { },
            maxStarts: 3,
            "Start",
            Sleep));

        Assert.Equal(1, starts);
    }

    /// <summary>
    /// Prüft die Ermittlung des UIA-Timeouts: Vorgabe lokal, großzügiger in der CI, Überschreibung per Umgebungsvariable, ungültige Werte werden ignoriert.
    /// </summary>
    /// <param name="explicitValue">Wert der Umgebungsvariable.</param>
    /// <param name="ci">Wert von GITHUB_ACTIONS.</param>
    /// <param name="expectedSeconds">Erwarteter Timeout in Sekunden.</param>
    [Theory]
    [InlineData(null, null, 15)]
    [InlineData(null, "true", 60)]
    [InlineData("90", "true", 90)]
    [InlineData("30", null, 30)]
    [InlineData("abc", "true", 60)]
    [InlineData("0", null, 15)]
    [InlineData("100000", null, 15)]
    public void ResolveUiaTimeout_UsesEnvironment(string? explicitValue, string? ci, int expectedSeconds)
    {
        var timeout = E2EStartupPolicy.ResolveUiaTimeout(name => name switch
        {
            E2EStartupPolicy.UiaTimeoutEnvironmentVariable => explicitValue,
            "GITHUB_ACTIONS" => ci,
            _ => null,
        });

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), timeout);
    }
}

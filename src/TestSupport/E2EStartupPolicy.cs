using System.Diagnostics;

namespace Tankradar.TestSupport;

/// <summary>
/// Regeln für den App-Start der E2E-Tests auf langsamen Runnern: UIA-Timeout, Wartezeit auf das Hauptfenster und begrenzte Neustarts.
/// </summary>
public static class E2EStartupPolicy
{
    /// <summary>
    /// Umgebungsvariable, die den UIA-Timeout (Verbindung und Transaktion der UIA3-Engine) in Sekunden festlegt.
    /// </summary>
    public const string UiaTimeoutEnvironmentVariable = "TANKRADAR_E2E_UIA_TIMEOUT_SECONDS";

    /// <summary>
    /// Standard-UIA-Timeout außerhalb der CI.
    /// </summary>
    public const int LocalUiaTimeoutSeconds = 15;

    /// <summary>
    /// Standard-UIA-Timeout in der CI (GitHub-Runner sind deutlich langsamer).
    /// </summary>
    public const int CiUiaTimeoutSeconds = 60;

    /// <summary>
    /// Ermittelt den UIA-Timeout: gültiger Wert (1 bis 600 Sekunden) aus der Umgebungsvariable, sonst großzügiger Wert in der CI, sonst lokaler Standard.
    /// </summary>
    /// <param name="getEnvironmentVariable">Zugriff auf Umgebungsvariablen (für Tests austauschbar).</param>
    /// <returns>Der Timeout.</returns>
    public static TimeSpan ResolveUiaTimeout(Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);
        if (int.TryParse(getEnvironmentVariable(UiaTimeoutEnvironmentVariable)?.Trim(), out var seconds) && seconds is >= 1 and <= 600)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        var ci = string.Equals(getEnvironmentVariable("GITHUB_ACTIONS")?.Trim(), "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(getEnvironmentVariable("CI")?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
        return TimeSpan.FromSeconds(ci ? CiUiaTimeoutSeconds : LocalUiaTimeoutSeconds);
    }

    /// <summary>
    /// Führt den vollständigen Start (App starten und mit UIA verbinden) aus und startet ihn bei einem UIA-Timeout begrenzt neu.
    /// Echte Fehler werden sofort und unverändert weitergereicht.
    /// </summary>
    /// <typeparam name="T">Ergebnistyp des Starts.</typeparam>
    /// <param name="start">Startet die App und baut die Verbindung auf.</param>
    /// <param name="cleanup">Beendet die halb gestartete App vor einem Neustart.</param>
    /// <param name="maxStarts">Höchstzahl der Starts (mindestens 1).</param>
    /// <param name="description">Beschreibung für die Fehlermeldung.</param>
    /// <param name="sleep">Wartefunktion (für Tests austauschbar).</param>
    /// <returns>Das Ergebnis des ersten erfolgreichen Starts.</returns>
    public static T StartWithRestart<T>(Func<T> start, Action cleanup, int maxStarts, string description, Action<TimeSpan>? sleep = null)
    {
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(cleanup);
        var starts = Math.Max(1, maxStarts);
        var doSleep = sleep ?? Thread.Sleep;
        Exception? last = null;

        for (var attempt = 1; attempt <= starts; attempt++)
        {
            try
            {
                return start();
            }
            catch (Exception ex) when (TransientRetry.IsTransient(ex))
            {
                last = ex;
                if (attempt < starts)
                {
                    cleanup();
                    doSleep(TimeSpan.FromSeconds(3));
                }
            }
        }

        throw new InvalidOperationException(
            $"{description}: Auch nach {starts} Starts der App (je mit Wiederholungen) ist die UI-Automation nicht rechtzeitig erreichbar gewesen. Letzter Fehler: {last!.Message}",
            last);
    }

    /// <summary>
    /// Wartet, bis der Prozess ein Hauptfenster-Handle hat und auf Eingaben wartet, höchstens bis zum Zeitlimit; ein Überschreiten ist kein Fehler (die Verbindung wird danach regulär versucht).
    /// </summary>
    /// <param name="process">Der gestartete Prozess.</param>
    /// <param name="timeout">Das Zeitlimit.</param>
    /// <returns><see langword="true"/>, wenn der Prozess bereit ist.</returns>
    public static bool WaitUntilReady(Process process, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(process);
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < timeout)
        {
            process.Refresh();
            if (process.HasExited)
            {
                return false;
            }

            if (process.MainWindowHandle != IntPtr.Zero)
            {
                try
                {
                    var remaining = timeout - watch.Elapsed;
                    return remaining > TimeSpan.Zero && process.WaitForInputIdle((int)Math.Min(remaining.TotalMilliseconds, int.MaxValue));
                }
                catch (InvalidOperationException)
                {
                    return true;
                }
            }

            Thread.Sleep(100);
        }

        return false;
    }
}

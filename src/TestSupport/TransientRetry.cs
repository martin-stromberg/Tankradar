using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Tankradar.TestSupport;

/// <summary>
/// Wiederholt Aktionen der UI-Automation, die an einem vorübergehenden UIA-Timeout scheitern (z. B. auf langsamen CI-Runnern),
/// begrenzt und mit klarer Meldung. Andere Fehler (Assertions, fehlende Elemente, echte Programmfehler) werden nie wiederholt und nie verdeckt.
/// </summary>
public static class TransientRetry
{
    /// <summary>
    /// HRESULT 0x80131505 (COR_E_TIMEOUT), mit dem die UI-Automation einen Timeout meldet.
    /// </summary>
    public const int UiaTimeoutHResult = -2146233083;

    /// <summary>
    /// Win32-Fehlercode 1460 (ERROR_TIMEOUT); als HRESULT 0x800705B4, mit dem <c>UIA3Automation.FromHandle</c> einen Timeout als <see cref="Win32Exception"/> meldet.
    /// </summary>
    public const int Win32TimeoutErrorCode = 1460;

    private const int Win32TimeoutHResult = unchecked((int)0x800705B4);

    /// <summary>
    /// Standardzahl der Versuche.
    /// </summary>
    public const int DefaultMaxAttempts = 3;

    /// <summary>
    /// Prüft, ob eine Ausnahme ein vorübergehender UIA-Timeout ist (<see cref="TimeoutException"/> oder <see cref="COMException"/> mit 0x80131505 oder <see cref="Win32Exception"/> mit 0x800705B4, auch als innere Ausnahme).
    /// </summary>
    /// <param name="exception">Die Ausnahme.</param>
    /// <returns><see langword="true"/>, wenn ein erneuter Versuch sinnvoll ist.</returns>
    public static bool IsTransient(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is TimeoutException || (current is COMException com && com.HResult == UiaTimeoutHResult) || IsWin32Timeout(current))
            {
                return true;
            }

            if (current.InnerException is null)
            {
                break;
            }
        }

        return false;
    }

    private static bool IsWin32Timeout(Exception exception)
    {
        return exception is Win32Exception win32
            && (win32.NativeErrorCode == Win32TimeoutErrorCode || win32.NativeErrorCode == Win32TimeoutHResult || win32.HResult == Win32TimeoutHResult);
    }

    /// <summary>
    /// Führt die Aktion aus und wiederholt sie bei einem vorübergehenden UIA-Timeout.
    /// Scheitert sie in allen Versuchen daran, wird eine <see cref="InvalidOperationException"/> mit klarer Meldung und dem letzten Fehler als innerer Ausnahme ausgelöst.
    /// </summary>
    /// <typeparam name="T">Der Ergebnistyp.</typeparam>
    /// <param name="action">Die Aktion.</param>
    /// <param name="description">Beschreibung der Aktion für die Fehlermeldung.</param>
    /// <param name="maxAttempts">Höchstzahl der Versuche (mindestens 1).</param>
    /// <param name="pause">Pause zwischen den Versuchen; ohne Angabe 2 Sekunden.</param>
    /// <param name="sleep">Ersatz für das Warten (für Tests); ohne Angabe <see cref="Thread.Sleep(TimeSpan)"/>.</param>
    /// <param name="backoffFactor">Faktor, mit dem die Pause je Versuch wächst (1 = konstant).</param>
    /// <param name="maxPause">Obergrenze einer einzelnen Pause; ohne Angabe unbegrenzt.</param>
    /// <param name="totalLimit">Gesamtzeitlimit; eine Pause, die es überschreiten würde, beendet die Wiederholungen. Ohne Angabe unbegrenzt.</param>
    /// <param name="elapsed">Ersatz für die seit dem Beginn vergangene Zeit (für Tests).</param>
    /// <returns>Das Ergebnis des ersten erfolgreichen Versuchs.</returns>
    public static T Run<T>(
        Func<T> action,
        string description,
        int maxAttempts = DefaultMaxAttempts,
        TimeSpan? pause = null,
        Action<TimeSpan>? sleep = null,
        double backoffFactor = 1.0,
        TimeSpan? maxPause = null,
        TimeSpan? totalLimit = null,
        Func<TimeSpan>? elapsed = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        var attempts = Math.Max(1, maxAttempts);
        var wait = pause ?? TimeSpan.FromSeconds(2);
        var doSleep = sleep ?? Thread.Sleep;
        Exception? last = null;
        var watch = Stopwatch.StartNew();
        var getElapsed = elapsed ?? (() => watch.Elapsed);
        var waited = TimeSpan.Zero;
        var made = 0;
        var limitHit = false;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            made = attempt;
            try
            {
                return action();
            }
            catch (Exception ex) when (IsTransient(ex))
            {
                last = ex;
                if (attempt < attempts)
                {
                    var next = maxPause is { } cap && wait > cap ? cap : wait;
                    if (totalLimit is { } limit && getElapsed() + next > limit)
                    {
                        limitHit = true;
                        break;
                    }

                    doSleep(next);
                    waited += next;
                    wait = TimeSpan.FromTicks((long)Math.Min(wait.Ticks * Math.Max(1.0, backoffFactor), TimeSpan.FromHours(1).Ticks));
                }
            }
        }

        var limitHint = limitHit ? $" (Gesamtzeitlimit {totalLimit!.Value.TotalSeconds:0.#} s erreicht)" : string.Empty;
        throw new InvalidOperationException(
            $"{description}: Die UI-Automation hat auch nach {made} Versuchen und {waited.TotalSeconds:0.#} s Wartezeit{limitHint} nicht rechtzeitig geantwortet (UIA-Timeout, HRESULT 0x80131505 bzw. 0x800705B4). Letzter Fehler: {last!.Message}",
            last);
    }

    /// <summary>
    /// Wie die Variante mit Ergebnis, für Aktionen ohne Ergebnis.
    /// </summary>
    /// <param name="action">Die Aktion.</param>
    /// <param name="description">Beschreibung der Aktion für die Fehlermeldung.</param>
    /// <param name="maxAttempts">Höchstzahl der Versuche (mindestens 1).</param>
    /// <param name="pause">Pause zwischen den Versuchen; ohne Angabe 2 Sekunden.</param>
    /// <param name="sleep">Ersatz für das Warten (für Tests).</param>
    public static void Run(Action action, string description, int maxAttempts = DefaultMaxAttempts, TimeSpan? pause = null, Action<TimeSpan>? sleep = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        Run(
            () =>
            {
                action();
                return true;
            },
            description,
            maxAttempts,
            pause,
            sleep);
    }
}

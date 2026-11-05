using System.Diagnostics;
using FlaUI.Core.Exceptions;

namespace Tankradar.TestSupport;

/// <summary>
/// Wartet auf eine Bedingung der UI-Automation bis zu einer Frist. Ein vorübergehender UIA-Timeout (siehe <see cref="TransientRetry.IsTransient"/>) und ein Element,
/// das währenddessen nicht mehr verfügbar ist (<c>FlaUI.Core.Exceptions.ElementNotAvailableException</c>, z. B. weil die Liste neu aufgebaut wird), zählen als „noch nicht erfüllt“;
/// gewartet wird weiter bis zur Frist. Andere Ausnahmen (Assertions, fehlende Elemente, echte Programmfehler) werden nie verdeckt.
/// </summary>
public static class TransientWait
{
    /// <summary>
    /// Wertet die Bedingung wiederholt aus, bis sie erfüllt ist oder die Frist abläuft.
    /// </summary>
    /// <param name="condition">Die Bedingung.</param>
    /// <param name="timeout">Die Frist.</param>
    /// <param name="lastTimeout">Der letzte vorübergehende Fehler (UIA-Timeout oder nicht mehr verfügbares Element), der beim Auswerten auftrat; <see langword="null"/>, wenn keiner auftrat.</param>
    /// <param name="poll">Die Pause zwischen den Auswertungen; ohne Angabe 100 ms.</param>
    /// <param name="sleep">Ersatz für das Warten (für Tests).</param>
    /// <param name="elapsed">Ersatz für die seit dem Beginn vergangene Zeit (für Tests).</param>
    /// <returns><see langword="true"/>, wenn die Bedingung erfüllt wurde.</returns>
    public static bool Until(Func<bool> condition, TimeSpan timeout, out Exception? lastTimeout, TimeSpan? poll = null, Action<TimeSpan>? sleep = null, Func<TimeSpan>? elapsed = null)
    {
        return Until(condition, timeout, out lastTimeout, out _, poll, sleep, elapsed);
    }

    /// <summary>
    /// Wertet die Bedingung wiederholt aus, bis sie erfüllt ist oder die Frist abläuft, und liefert zusätzlich die Zeit, die in vorübergehenden Fehlern verbracht wurde.
    /// Damit lassen sich Zeitgrenzen auf die Messzeit ohne UIA-Wartezeit beziehen.
    /// </summary>
    /// <param name="condition">Die Bedingung.</param>
    /// <param name="timeout">Die Frist.</param>
    /// <param name="lastTimeout">Der letzte vorübergehende Fehler, der beim Auswerten auftrat; <see langword="null"/>, wenn keiner auftrat.</param>
    /// <param name="timeoutTime">Die Summe der Zeit, die Auswertungen verbrachten, die mit einem vorübergehenden Fehler endeten.</param>
    /// <param name="poll">Die Pause zwischen den Auswertungen; ohne Angabe 100 ms.</param>
    /// <param name="sleep">Ersatz für das Warten (für Tests).</param>
    /// <param name="elapsed">Ersatz für die seit dem Beginn vergangene Zeit (für Tests).</param>
    /// <returns><see langword="true"/>, wenn die Bedingung erfüllt wurde.</returns>
    public static bool Until(Func<bool> condition, TimeSpan timeout, out Exception? lastTimeout, out TimeSpan timeoutTime, TimeSpan? poll = null, Action<TimeSpan>? sleep = null, Func<TimeSpan>? elapsed = null)
    {
        ArgumentNullException.ThrowIfNull(condition);
        var pause = poll ?? TimeSpan.FromMilliseconds(100);
        var doSleep = sleep ?? Thread.Sleep;
        var watch = Stopwatch.StartNew();
        var getElapsed = elapsed ?? (() => watch.Elapsed);
        lastTimeout = null;
        timeoutTime = TimeSpan.Zero;
        do
        {
            var attemptStart = getElapsed();
            try
            {
                if (condition())
                {
                    return true;
                }
            }
            catch (Exception ex) when (IsTransient(ex))
            {
                lastTimeout = ex;
                timeoutTime += getElapsed() - attemptStart;
            }

            doSleep(pause);
        }
        while (getElapsed() < timeout);

        return false;
    }

    /// <summary>
    /// Prüft, ob eine Ausnahme ein vorübergehender Fehler ist: ein UIA-Timeout oder ein nicht mehr verfügbares Element (auch als innere Ausnahme).
    /// </summary>
    /// <param name="exception">Die Ausnahme.</param>
    /// <returns><see langword="true"/>, wenn weiter gewartet werden darf.</returns>
    public static bool IsTransient(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (TransientRetry.IsTransient(exception))
        {
            return true;
        }

        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is ElementNotAvailableException)
            {
                return true;
            }
        }

        return false;
    }
}

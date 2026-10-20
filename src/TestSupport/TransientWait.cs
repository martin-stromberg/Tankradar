using System.Diagnostics;

namespace Tankradar.TestSupport;

/// <summary>
/// Wartet auf eine Bedingung der UI-Automation bis zu einer Frist. Ein vorübergehender UIA-Timeout (siehe <see cref="TransientRetry.IsTransient"/>) zählt als „noch nicht erfüllt“;
/// gewartet wird weiter bis zur Frist. Andere Ausnahmen (Assertions, fehlende Elemente, echte Programmfehler) werden nie verdeckt.
/// </summary>
public static class TransientWait
{
    /// <summary>
    /// Wertet die Bedingung wiederholt aus, bis sie erfüllt ist oder die Frist abläuft.
    /// </summary>
    /// <param name="condition">Die Bedingung.</param>
    /// <param name="timeout">Die Frist.</param>
    /// <param name="lastTimeout">Der letzte vorübergehende UIA-Timeout, der beim Auswerten auftrat; <see langword="null"/>, wenn keiner auftrat.</param>
    /// <param name="poll">Die Pause zwischen den Auswertungen; ohne Angabe 100 ms.</param>
    /// <param name="sleep">Ersatz für das Warten (für Tests).</param>
    /// <param name="elapsed">Ersatz für die seit dem Beginn vergangene Zeit (für Tests).</param>
    /// <returns><see langword="true"/>, wenn die Bedingung erfüllt wurde.</returns>
    public static bool Until(Func<bool> condition, TimeSpan timeout, out Exception? lastTimeout, TimeSpan? poll = null, Action<TimeSpan>? sleep = null, Func<TimeSpan>? elapsed = null)
    {
        ArgumentNullException.ThrowIfNull(condition);
        var pause = poll ?? TimeSpan.FromMilliseconds(100);
        var doSleep = sleep ?? Thread.Sleep;
        var watch = Stopwatch.StartNew();
        var getElapsed = elapsed ?? (() => watch.Elapsed);
        lastTimeout = null;
        do
        {
            try
            {
                if (condition())
                {
                    return true;
                }
            }
            catch (Exception ex) when (TransientRetry.IsTransient(ex))
            {
                lastTimeout = ex;
            }

            doSleep(pause);
        }
        while (getElapsed() < timeout);

        return false;
    }
}

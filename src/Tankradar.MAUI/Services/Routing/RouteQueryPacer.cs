using Tankradar.MAUI.Services.Pricing;

namespace Tankradar.MAUI.Services.Routing;

/// <summary>
/// Sorgt für den Mindestabstand zwischen den Preisabfragen der Routensuche (<see cref="RouteSearchOptions.MinQueryInterval"/>), auch über mehrere Suchen hinweg.
/// Der Abstand gilt zusätzlich zur allgemeinen Drosselung des Preisdienstes (<see cref="RequestThrottle"/>), über die alle Abfragen der App laufen; die Routensuche
/// umgeht diese also nicht, sie wartet nur länger. Die Zeit wird monoton gemessen, damit Änderungen der Systemuhr den Abstand nicht verkürzen.
/// </summary>
public sealed class RouteQueryPacer
{
    private readonly object _gate = new();
    private readonly RouteSearchOptions _options;
    private readonly IDelay _delay;
    private readonly TimeProvider _timeProvider;
    private long? _lastQuery;

    /// <summary>
    /// Erstellt den Taktgeber.
    /// </summary>
    /// <param name="options">Die Einstellungen der Routensuche.</param>
    /// <param name="delay">Das Warteverfahren.</param>
    /// <param name="timeProvider">Die Zeitquelle.</param>
    public RouteQueryPacer(RouteSearchOptions options, IDelay delay, TimeProvider timeProvider)
    {
        _options = options;
        _delay = delay;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Wartet bei Bedarf, bis seit der letzten vermerkten Abfrage der Mindestabstand vergangen ist.
    /// </summary>
    /// <param name="cancellationToken">Abbruchsignal; ein Abbruch beendet auch die Wartezeit.</param>
    /// <returns>Ein Task, der abgeschlossen ist, sobald die nächste Abfrage gesendet werden darf.</returns>
    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        TimeSpan remaining;
        lock (_gate)
        {
            remaining = _lastQuery is { } last ? _options.MinQueryInterval - _timeProvider.GetElapsedTime(last) : TimeSpan.Zero;
        }

        if (remaining > TimeSpan.Zero)
        {
            await _delay.DelayAsync(remaining, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Vermerkt, dass soeben eine Abfrage an den Preisdienst gegangen ist.
    /// </summary>
    public void MarkQueried()
    {
        lock (_gate)
        {
            _lastQuery = _timeProvider.GetTimestamp();
        }
    }
}

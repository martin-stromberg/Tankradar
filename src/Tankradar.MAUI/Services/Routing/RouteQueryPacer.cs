using Tankradar.MAUI.Services.Pricing;

namespace Tankradar.MAUI.Services.Routing;

/// <summary>
/// Sorgt für den Mindestabstand zwischen den Preisabfragen der Routensuche (<see cref="RouteSearchOptions.MinQueryInterval"/> zuzüglich der Reserve <see cref="RouteSearchOptions.QueryIntervalReserve"/>), auch über mehrere Suchen hinweg und
/// bei gleichzeitigen Aufrufern. Jede Abfrage reserviert einen Zeitpunkt (Absendezeitpunkt): der erste Aufrufer sofort, jeder weitere frühestens ein Intervall (Mindestabstand plus Reserve) nach dem
/// vorherigen reservierten Zeitpunkt. Die Reservierung gilt auch, wenn die Abfrage danach abgebrochen wird, während sie unterwegs ist; nur ein Abbruch in der Wartezeit
/// (es wurde nichts gesendet) gibt sie wieder frei. Der Abstand gilt zusätzlich zur allgemeinen Drosselung des Preisdienstes (<see cref="RequestThrottle"/>), über die alle
/// Abfragen der App laufen; die Routensuche umgeht diese also nicht, sie wartet nur länger. Die Zeit wird monoton gemessen.
/// </summary>
public sealed class RouteQueryPacer
{
    private readonly object _gate = new();
    private readonly RouteSearchOptions _options;
    private readonly IDelay _delay;
    private readonly TimeProvider _timeProvider;
    private long? _nextSlot;

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
    /// Reserviert den nächsten Zeitpunkt für eine Abfrage und wartet bei Bedarf bis dahin. Die Abfrage darf unmittelbar danach gesendet werden und muss dann auch gesendet werden.
    /// </summary>
    /// <param name="cancellationToken">Abbruchsignal; ein Abbruch beendet die Wartezeit und gibt die Reservierung wieder frei.</param>
    /// <returns>Ein Task, der abgeschlossen ist, sobald die Abfrage gesendet werden darf.</returns>
    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        long? previous;
        long slot;
        TimeSpan remaining;
        lock (_gate)
        {
            var now = _timeProvider.GetTimestamp();
            previous = _nextSlot;
            slot = previous is { } next && _timeProvider.GetElapsedTime(now, next) > TimeSpan.Zero ? next : now;
            remaining = _timeProvider.GetElapsedTime(now, slot);
            _nextSlot = slot + (long)(_options.EffectiveQueryInterval.TotalSeconds * _timeProvider.TimestampFrequency);
        }

        if (remaining <= TimeSpan.Zero)
        {
            return;
        }

        var reserved = slot + (long)(_options.EffectiveQueryInterval.TotalSeconds * _timeProvider.TimestampFrequency);
        try
        {
            await _delay.DelayAsync(remaining, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            lock (_gate)
            {
                // Es wurde nichts gesendet: Nur wenn niemand nach uns reserviert hat, wird der Zeitpunkt wieder frei.
                if (_nextSlot == reserved)
                {
                    _nextSlot = previous;
                }
            }

            throw;
        }
    }
}

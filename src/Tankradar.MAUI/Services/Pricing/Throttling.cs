namespace Tankradar.MAUI.Services.Pricing;

/// <summary>
/// Abstraktion des Wartens, damit Wartezeiten in Tests ohne echte Verzögerung geprüft werden können.
/// </summary>
public interface IDelay
{
    /// <summary>
    /// Wartet die angegebene Zeit.
    /// </summary>
    /// <param name="delay">Die Wartezeit.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Ein Task, der nach der Wartezeit abgeschlossen ist.</returns>
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}

/// <summary>
/// Echte Verzögerung über <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.
/// </summary>
public sealed class TaskDelay : IDelay
{
    /// <inheritdoc />
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        return delay <= TimeSpan.Zero ? Task.CompletedTask : Task.Delay(delay, cancellationToken);
    }
}

/// <summary>
/// Stellt sicher, dass zwischen zwei Anfragen an den Preisdienst ein Mindestabstand liegt; Anfragen werden dabei nacheinander abgewickelt.
/// </summary>
public sealed class RequestThrottle
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly TimeSpan _minInterval;
    private readonly IDelay _delay;
    private readonly TimeProvider _timeProvider;
    private long? _lastRequest;

    /// <summary>
    /// Erstellt die Drosselung.
    /// </summary>
    /// <param name="minInterval">Der Mindestabstand zwischen zwei Anfragen.</param>
    /// <param name="delay">Das Warteverfahren.</param>
    /// <param name="timeProvider">Die Zeitquelle.</param>
    public RequestThrottle(TimeSpan minInterval, IDelay delay, TimeProvider timeProvider)
    {
        _minInterval = minInterval;
        _delay = delay;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Wartet bei Bedarf bis zum Ablauf des Mindestabstands und vermerkt danach den Anfragezeitpunkt.
    /// </summary>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Ein Task, der abgeschlossen ist, sobald die nächste Anfrage gesendet werden darf.</returns>
    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_lastRequest is { } last)
            {
                // Monotone Uhr: Änderungen der Systemzeit dürfen den Abstand nicht verkürzen.
                var remaining = _minInterval - _timeProvider.GetElapsedTime(last);
                if (remaining > TimeSpan.Zero)
                {
                    await _delay.DelayAsync(remaining, cancellationToken).ConfigureAwait(false);
                }
            }

            _lastRequest = _timeProvider.GetTimestamp();
        }
        finally
        {
            _gate.Release();
        }
    }
}

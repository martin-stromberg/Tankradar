namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// <see cref="SynchronizationContext"/> für Tests, der weitergereichte Rückrufe in einer Warteschlange sammelt, bis <see cref="RunAll"/> sie wie ein UI-Thread
/// abarbeitet (während der Abarbeitung ist der Kontext der aktuelle).
/// </summary>
public sealed class QueueSynchronizationContext : SynchronizationContext
{
    private readonly Queue<Action> _pending = new();

    /// <summary>
    /// Die Anzahl der noch nicht abgearbeiteten Rückrufe.
    /// </summary>
    public int PendingCount
    {
        get
        {
            lock (_pending)
            {
                return _pending.Count;
            }
        }
    }

    /// <inheritdoc />
    public override void Post(SendOrPostCallback d, object? state)
    {
        lock (_pending)
        {
            _pending.Enqueue(() => d(state));
        }
    }

    /// <summary>
    /// Arbeitet alle wartenden Rückrufe (auch währenddessen neu hinzugekommene) mit diesem Kontext als aktuellem Kontext ab.
    /// </summary>
    public void RunAll()
    {
        var previous = Current;
        SetSynchronizationContext(this);
        try
        {
            while (TryDequeue() is { } next)
            {
                next();
            }
        }
        finally
        {
            SetSynchronizationContext(previous);
        }
    }

    /// <summary>
    /// Führt eine Aktion mit diesem Kontext als aktuellem Kontext aus und arbeitet danach die wartenden Rückrufe ab.
    /// </summary>
    /// <param name="action">Die Aktion.</param>
    public void Run(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Post(_ => action(), null);
        RunAll();
    }

    private Action? TryDequeue()
    {
        lock (_pending)
        {
            return _pending.Count > 0 ? _pending.Dequeue() : null;
        }
    }
}

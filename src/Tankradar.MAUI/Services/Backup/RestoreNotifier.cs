namespace Tankradar.MAUI.Services.Backup;

/// <summary>
/// Umsetzung von <see cref="IRestoreNotifier"/> mit schwach gehaltenen Abonnenten (<see cref="WeakEventManager"/>).
/// Abonnenten müssen Instanzmethoden verwenden; ein Lambda-Ausdruck würde sonst vorzeitig freigegeben.
/// </summary>
public sealed class RestoreNotifier : IRestoreNotifier
{
    private const string EventName = nameof(Restored);

    private readonly WeakEventManager _manager = new();
    private int _generation;

    /// <inheritdoc />
    public event EventHandler? Restored
    {
        add { _manager.AddEventHandler(value!, EventName); }
        remove { _manager.RemoveEventHandler(value!, EventName); }
    }

    /// <inheritdoc />
    public int Generation
    {
        get { return _generation; }
    }

    /// <inheritdoc />
    public void NotifyRestored()
    {
        Interlocked.Increment(ref _generation);
        _manager.HandleEvent(this, EventArgs.Empty, EventName);
    }
}

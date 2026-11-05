namespace Tankradar.MAUI.Services.Pricing;

/// <summary>
/// Quelle des Netzwerkzustands (auf dem Gerät: die MAUI-Konnektivität; in Tests ein Ersatz).
/// </summary>
public interface INetworkStatusSource
{
    /// <summary>
    /// Gibt an, ob das Betriebssystem aktuell eine Internetverbindung meldet.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Wird ausgelöst, wenn sich der Netzwerkzustand geändert haben könnte.
    /// </summary>
    event EventHandler? StatusChanged;
}

/// <summary>
/// Erkennt, ob eine Netzverbindung besteht und wann sie wiederhergestellt wird.
/// </summary>
public interface IConnectionMonitor
{
    /// <summary>
    /// Gibt an, ob aktuell eine Netzverbindung besteht.
    /// </summary>
    bool IsOnline { get; }

    /// <summary>
    /// Wird ausgelöst, wenn sich der Verbindungszustand ändert; das Argument ist der neue Zustand (online = <see langword="true"/>).
    /// </summary>
    event EventHandler<bool>? ConnectionChanged;

    /// <summary>
    /// Wird ausgelöst, wenn die Verbindung nach einem Ausfall wiederhergestellt wurde. Ansichten nutzen dies, um veraltete Preise zu aktualisieren und den Offline-Hinweis zu entfernen.
    /// </summary>
    event EventHandler? ConnectionRestored;
}

/// <summary>
/// Standardumsetzung von <see cref="IConnectionMonitor"/> auf Basis einer <see cref="INetworkStatusSource"/>.
/// </summary>
public sealed class ConnectionMonitor : IConnectionMonitor, IDisposable
{
    private readonly INetworkStatusSource _source;
    private readonly object _gate = new();
    private bool _isOnline;

    /// <summary>
    /// Erstellt den Monitor und liest den Anfangszustand.
    /// </summary>
    /// <param name="source">Die Quelle des Netzwerkzustands.</param>
    public ConnectionMonitor(INetworkStatusSource source)
    {
        _source = source;
        _isOnline = source.IsConnected;
        _source.StatusChanged += OnStatusChanged;
    }

    /// <inheritdoc />
    public event EventHandler<bool>? ConnectionChanged;

    /// <inheritdoc />
    public event EventHandler? ConnectionRestored;

    /// <inheritdoc />
    public bool IsOnline
    {
        get
        {
            lock (_gate)
            {
                return _isOnline;
            }
        }
    }

    /// <summary>
    /// Meldet sich bei der Quelle ab.
    /// </summary>
    public void Dispose()
    {
        _source.StatusChanged -= OnStatusChanged;
    }

    private void OnStatusChanged(object? sender, EventArgs e)
    {
        bool current;
        bool changed;
        lock (_gate)
        {
            current = _source.IsConnected;
            changed = current != _isOnline;
            _isOnline = current;
        }

        if (!changed)
        {
            return;
        }

        ConnectionChanged?.Invoke(this, current);
        if (current)
        {
            ConnectionRestored?.Invoke(this, EventArgs.Empty);
        }
    }
}

/// <summary>
/// Netzwerkzustand über <see cref="Connectivity"/> der MAUI-Plattform. Ist der Zustand nicht ermittelbar, gilt die Verbindung als vorhanden; ein Abruf entscheidet dann selbst.
/// </summary>
public sealed class MauiNetworkStatusSource : INetworkStatusSource
{
    /// <summary>
    /// Bewertet den von der Plattform gemeldeten Netzzugang: Internet, eingeschränktes Internet und „unbekannt“ gelten als verbunden
    /// (ein Abruf entscheidet dann selbst); nur „kein Netz“ und „nur lokal“ gelten als offline.
    /// </summary>
    /// <param name="access">Der gemeldete Netzzugang.</param>
    /// <returns><see langword="true"/>, wenn ein Abruf versucht werden soll.</returns>
    public static bool IsReachable(NetworkAccess access)
    {
        return access is NetworkAccess.Internet or NetworkAccess.ConstrainedInternet or NetworkAccess.Unknown;
    }

    /// <inheritdoc />
    public event EventHandler? StatusChanged
    {
        add
        {
            _handlers += value;
            EnsureSubscribed();
        }
        remove => _handlers -= value;
    }

    /// <inheritdoc />
    public bool IsConnected
    {
        get
        {
            try
            {
                return IsReachable(Connectivity.Current.NetworkAccess);
            }
            catch (Exception)
            {
                return true;
            }
        }
    }

    private event EventHandler? _handlers;

    private bool _subscribed;

    private void EnsureSubscribed()
    {
        if (_subscribed)
        {
            return;
        }

        try
        {
            Connectivity.Current.ConnectivityChanged += (_, _) => _handlers?.Invoke(this, EventArgs.Empty);
            _subscribed = true;
        }
        catch (Exception)
        {
            // Ohne Plattformunterstützung bleibt der Zustand unverändert.
        }
    }
}

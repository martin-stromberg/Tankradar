using Tankradar.MAUI.Services.Pricing;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// <see cref="INetworkStatusSource"/> für Tests mit von Hand gesteuertem Zustand.
/// </summary>
public sealed class FakeNetworkStatusSource : INetworkStatusSource
{
    /// <inheritdoc />
    public event EventHandler? StatusChanged;

    /// <inheritdoc />
    public bool IsConnected { get; set; } = true;

    /// <summary>
    /// Anzahl der aktuell angemeldeten Beobachter.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public int SubscriberCount => StatusChanged?.GetInvocationList().Length ?? 0;

    /// <summary>
    /// Setzt den Zustand und meldet die Änderung.
    /// </summary>
    /// <param name="connected">Der neue Zustand.</param>
    public void Change(bool connected)
    {
        IsConnected = connected;
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// <see cref="IConnectionMonitor"/> für Tests mit von Hand gesteuertem Zustand.
/// </summary>
public sealed class FakeConnectionMonitor : IConnectionMonitor
{
    /// <inheritdoc />
    public event EventHandler<bool>? ConnectionChanged;

    /// <inheritdoc />
    public event EventHandler? ConnectionRestored;

    /// <inheritdoc />
    public bool IsOnline { get; set; } = true;

    /// <summary>
    /// Setzt den Zustand und löst die passenden Ereignisse aus.
    /// </summary>
    /// <param name="online">Der neue Zustand.</param>
    public void Change(bool online)
    {
        IsOnline = online;
        ConnectionChanged?.Invoke(this, online);
        if (online)
        {
            ConnectionRestored?.Invoke(this, EventArgs.Empty);
        }
    }
}

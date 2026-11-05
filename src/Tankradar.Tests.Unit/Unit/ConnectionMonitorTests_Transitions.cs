using Tankradar.MAUI.Services.Pricing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Erkennung von Verbindungsverlust und Wiederherstellung.
/// </summary>
public class ConnectionMonitorTests_Transitions : BaseTest
{
    private readonly FakeNetworkStatusSource _source = new();

    /// <summary>
    /// Prüft, dass der Anfangszustand der Quelle übernommen wird.
    /// </summary>
    [Fact]
    public void Constructor_ReadsInitialState()
    {
        _source.IsConnected = false;

        using var monitor = new ConnectionMonitor(_source);

        Assert.False(monitor.IsOnline);
    }

    /// <summary>
    /// Prüft, dass eine wiederhergestellte Verbindung gemeldet wird.
    /// </summary>
    [Fact]
    public void StatusChange_OfflineToOnline_RaisesRestored()
    {
        _source.IsConnected = false;
        using var monitor = new ConnectionMonitor(_source);
        var changes = new List<bool>();
        var restored = 0;
        monitor.ConnectionChanged += (_, online) => changes.Add(online);
        monitor.ConnectionRestored += (_, _) => restored++;

        _source.Change(true);

        Assert.True(monitor.IsOnline);
        Assert.Equal([true], changes);
        Assert.Equal(1, restored);
    }

    /// <summary>
    /// Prüft, dass ein Verbindungsverlust gemeldet wird, aber nicht als Wiederherstellung.
    /// </summary>
    [Fact]
    public void StatusChange_OnlineToOffline_RaisesChangedOnly()
    {
        using var monitor = new ConnectionMonitor(_source);
        var changes = new List<bool>();
        var restored = 0;
        monitor.ConnectionChanged += (_, online) => changes.Add(online);
        monitor.ConnectionRestored += (_, _) => restored++;

        _source.Change(false);

        Assert.False(monitor.IsOnline);
        Assert.Equal([false], changes);
        Assert.Equal(0, restored);
    }

    /// <summary>
    /// Prüft, dass gleichbleibender Zustand keine Ereignisse auslöst.
    /// </summary>
    [Fact]
    public void StatusChange_SameState_RaisesNothing()
    {
        using var monitor = new ConnectionMonitor(_source);
        var events = 0;
        monitor.ConnectionChanged += (_, _) => events++;
        monitor.ConnectionRestored += (_, _) => events++;

        _source.Change(true);

        Assert.Equal(0, events);
    }

    /// <summary>
    /// Prüft den vollständigen Zyklus online, offline, online.
    /// </summary>
    [Fact]
    public void StatusChange_FullCycle_RestoredRaisedOncePerRecovery()
    {
        using var monitor = new ConnectionMonitor(_source);
        var restored = 0;
        monitor.ConnectionRestored += (_, _) => restored++;

        _source.Change(false);
        _source.Change(true);
        _source.Change(false);
        _source.Change(true);

        Assert.Equal(2, restored);
    }

    /// <summary>
    /// Prüft, dass nach dem Freigeben keine Änderungen mehr beobachtet werden.
    /// </summary>
    [Fact]
    public void Dispose_UnsubscribesFromSource()
    {
        var monitor = new ConnectionMonitor(_source);
        Assert.Equal(1, _source.SubscriberCount);

        monitor.Dispose();

        Assert.Equal(0, _source.SubscriberCount);
    }
}

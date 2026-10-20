using System.Runtime.CompilerServices;
using Tankradar.MAUI.Services.Backup;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Benachrichtigung des <see cref="RestoreNotifier"/> nach einer Wiederherstellung (H5): Ereignis, Zähler und schwach gehaltene Abonnenten.
/// </summary>
public class RestoreNotifierTests : BaseTest
{
    /// <summary>
    /// Prüft, dass jeder Abonnent informiert und der Zähler erhöht wird.
    /// </summary>
    [Fact]
    public void NotifyRestored_RaisesEventForAllSubscribersAndCounts()
    {
        var notifier = new RestoreNotifier();
        var first = new Subscriber();
        var second = new Subscriber();
        notifier.Restored += first.OnRestored;
        notifier.Restored += second.OnRestored;

        notifier.NotifyRestored();
        notifier.NotifyRestored();

        Assert.Equal(2, first.Calls);
        Assert.Equal(2, second.Calls);
        Assert.Equal(2, notifier.Generation);
    }

    /// <summary>
    /// Prüft, dass ein abgemeldeter Abonnent nicht mehr informiert wird.
    /// </summary>
    [Fact]
    public void Unsubscribed_IsNotNotified()
    {
        var notifier = new RestoreNotifier();
        var subscriber = new Subscriber();
        notifier.Restored += subscriber.OnRestored;
        notifier.Restored -= subscriber.OnRestored;

        notifier.NotifyRestored();

        Assert.Equal(0, subscriber.Calls);
    }

    /// <summary>
    /// Prüft, dass ohne Abonnent keine Ausnahme entsteht und der Zähler trotzdem zählt.
    /// </summary>
    [Fact]
    public void NotifyRestored_WithoutSubscribers_OnlyCounts()
    {
        var notifier = new RestoreNotifier();

        notifier.NotifyRestored();

        Assert.Equal(1, notifier.Generation);
    }

    /// <summary>
    /// Prüft, dass der Dienst einen Abonnenten nicht am Leben hält: Er wird freigegeben und nicht mehr benachrichtigt.
    /// </summary>
    [Fact]
    public void Subscriber_IsHeldWeakly()
    {
        var notifier = new RestoreNotifier();
        var weak = Subscribe(notifier);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        notifier.NotifyRestored();

        Assert.False(weak.TryGetTarget(out _));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<Subscriber> Subscribe(RestoreNotifier notifier)
    {
        var subscriber = new Subscriber();
        notifier.Restored += subscriber.OnRestored;
        return new WeakReference<Subscriber>(subscriber);
    }

    private sealed class Subscriber
    {
        public int Calls { get; private set; }

        public void OnRestored(object? sender, EventArgs e)
        {
            Calls++;
        }
    }
}

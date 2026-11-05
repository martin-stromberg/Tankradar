using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Threading, Abbruch und Zusammenführung der Startseite: Verbindungsereignisse im UI-Kontext, kein Standort nach dem Verlassen, keine verlorenen Preise bei Favoritenänderung.
/// </summary>
public class FavoritesViewModelTests_Threading : FavoritesViewModelTestBase
{
    private sealed class QueueContext : SynchronizationContext
    {
        public Queue<Action> Pending { get; } = new();

        public override void Post(SendOrPostCallback d, object? state)
        {
            lock (Pending)
            {
                Pending.Enqueue(() => d(state));
            }
        }

        public void RunAll()
        {
            while (true)
            {
                Action? next;
                lock (Pending)
                {
                    next = Pending.Count > 0 ? Pending.Dequeue() : null;
                }

                if (next is null)
                {
                    return;
                }

                next();
            }
        }
    }

    /// <summary>
    /// Prüft, dass Verbindungsereignisse von einem Hintergrund-Thread erst im Erzeugungskontext des ViewModels verarbeitet werden.
    /// </summary>
    [Fact]
    public async Task ConnectionEvents_FromBackgroundThread_AreMarshalledToCreationContext()
    {
        var stale = SeedPrices(Clock.UtcNow.AddMinutes(-90));
        Prices.RefreshResult = new StationRefreshResult(stale, PriceDataSource.Live, PriceFailure.None);
        await CreateGroupAsync("Heimat", FavoritesFixture.StationA);
        var previous = SynchronizationContext.Current;
        var context = new QueueContext();
        MAUI.ViewModels.FavoritesViewModel viewModel;
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            viewModel = CreateViewModel();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        await viewModel.LoadAsync();
        viewModel.OnAppearing();
        await viewModel.LastTask;
        Assert.Single(Prices.RefreshRequests);

        await Task.Run(() => Connection.Change(false));
        await Task.Run(() => Connection.Change(true));

        Assert.Single(Prices.RefreshRequests);
        Assert.Equal(3, context.Pending.Count);

        context.RunAll();
        await viewModel.LastTask;

        Assert.Equal(2, Prices.RefreshRequests.Count);
    }

    /// <summary>
    /// Prüft, dass ein während der Ortung verlassener Abruf weder die Position behält noch Preise abruft.
    /// </summary>
    [Fact]
    public async Task Disappearing_DuringLocation_KeepsNoPositionAndSkipsRequest()
    {
        SeedPrices(FreshTime);
        await CreateGroupAsync("Heimat", FavoritesFixture.StationA);
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();
        Location.OnCall = viewModel.OnDisappearing;

        await viewModel.RefreshAsync(force: false);

        Assert.Empty(Prices.RefreshRequests);
        Assert.False(viewModel.Groups[0].Stations[0].HasDistance);
        Assert.False(viewModel.IsBusy);
    }

    /// <summary>
    /// Prüft, dass ein während der Ortung verlassener Abruf ohne Kennungen weder neu aufbaut noch „In der Nähe“ lädt.
    /// </summary>
    [Fact]
    public async Task Disappearing_DuringLocation_WithoutStations_SkipsNearby()
    {
        Settings.Stored = AppSettings.CreateDefault() with { ShowNearby = true };
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();
        Location.OnCall = viewModel.OnDisappearing;

        await viewModel.RefreshAsync(force: false);

        Assert.Empty(Prices.Queries);
    }

    /// <summary>
    /// Prüft, dass eine während des Abrufs hinzugekommene Tankstelle ihre lokal bekannten Preise behält.
    /// </summary>
    [Fact]
    public async Task FavoriteAddedDuringRefresh_KeepsPricesOfNewStation()
    {
        var all = SeedPrices(FreshTime);
        var group = await CreateGroupAsync("Heimat", FavoritesFixture.StationA);
        var gate = new TaskCompletionSource();
        Prices.RefreshGate = gate.Task;
        Prices.RefreshResult = new StationRefreshResult([all[0]], PriceDataSource.Live, PriceFailure.None);
        var viewModel = CreateViewModel();
        viewModel.OnAppearing();
        await Prices.RefreshEntered.Task.WaitAsync(TimeSpan.FromSeconds(30));

        await Fixture.Service.AddStationAsync(group, FavoritesFixture.StationB);
        await viewModel.LoadAsync();
        gate.SetResult();
        await viewModel.LastTask;

        Assert.Equal(2, viewModel.Groups[0].Stations.Count);
        Assert.All(viewModel.Groups[0].Stations, station => Assert.True(station.HasPrices));
    }
}

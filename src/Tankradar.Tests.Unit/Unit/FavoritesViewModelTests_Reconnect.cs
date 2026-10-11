using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Offline-Hinweis und automatische Aktualisierung der Startseite nach Wiederverbindung.
/// </summary>
public class FavoritesViewModelTests_Reconnect : FavoritesViewModelTestBase
{
    private async Task<MAUI.ViewModels.FavoritesViewModel> AppearAsync(IReadOnlyList<StationInfo> stations, PriceDataSource source, PriceFailure failure)
    {
        Prices.RefreshResult = new StationRefreshResult(stations, source, failure);
        await CreateGroupAsync("Heimat", FavoritesFixture.StationA);
        var viewModel = CreateViewModel();
        viewModel.OnAppearing();
        await viewModel.LastTask;
        return viewModel;
    }

    /// <summary>
    /// Prüft, dass offline die zuletzt bekannten Preise mit Alter und der Hinweis in der Kopfzeile erscheinen.
    /// </summary>
    [Fact]
    public async Task Offline_ShowsLastKnownPricesAndBanner()
    {
        var stations = SeedPrices(Clock.UtcNow.AddMinutes(-75));
        Connection.IsOnline = false;

        var viewModel = await AppearAsync(stations, PriceDataSource.OfflineFallback, PriceFailure.Offline);

        Assert.True(viewModel.IsOffline);
        Assert.Equal(SearchTexts.OfflineBanner, viewModel.OfflineMessage);
        var line = viewModel.Groups[0].Stations[0].PriceLines[0];
        Assert.Equal("vor 75 Min.", line.AgeText);
        Assert.True(line.IsStale);
        Assert.False(viewModel.CanRefresh);
        Assert.False(viewModel.HasStatusMessage);
    }

    /// <summary>
    /// Prüft, dass nach Wiederverbindung veraltete Preise neu abgerufen werden und der Offline-Hinweis entfällt.
    /// </summary>
    [Fact]
    public async Task ConnectionRestored_RefreshesStalePricesAndClearsBanner()
    {
        var stale = SeedPrices(Clock.UtcNow.AddMinutes(-90));
        Connection.IsOnline = false;
        var viewModel = await AppearAsync(stale, PriceDataSource.OfflineFallback, PriceFailure.Offline);
        var fresh = SeedPrices(Clock.UtcNow);
        Prices.RefreshResult = new StationRefreshResult(fresh, PriceDataSource.Live, PriceFailure.None);

        Connection.Change(true);
        await viewModel.LastTask;

        Assert.Equal(2, Prices.RefreshRequests.Count);
        Assert.False(viewModel.IsOffline);
        Assert.Null(viewModel.OfflineMessage);
        Assert.Equal("vor 0 Min.", viewModel.Groups[0].Stations[0].PriceLines[0].AgeText);
        Assert.True(viewModel.CanRefresh);
    }

    /// <summary>
    /// Prüft, dass nach Wiederverbindung mit frischen Preisen kein weiterer Abruf erfolgt.
    /// </summary>
    [Fact]
    public async Task ConnectionRestored_FreshPrices_DoesNotQueryAgain()
    {
        var stations = SeedPrices(FreshTime);
        await AppearAsync(stations, PriceDataSource.Live, PriceFailure.None);

        Connection.Change(false);
        Connection.Change(true);

        Assert.Single(Prices.RefreshRequests);
    }

    /// <summary>
    /// Prüft, dass nach einem Offline-Rückfall bei bestehender Verbindung die Wiederverbindung den Abruf auslöst, auch wenn die Preise nicht veraltet sind.
    /// </summary>
    [Fact]
    public async Task ConnectionRestored_AfterOfflineFallback_Refreshes()
    {
        var stations = SeedPrices(FreshTime);
        var viewModel = await AppearAsync(stations, PriceDataSource.OfflineFallback, PriceFailure.Unreachable);
        Prices.RefreshResult = new StationRefreshResult(stations, PriceDataSource.Live, PriceFailure.None);

        Connection.Change(true);
        await viewModel.LastTask;

        Assert.Equal(2, Prices.RefreshRequests.Count);
        Assert.False(viewModel.IsOffline);
    }

    /// <summary>
    /// Prüft, dass nach dem Verlassen der Seite eine Wiederverbindung ignoriert wird.
    /// </summary>
    [Fact]
    public async Task ConnectionRestored_AfterDisappearing_IsIgnored()
    {
        var stations = SeedPrices(Clock.UtcNow.AddMinutes(-90));
        var viewModel = await AppearAsync(stations, PriceDataSource.OfflineFallback, PriceFailure.Offline);

        viewModel.OnDisappearing();
        Connection.Change(true);

        Assert.Single(Prices.RefreshRequests);
    }

    /// <summary>
    /// Prüft, dass der Verlust der Verbindung den Hinweis sofort zeigt und die Aktualisierung sperrt.
    /// </summary>
    [Fact]
    public async Task ConnectionLost_ShowsBannerImmediately()
    {
        var stations = SeedPrices(FreshTime);
        var viewModel = await AppearAsync(stations, PriceDataSource.Live, PriceFailure.None);
        Assert.False(viewModel.IsOffline);

        Connection.Change(false);

        Assert.True(viewModel.IsOffline);
        Assert.False(viewModel.CanRefresh);
    }

    /// <summary>
    /// Prüft, dass nach Wiederverbindung auch „In der Nähe“ neu geladen wird, wenn dessen letztes Ergebnis ein Offline-Stand war.
    /// </summary>
    [Fact]
    public async Task ConnectionRestored_ReloadsNearbyWhenOfflineResult()
    {
        Settings.Stored = AppSettings.CreateDefault() with { ShowNearby = true };
        var stations = SeedPrices(FreshTime);
        Prices.Result = new StationSearchResult(stations, PriceDataSource.OfflineFallback, PriceFailure.Unreachable);
        var viewModel = CreateViewModel();
        viewModel.OnAppearing();
        await viewModel.LastTask;
        Assert.True(viewModel.IsOffline);
        Prices.Result = new StationSearchResult(stations, PriceDataSource.Live, PriceFailure.None);

        Connection.Change(true);
        await viewModel.LastTask;

        Assert.Equal(2, Prices.Queries.Count);
        Assert.False(viewModel.IsOffline);
        Assert.False(viewModel.Nearby.IsOfflineResult);
    }
}

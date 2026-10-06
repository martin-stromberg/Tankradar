using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Offline-Hinweis und Verbindungsereignis des <see cref="MAUI.ViewModels.MapViewModel"/>.
/// </summary>
public class MapViewModelTests_Offline : MapViewModelTestBase
{
    /// <summary>
    /// Prüft, dass ohne Verbindung der Hinweis in der Kopfzeile erscheint.
    /// </summary>
    [Fact]
    public async Task NoConnection_ShowsBanner()
    {
        Connection.IsOnline = false;
        var offlineViewModel = new MAUI.ViewModels.MapViewModel(Settings, Location, Geocoding, Prices, Connection, Clock, Logger);

        offlineViewModel.OnAppearing();
        await offlineViewModel.LastSettingsTask;

        Assert.True(offlineViewModel.IsOffline);
        Assert.Equal(SearchTexts.OfflineBanner, offlineViewModel.OfflineMessage);
    }

    /// <summary>
    /// Prüft, dass mit Verbindung kein Hinweis erscheint.
    /// </summary>
    [Fact]
    public async Task Connected_ShowsNoBanner()
    {
        await AppearAsync();

        Assert.False(ViewModel.IsOffline);
        Assert.Null(ViewModel.OfflineMessage);
    }

    /// <summary>
    /// Prüft, dass zuletzt bekannte Preise den Hinweis samt Quellenhinweis auslösen, auch bei bestehender Verbindung.
    /// </summary>
    [Fact]
    public async Task OfflineFallback_ShowsBannerAndSourceNote()
    {
        Prices.Result = Result(PriceDataSource.OfflineFallback, PriceFailure.Unreachable, TwoStations());
        await AppearAsync();

        await SearchAsync();

        Assert.True(ViewModel.IsOffline);
        Assert.Equal(SearchTexts.OfflineFallbackNote, ViewModel.SourceNote);
        Assert.Equal(2, ViewModel.Stations.Count);
    }

    /// <summary>
    /// Prüft, dass eine spätere Live-Suche den Hinweis wieder entfernt.
    /// </summary>
    [Fact]
    public async Task LiveSearchAfterFallback_ClearsBannerAndSourceNote()
    {
        Prices.Result = Result(PriceDataSource.OfflineFallback, PriceFailure.Unreachable, TwoStations());
        await AppearAsync();
        await SearchAsync();

        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await SearchAsync();

        Assert.False(ViewModel.IsOffline);
        Assert.Null(ViewModel.SourceNote);
    }

    /// <summary>
    /// Prüft, dass ein Verbindungswechsel den Hinweis ein- und ausblendet.
    /// </summary>
    [Fact]
    public async Task ConnectionChanged_TogglesBanner()
    {
        await AppearAsync();

        Connection.Change(false);
        Assert.True(ViewModel.IsOffline);

        Connection.Change(true);
        Assert.False(ViewModel.IsOffline);
    }

    /// <summary>
    /// Prüft, dass nach <c>OnDisappearing</c> Verbindungswechsel keine Wirkung mehr haben und ein erneutes Erscheinen sich nur einmal anmeldet.
    /// </summary>
    [Fact]
    public async Task OnDisappearing_UnsubscribesFromConnectionChanges()
    {
        await AppearAsync();
        await AppearAsync();
        ViewModel.OnDisappearing();

        Connection.Change(false);

        Assert.False(ViewModel.IsOffline);
    }

    /// <summary>
    /// Prüft, dass sich das ViewModel nach erneutem Erscheinen wieder anmeldet.
    /// </summary>
    [Fact]
    public async Task Reappearing_SubscribesAgain()
    {
        await AppearAsync();
        ViewModel.OnDisappearing();
        await AppearAsync();

        Connection.Change(false);

        Assert.True(ViewModel.IsOffline);
    }

    /// <summary>
    /// Prüft, dass die Anzeige nach einem Verbindungsausfall während der Abwesenheit beim Erscheinen aktualisiert wird.
    /// </summary>
    [Fact]
    public async Task AppearingWhileOffline_UpdatesBanner()
    {
        await AppearAsync();
        ViewModel.OnDisappearing();
        Connection.IsOnline = false;

        await AppearAsync();

        Assert.True(ViewModel.IsOffline);
    }
}

using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Verhalten der Detailansicht ohne Verbindung (zuletzt bekannte Daten mit Alter, Offline-Hinweis) und die automatische Aktualisierung nach Wiederverbindung.
/// </summary>
public class StationDetailViewModelTests_Offline : StationDetailViewModelTestBase
{
    /// <summary>
    /// Prüft, dass ohne Verbindung die zuletzt bekannten Daten mit Altersangabe und Offline-Hinweis erscheinen.
    /// </summary>
    [Fact]
    public async Task Offline_ShowsLastKnownDataWithAgeAndBanner()
    {
        Connection.IsOnline = false;
        Prices.DetailResult = new StationDetailResult(CreateStation(Now.AddMinutes(-90), Now.AddMinutes(-90)), PriceDataSource.OfflineFallback, PriceFailure.Offline);
        ViewModel.Show(CreateOrigin());

        await AppearAsync();

        Assert.True(ViewModel.IsOffline);
        Assert.Equal(SearchTexts.OfflineBanner, ViewModel.OfflineMessage);
        Assert.Equal(DetailTexts.LastKnownNote, ViewModel.SourceNote);
        Assert.False(ViewModel.HasStatusMessage);
        var line = ViewModel.Detail!.PriceLines[0];
        Assert.Equal("vor 90 Min.", line.AgeText);
        Assert.True(line.IsStale);
        Assert.True(ViewModel.Detail.HasUnconfirmedPrice);
        Assert.Equal("Stand: vor 1 Std.", ViewModel.Detail.OpeningHoursAgeText);
    }

    /// <summary>
    /// Prüft, dass ohne Verbindung und ohne lokale Daten die Anfangsanzeige aus der Liste bleibt und der Offline-Hinweis erscheint.
    /// </summary>
    [Fact]
    public async Task Offline_UnknownStation_KeepsListDataAndBanner()
    {
        Connection.IsOnline = false;
        Prices.DetailResult = new StationDetailResult(null, PriceDataSource.OfflineFallback, PriceFailure.Offline);
        ViewModel.Show(CreateOrigin());

        await AppearAsync();

        Assert.True(ViewModel.IsOffline);
        Assert.Equal("1,850 €", ViewModel.Detail!.PriceLines[0].PriceText);
        Assert.Equal(DetailTexts.LastKnownNote, ViewModel.SourceNote);
    }

    /// <summary>
    /// Prüft, dass nach Wiederverbindung veraltete Daten automatisch neu abgefragt werden und der Offline-Hinweis verschwindet.
    /// </summary>
    [Fact]
    public async Task ConnectionRestored_RefreshesStaleDataAndClearsBanner()
    {
        Connection.IsOnline = false;
        Prices.DetailResult = new StationDetailResult(CreateStation(Now.AddMinutes(-90), Now.AddMinutes(-90)), PriceDataSource.OfflineFallback, PriceFailure.Offline);
        ViewModel.Show(CreateOrigin());
        await AppearAsync();
        Prices.DetailResult = new StationDetailResult(CreateStation(Now, Now), PriceDataSource.Live, PriceFailure.None);

        Connection.Change(true);
        await ViewModel.LastLoadTask;

        Assert.Equal(2, Prices.DetailRequests.Count);
        Assert.False(ViewModel.IsOffline);
        Assert.Null(ViewModel.OfflineMessage);
        Assert.False(ViewModel.HasSourceNote);
        var line = ViewModel.Detail!.PriceLines[0];
        Assert.Equal("vor 0 Min.", line.AgeText);
        Assert.False(line.IsStale);
        Assert.False(ViewModel.Detail.HasUnconfirmedPrice);
    }

    /// <summary>
    /// Prüft, dass eine Wiederverbindung ohne veraltete Daten keinen erneuten Abruf auslöst.
    /// </summary>
    [Fact]
    public async Task ConnectionRestored_FreshData_DoesNotQueryAgain()
    {
        Prices.DetailResult = new StationDetailResult(CreateStation(Now, Now), PriceDataSource.Live, PriceFailure.None);
        ViewModel.Show(CreateOrigin());
        await AppearAsync();

        Connection.Change(false);
        Connection.Change(true);
        await ViewModel.LastLoadTask;

        Assert.Single(Prices.DetailRequests);
    }

    /// <summary>
    /// Prüft, dass eine Wiederverbindung bei veralteten Preisen aus einem Online-Abruf (60 Minuten und älter) neu abfragt.
    /// </summary>
    [Fact]
    public async Task ConnectionRestored_StalePricesWhileOnline_QueriesAgain()
    {
        Prices.DetailResult = new StationDetailResult(CreateStation(Now.AddMinutes(-70), Now), PriceDataSource.Cache, PriceFailure.None);
        ViewModel.Show(CreateOrigin());
        await AppearAsync();
        Assert.True(ViewModel.Detail!.PriceLines[0].IsStale);

        Connection.Change(true);
        await ViewModel.LastLoadTask;

        Assert.Equal(2, Prices.DetailRequests.Count);
    }

    /// <summary>
    /// Prüft, dass nach dem Verlassen der Seite eine Wiederverbindung nichts mehr auslöst.
    /// </summary>
    [Fact]
    public async Task ConnectionRestored_AfterDisappearing_IsIgnored()
    {
        Connection.IsOnline = false;
        Prices.DetailResult = new StationDetailResult(CreateStation(Now.AddMinutes(-90), Now.AddMinutes(-90)), PriceDataSource.OfflineFallback, PriceFailure.Offline);
        ViewModel.Show(CreateOrigin());
        await AppearAsync();
        ViewModel.OnDisappearing();

        Connection.Change(true);

        Assert.Single(Prices.DetailRequests);
    }

    /// <summary>
    /// Prüft, dass ein Verbindungsverlust bei geöffneter Ansicht den Offline-Hinweis sofort zeigt.
    /// </summary>
    [Fact]
    public async Task ConnectionLost_ShowsBannerImmediately()
    {
        Prices.DetailResult = new StationDetailResult(CreateStation(Now, Now), PriceDataSource.Live, PriceFailure.None);
        ViewModel.Show(CreateOrigin());
        await AppearAsync();
        Assert.False(ViewModel.IsOffline);

        Connection.Change(false);

        Assert.True(ViewModel.IsOffline);
    }
}

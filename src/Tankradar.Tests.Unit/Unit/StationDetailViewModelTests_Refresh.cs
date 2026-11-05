using Tankradar.MAUI.Models.Pricing;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, wann „Preise aktualisieren“ bedienbar ist: nicht ohne Verbindung (der Offline-Hinweis erklärt den Grund) und nicht während eines Abrufs.
/// </summary>
public class StationDetailViewModelTests_Refresh : StationDetailViewModelTestBase
{
    /// <summary>
    /// Prüft, dass die Schaltfläche mit Verbindung bedienbar ist.
    /// </summary>
    [Fact]
    public async Task CanRefresh_Online_IsTrue()
    {
        Prices.DetailResult = new StationDetailResult(CreateStation(Now, Now), PriceDataSource.Live, PriceFailure.None);
        ViewModel.Show(CreateOrigin());

        await AppearAsync();

        Assert.True(ViewModel.CanRefresh);
    }

    /// <summary>
    /// Prüft, dass die Schaltfläche ohne Verbindung deaktiviert ist und nach Wiederverbindung wieder bedienbar wird.
    /// </summary>
    [Fact]
    public async Task CanRefresh_Offline_IsFalseUntilConnectionReturns()
    {
        Connection.IsOnline = false;
        Prices.DetailResult = new StationDetailResult(CreateStation(Now.AddMinutes(-90), Now.AddMinutes(-90)), PriceDataSource.OfflineFallback, PriceFailure.Offline);
        ViewModel.Show(CreateOrigin());
        await AppearAsync();
        Assert.False(ViewModel.CanRefresh);
        var changes = new List<string?>();
        ViewModel.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
        Prices.DetailResult = new StationDetailResult(CreateStation(Now, Now), PriceDataSource.Live, PriceFailure.None);

        Connection.Change(true);
        await ViewModel.LastLoadTask;

        Assert.Contains("CanRefresh", changes);
        Assert.True(ViewModel.CanRefresh);
    }

    /// <summary>
    /// Prüft, dass ein Verbindungsverlust die Schaltfläche sofort deaktiviert.
    /// </summary>
    [Fact]
    public async Task CanRefresh_ConnectionLost_BecomesFalse()
    {
        Prices.DetailResult = new StationDetailResult(CreateStation(Now, Now), PriceDataSource.Live, PriceFailure.None);
        ViewModel.Show(CreateOrigin());
        await AppearAsync();

        Connection.Change(false);

        Assert.False(ViewModel.CanRefresh);
    }

    /// <summary>
    /// Prüft, dass ein Abruf mit Verbindung trotz zuletzt bekannter Daten (Fehler) erneut angestoßen werden kann.
    /// </summary>
    [Fact]
    public async Task CanRefresh_OnlineWithOfflineFallbackData_StaysTrueForRetry()
    {
        Prices.DetailResult = new StationDetailResult(CreateStation(Now.AddMinutes(-90), Now.AddMinutes(-90)), PriceDataSource.OfflineFallback, PriceFailure.Offline);
        ViewModel.Show(CreateOrigin());

        await AppearAsync();

        Assert.True(ViewModel.IsOffline);
        Assert.True(ViewModel.CanRefresh);
    }
}

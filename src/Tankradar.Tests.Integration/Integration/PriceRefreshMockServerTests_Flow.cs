using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.Tests.Integration.Integration.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft den Preisabruf nach Kennungen mit echtem Client und Repository gegen den lokalen Mock-Server (nie ein produktiver Endpunkt).
/// </summary>
public class PriceRefreshMockServerTests_Flow : SearchMockServerTestBase
{
    private static readonly string[] Favorites = [MockTankerkoenigServer.StationAlpha, MockTankerkoenigServer.StationBeta];

    private static decimal Price(StationInfo station, FuelType fuel)
    {
        return station.Prices.Single(p => p.FuelType == fuel).Price;
    }

    /// <summary>
    /// Prüft, dass neue Preise des Mocks übernommen werden und das Alter der Preise wieder bei null beginnt.
    /// </summary>
    [Fact]
    public async Task Refresh_AgainstMock_UpdatesPricesAndAges()
    {
        var service = CreateService();
        await service.SearchNearbyAsync(Query(5));
        Clock.Advance(TimeSpan.FromMinutes(20));
        Server.SetPrices(MockTankerkoenigServer.StationAlpha, 1.909m, 1.849m, 1.749m);

        var result = await service.RefreshStationsAsync(Favorites, force: true);

        Assert.Equal(PriceDataSource.Live, result.Source);
        var alpha = result.Stations.Single(s => s.Id == MockTankerkoenigServer.StationAlpha);
        Assert.Equal(1.749m, Price(alpha, FuelType.Diesel));
        Assert.Equal("vor 0 Min.", PriceFreshness.FormatAge(alpha.Prices[0].RetrievedUtc, Clock.GetUtcNow().UtcDateTime));
        Assert.Equal(1, Server.PricesRequests);
        Assert.Equal(string.Join(',', Favorites), Server.LastPricesIds);
        var known = await service.GetKnownStationsAsync(Favorites);
        Assert.Equal(1.749m, Price(known.Single(s => s.Id == MockTankerkoenigServer.StationAlpha), FuelType.Diesel));
    }

    /// <summary>
    /// Prüft, dass mehr als zehn Favoriten mit mehreren Anfragen abgerufen werden.
    /// </summary>
    [Fact]
    public async Task Refresh_MoreThanTenStations_UsesSeveralRequests()
    {
        Server.AddGeneratedStations(12);
        var service = CreateService();
        var search = await service.SearchNearbyAsync(Query(5));
        var ids = search.Stations.Select(s => s.Id).Take(14).ToList();

        var result = await service.RefreshStationsAsync(ids, force: true);

        Assert.Equal(14, ids.Count);
        Assert.Equal(2, Server.PricesRequests);
        Assert.Equal(14, result.Stations.Count);
        Assert.Equal(PriceFailure.None, result.Failure);
    }

    /// <summary>
    /// Prüft, dass bei beendetem Server die zuletzt bekannten Preise mit ihrem Alter geliefert werden.
    /// </summary>
    [Fact]
    public async Task Refresh_ServerGone_ReturnsLastKnownPrices()
    {
        await CreateService().SearchNearbyAsync(Query(5));
        var closedUrl = StopServer();
        Clock.Advance(TimeSpan.FromMinutes(75));

        var result = await CreateService(baseUrl: closedUrl).RefreshStationsAsync(Favorites, force: true);

        Assert.Equal(PriceDataSource.OfflineFallback, result.Source);
        Assert.Equal(PriceFailure.Unreachable, result.Failure);
        var alpha = result.Stations.Single(s => s.Id == MockTankerkoenigServer.StationAlpha);
        Assert.Equal(1.859m, Price(alpha, FuelType.SuperE5));
        Assert.True(PriceFreshness.IsStale(alpha.Prices[0].RetrievedUtc, Clock.GetUtcNow().UtcDateTime));
    }

    /// <summary>
    /// Prüft, dass ein falscher Schlüssel als Fehler gemeldet wird, ohne eine Ausnahme auszulösen.
    /// </summary>
    [Fact]
    public async Task Refresh_WrongKey_ReportsFailureWithoutThrowing()
    {
        await CreateService().SearchNearbyAsync(Query(5));

        var result = await CreateService(key: "falsch").RefreshStationsAsync(Favorites, force: true);

        Assert.Equal(PriceFailure.Rejected, result.Failure);
        Assert.Equal(PriceDataSource.OfflineFallback, result.Source);
        Assert.Equal(2, result.Stations.Count);
    }

    /// <summary>
    /// Prüft, dass nach Wiederverbindung erneut abgerufen wird, nachdem offline kein Abruf stattfand.
    /// </summary>
    [Fact]
    public async Task Refresh_ConnectionRestored_FetchesAgain()
    {
        var service = CreateService();
        await service.SearchNearbyAsync(Query(5));
        Connection.Change(false);
        var offline = await service.RefreshStationsAsync(Favorites, force: false);
        var requestsOffline = Server.PricesRequests;

        Connection.Change(true);
        var online = await service.RefreshStationsAsync(Favorites, force: false);

        Assert.Equal(PriceFailure.Offline, offline.Failure);
        Assert.Equal(0, requestsOffline);
        Assert.Equal(PriceDataSource.Live, online.Source);
        Assert.Equal(1, Server.PricesRequests);
    }
}

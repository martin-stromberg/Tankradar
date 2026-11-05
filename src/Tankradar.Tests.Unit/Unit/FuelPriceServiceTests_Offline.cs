using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Offline-Rückfall: Ohne Verbindung oder bei Abruffehlern liefert der Dienst die zuletzt bekannten Preise samt Alter statt eines Fehlers.
/// </summary>
public class FuelPriceServiceTests_Offline : FuelPriceServiceTestBase
{
    private StationSearchQuery Query => new(StationFactory.CenterLatitude, StationFactory.CenterLongitude, 5, AllFuels);

    private async Task SeedKnownPricesAsync()
    {
        Client.SearchResult.Add(StationFactory.Create(1, Clock.UtcNow, (FuelType.SuperE5, 1.85m), (FuelType.Diesel, 1.65m)));
        await Service.SearchNearbyAsync(Query);
        Client.SearchResult.Clear();
    }

    /// <summary>
    /// Prüft, dass ohne Verbindung die zuletzt bekannten Preise mit ihrem Alter geliefert werden und kein Abruf stattfindet.
    /// </summary>
    [Fact]
    public async Task SearchNearbyAsync_Offline_ReturnsLastKnownPricesWithAge()
    {
        await SeedKnownPricesAsync();
        Clock.Advance(TimeSpan.FromMinutes(75));
        Connection.IsOnline = false;

        var result = await Service.SearchNearbyAsync(Query);

        Assert.Equal(PriceDataSource.OfflineFallback, result.Source);
        Assert.Equal(PriceFailure.Offline, result.Failure);
        Assert.Equal(1, Client.SearchCalls);
        var station = Assert.Single(result.Stations);
        Assert.Equal(2, station.Prices.Count);
        Assert.All(station.Prices, p => Assert.Equal("vor 75 Min.", PriceFreshness.FormatAge(p.RetrievedUtc, Clock.UtcNow)));
        Assert.True(StationHints.HasUnconfirmedPrice(station.Prices, Clock.UtcNow));
        Assert.NotNull(station.DistanceKm);
    }

    /// <summary>
    /// Prüft, dass ein nicht erreichbarer Dienst zum Rückfall führt und die Ausnahme nicht nach außen dringt.
    /// </summary>
    [Fact]
    public async Task SearchNearbyAsync_ServiceUnreachable_FallsBackWithoutThrowing()
    {
        await SeedKnownPricesAsync();
        Clock.Advance(TimeSpan.FromMinutes(10));
        Client.Failure = new PriceApiException(PriceFailure.Unreachable, "nicht erreichbar");

        var result = await Service.SearchNearbyAsync(Query);

        Assert.Equal(PriceDataSource.OfflineFallback, result.Source);
        Assert.Equal(PriceFailure.Unreachable, result.Failure);
        Assert.Single(result.Stations);
    }

    /// <summary>
    /// Prüft, dass jeder Fehlergrund den Rückfall auslöst.
    /// </summary>
    /// <param name="failure">Der Fehlergrund.</param>
    [Theory]
    [InlineData(PriceFailure.ApiKeyMissing)]
    [InlineData(PriceFailure.Rejected)]
    [InlineData(PriceFailure.InvalidResponse)]
    public async Task SearchNearbyAsync_AnyApiFailure_FallsBack(PriceFailure failure)
    {
        await SeedKnownPricesAsync();
        Clock.Advance(TimeSpan.FromMinutes(10));
        Client.Failure = new PriceApiException(failure, "Fehler");

        var result = await Service.SearchNearbyAsync(Query);

        Assert.Equal(failure, result.Failure);
        Assert.Single(result.Stations);
    }

    /// <summary>
    /// Prüft, dass ohne bekannte Daten ein leeres Ergebnis statt eines Fehlers entsteht.
    /// </summary>
    [Fact]
    public async Task SearchNearbyAsync_OfflineWithoutKnownData_ReturnsEmptyResult()
    {
        Connection.IsOnline = false;

        var result = await Service.SearchNearbyAsync(Query);

        Assert.Empty(result.Stations);
        Assert.Equal(PriceDataSource.OfflineFallback, result.Source);
    }

    /// <summary>
    /// Prüft, dass nach Wiederherstellung der Verbindung wieder live abgerufen wird.
    /// </summary>
    [Fact]
    public async Task SearchNearbyAsync_AfterConnectionRestored_FetchesLiveAgain()
    {
        await SeedKnownPricesAsync();
        Clock.Advance(TimeSpan.FromMinutes(10));
        Connection.Change(false);
        Assert.Equal(PriceDataSource.OfflineFallback, (await Service.SearchNearbyAsync(Query)).Source);

        Connection.Change(true);
        Client.SearchResult.Add(StationFactory.Create(1, Clock.UtcNow, (FuelType.SuperE5, 1.95m)));
        var result = await Service.SearchNearbyAsync(Query);

        Assert.Equal(PriceDataSource.Live, result.Source);
        Assert.Equal(1.95m, result.Stations.Single().Prices.Single().Price);
    }

    /// <summary>
    /// Prüft die Verfügbarkeitsprüfung für alle Ergebnisse.
    /// </summary>
    [Fact]
    public async Task CheckAvailabilityAsync_MapsOutcomes()
    {
        Assert.Equal(PriceFailure.None, await Service.CheckAvailabilityAsync());

        Client.Failure = new PriceApiException(PriceFailure.Rejected, "abgelehnt");
        Assert.Equal(PriceFailure.Rejected, await Service.CheckAvailabilityAsync());

        Connection.IsOnline = false;
        Assert.Equal(PriceFailure.Offline, await Service.CheckAvailabilityAsync());
    }
}

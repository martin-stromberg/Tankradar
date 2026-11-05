using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass <see cref="IPriceCacheInvalidator.InvalidateCaches"/> die Zwischenspeicher des Preisdienstes im Arbeitsspeicher leert.
/// </summary>
public class FuelPriceServiceTests_InvalidateCaches : FuelPriceServiceTestBase
{
    private StationSearchQuery Query()
    {
        return new StationSearchQuery(StationFactory.CenterLatitude, StationFactory.CenterLongitude, 5, AllFuels);
    }

    /// <summary>
    /// Prüft, dass der Dienst die Schnittstelle zum Verwerfen der Zwischenspeicher umsetzt.
    /// </summary>
    [Fact]
    public void Service_ImplementsInvalidator()
    {
        Assert.IsAssignableFrom<IPriceCacheInvalidator>(Service);
    }

    /// <summary>
    /// Prüft, dass nach dem Verwerfen eine Suche innerhalb der Cache-Lebensdauer erneut die Schnittstelle abruft.
    /// </summary>
    [Fact]
    public async Task InvalidateCaches_SearchCache_CausesNewApiCall()
    {
        Client.SearchResult.Add(StationFactory.Create(1, Clock.UtcNow, (FuelType.SuperE5, 1.85m)));
        await Service.SearchNearbyAsync(Query());
        await Service.SearchNearbyAsync(Query());
        Assert.Equal(1, Client.SearchCalls);

        ((IPriceCacheInvalidator)Service).InvalidateCaches();
        await Service.SearchNearbyAsync(Query());

        Assert.Equal(2, Client.SearchCalls);
    }

    /// <summary>
    /// Prüft, dass nach dem Verwerfen auch bereits aktualisierte Tankstellen ohne <c>force</c> erneut abgerufen werden.
    /// </summary>
    [Fact]
    public async Task InvalidateCaches_RefreshedStations_AreFetchedAgain()
    {
        var station = StationFactory.Create(1, Clock.UtcNow.AddHours(-3), (FuelType.SuperE5, 1.50m));
        await Repository.SaveAsync([station]);
        await Service.RefreshStationsAsync([station.Id], force: false);
        await Service.RefreshStationsAsync([station.Id], force: false);
        Assert.Single(Client.PricesRequests);

        ((IPriceCacheInvalidator)Service).InvalidateCaches();
        await Service.RefreshStationsAsync([station.Id], force: false);

        Assert.Equal(2, Client.PricesRequests.Count);
    }
}

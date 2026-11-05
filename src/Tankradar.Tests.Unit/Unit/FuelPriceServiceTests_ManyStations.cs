using System.Collections.Concurrent;
using System.Reflection;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass der Preisdienst bei sehr vielen Favoriten keine Tankstelle auslässt, und dass abgelaufene Suchcache-Einträge entfernt werden.
/// </summary>
public class FuelPriceServiceTests_ManyStations : FuelPriceServiceTestBase
{
    private const int Count = 135;

    private static string Id(int number)
    {
        return StationFactory.Create(number, DateTime.UtcNow).Id;
    }

    private async Task SeedAsync()
    {
        var stations = Enumerable.Range(1, Count)
            .Select(n => StationFactory.Create(n, Clock.UtcNow.AddHours(-3), (FuelType.SuperE5, 1.50m)))
            .ToList();
        await Repository.SaveAsync(stations);
    }

    private int SearchCacheCount()
    {
        var field = typeof(Tankradar.MAUI.Services.Pricing.FuelPriceService).GetField("_searchCache", BindingFlags.Instance | BindingFlags.NonPublic);
        var cache = Assert.IsType<ConcurrentDictionary<string, DateTime>>(field!.GetValue(Service));
        return cache.Count;
    }

    /// <summary>
    /// Prüft, dass das lokale Lesen mehr als 100 Kennungen ohne Kürzung liefert.
    /// </summary>
    [Fact]
    public async Task GetKnownStationsAsync_MoreThanHundredIds_ReturnsAll()
    {
        await SeedAsync();

        var stations = await Service.GetKnownStationsAsync(Enumerable.Range(1, Count).Select(Id).ToList());

        Assert.Equal(Count, stations.Count);
        Assert.Empty(Client.PricesRequests);
    }

    /// <summary>
    /// Prüft, dass der Abruf mehr als 100 Kennungen in Paketen zu 10 abruft und alle Tankstellen liefert.
    /// </summary>
    [Fact]
    public async Task RefreshStationsAsync_MoreThanHundredIds_FetchesAllInChunksOfTen()
    {
        await SeedAsync();
        Client.PricesResult[Id(Count)] = [new FuelPrice(FuelType.SuperE5, 1.99m, Clock.UtcNow)];

        var result = await Service.RefreshStationsAsync(Enumerable.Range(1, Count).Select(Id).ToList(), force: false);

        Assert.Equal(14, Client.PricesRequests.Count);
        Assert.All(Client.PricesRequests.Take(13), request => Assert.Equal(10, request.Count));
        Assert.Equal(5, Client.PricesRequests[13].Count);
        Assert.Equal(Count, Client.PricesRequests.Sum(request => request.Count));
        Assert.Equal(Count, result.Stations.Count);
        var last = result.Stations.Single(s => s.Id == Id(Count));
        Assert.Equal(1.99m, last.Prices.Single(p => p.FuelType == FuelType.SuperE5).Price);
    }

    /// <summary>
    /// Prüft, dass abgelaufene Einträge des Suchcaches (mit gerundeten Koordinaten) bei der nächsten Anfrage entfernt werden.
    /// </summary>
    [Fact]
    public async Task SearchCache_ExpiredEntries_AreRemoved()
    {
        Client.SearchResult.Add(StationFactory.Create(1, Clock.UtcNow, (FuelType.SuperE5, 1.85m)));
        await Service.SearchNearbyAsync(new StationSearchQuery(52.100, 13.100, 5, AllFuels));
        await Service.SearchNearbyAsync(new StationSearchQuery(52.300, 13.300, 5, AllFuels));
        Assert.Equal(2, SearchCacheCount());

        Clock.Advance(TimeSpan.FromMinutes(6));
        await Service.GetKnownStationsAsync([Id(1)]);

        Assert.Equal(0, SearchCacheCount());
    }
}

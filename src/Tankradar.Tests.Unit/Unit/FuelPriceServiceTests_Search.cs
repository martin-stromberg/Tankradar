using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Umkreissuche des Preisdienstes: Abruf, Speicherung mit Zeitstempel, Filterung, Cache und Eingabevalidierung.
/// </summary>
public class FuelPriceServiceTests_Search : FuelPriceServiceTestBase
{
    private StationSearchQuery Query(IReadOnlyList<FuelType>? fuels = null, int radius = 5)
    {
        return new StationSearchQuery(StationFactory.CenterLatitude, StationFactory.CenterLongitude, radius, fuels ?? AllFuels);
    }

    private void SeedTwoStations()
    {
        Client.SearchResult.Add(StationFactory.Create(2, Clock.UtcNow, (FuelType.SuperE5, 1.90m), (FuelType.Diesel, 1.70m)));
        Client.SearchResult.Add(StationFactory.Create(1, Clock.UtcNow, (FuelType.SuperE5, 1.85m)));
    }

    /// <summary>
    /// Prüft, dass ein Abruf live liefert, nach Entfernung sortiert und die Preise mit Zeitstempel speichert.
    /// </summary>
    [Fact]
    public async Task SearchNearbyAsync_Online_ReturnsLiveDataAndPersistsPrices()
    {
        SeedTwoStations();

        var result = await Service.SearchNearbyAsync(Query());

        Assert.Equal(PriceDataSource.Live, result.Source);
        Assert.Equal(PriceFailure.None, result.Failure);
        Assert.Equal(["Station 1", "Station 2"], result.Stations.Select(s => s.Name));
        var stored = await Repository.GetStationAsync(StationFactory.Create(2, Clock.UtcNow).Id);
        Assert.NotNull(stored);
        Assert.Equal(Clock.UtcNow, stored.Prices.First(p => p.FuelType == FuelType.Diesel).RetrievedUtc);
    }

    /// <summary>
    /// Prüft, dass nur Stationen mit Preisen der gewählten Sorten erscheinen und nur deren Preise enthalten sind.
    /// </summary>
    [Fact]
    public async Task SearchNearbyAsync_FuelFilter_KeepsOnlyRequestedPrices()
    {
        SeedTwoStations();

        var result = await Service.SearchNearbyAsync(Query([FuelType.Diesel]));

        var station = Assert.Single(result.Stations);
        Assert.Equal("Station 2", station.Name);
        Assert.Equal(FuelType.Diesel, Assert.Single(station.Prices).FuelType);
    }

    /// <summary>
    /// Prüft, dass ein zweiter Abruf innerhalb der Cache-Dauer aus dem lokalen Cache bedient wird.
    /// </summary>
    [Fact]
    public async Task SearchNearbyAsync_WithinCacheLifetime_UsesCacheWithoutApiCall()
    {
        SeedTwoStations();
        await Service.SearchNearbyAsync(Query());
        Clock.Advance(TimeSpan.FromMinutes(4));

        var result = await Service.SearchNearbyAsync(Query());

        Assert.Equal(PriceDataSource.Cache, result.Source);
        Assert.Equal(1, Client.SearchCalls);
        Assert.Equal(2, result.Stations.Count);
    }

    /// <summary>
    /// Prüft, dass nach Ablauf der Cache-Dauer erneut abgerufen wird und die Historie wächst.
    /// </summary>
    [Fact]
    public async Task SearchNearbyAsync_AfterCacheLifetime_FetchesAgain()
    {
        SeedTwoStations();
        await Service.SearchNearbyAsync(Query());
        Clock.Advance(TimeSpan.FromMinutes(6));
        Client.SearchResult.Clear();
        Client.SearchResult.Add(StationFactory.Create(1, Clock.UtcNow, (FuelType.SuperE5, 1.99m)));

        var result = await Service.SearchNearbyAsync(Query());

        Assert.Equal(PriceDataSource.Live, result.Source);
        Assert.Equal(2, Client.SearchCalls);
        Assert.Equal(1.99m, result.Stations.Single().Prices.Single().Price);
    }

    /// <summary>
    /// Prüft, dass eine andere Suche (anderer Radius) den Cache nicht trifft.
    /// </summary>
    [Fact]
    public async Task SearchNearbyAsync_DifferentRadius_BypassesCache()
    {
        SeedTwoStations();
        await Service.SearchNearbyAsync(Query(radius: 5));

        await Service.SearchNearbyAsync(Query(radius: 10));

        Assert.Equal(2, Client.SearchCalls);
    }

    /// <summary>
    /// Prüft die Eingabevalidierung: ungültige Anfragen erreichen die API nie.
    /// </summary>
    /// <param name="latitude">Breitengrad.</param>
    /// <param name="longitude">Längengrad.</param>
    /// <param name="radius">Radius.</param>
    /// <param name="emptyFuels">Ob die Sortenliste leer ist.</param>
    [Theory]
    [InlineData(91.0, 13.0, 5, false)]
    [InlineData(52.0, 181.0, 5, false)]
    [InlineData(double.NaN, 13.0, 5, false)]
    [InlineData(52.0, 13.0, 0, false)]
    [InlineData(52.0, 13.0, 26, false)]
    [InlineData(52.0, 13.0, 5, true)]
    public async Task SearchNearbyAsync_InvalidInput_ThrowsBeforeApiCall(double latitude, double longitude, int radius, bool emptyFuels)
    {
        var query = new StationSearchQuery(latitude, longitude, radius, emptyFuels ? [] : AllFuels);

        await Assert.ThrowsAsync<ArgumentException>(() => Service.SearchNearbyAsync(query));

        Assert.Equal(0, Client.SearchCalls);
    }

    /// <summary>
    /// Prüft, dass die Grenzwerte des Radius (1 und 25 km) zulässig sind.
    /// </summary>
    /// <param name="radius">Der Radius.</param>
    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    public async Task SearchNearbyAsync_RadiusBounds_AreAccepted(int radius)
    {
        SeedTwoStations();

        var result = await Service.SearchNearbyAsync(Query(radius: radius));

        Assert.Equal(PriceDataSource.Live, result.Source);
    }
}

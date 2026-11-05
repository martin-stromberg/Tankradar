using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass der eigene Anzeigename beim Abruf frischer Daten erhalten bleibt (die Quelle kennt ihn nicht) und über den Dienst gepflegt werden kann.
/// </summary>
public class FuelPriceServiceTests_CustomName : FuelPriceServiceTestBase
{
    private static StationInfo Fresh(StationInfo basis)
    {
        return new StationInfo { Id = basis.Id, Name = "Klaus Heinrich", Brand = "Westfalen", Latitude = basis.Latitude, Longitude = basis.Longitude, DistanceKm = basis.DistanceKm, Prices = basis.Prices, DetailsUpdatedUtc = basis.DetailsUpdatedUtc };
    }

    /// <summary>
    /// Prüft, dass ein eigener Name nach einem Live-Detailabruf (der die Stammdaten überschreibt) weiter gilt und gespeichert bleibt.
    /// </summary>
    [Fact]
    public async Task GetStationDetailAsync_Live_KeepsCustomName()
    {
        var station = Fresh(StationFactory.Create(3, Clock.UtcNow, (FuelType.Diesel, 1.60m)));
        Client.DetailResult = station;
        await Service.GetStationDetailAsync(station.Id);
        Assert.Equal(StationNameResult.Ok, await Service.SetCustomNameAsync(station.Id, "Zuhause"));
        Clock.Advance(TimeSpan.FromMinutes(30));

        var result = await Service.GetStationDetailAsync(station.Id);

        Assert.Equal(PriceDataSource.Live, result.Source);
        Assert.Equal("Zuhause", result.Station!.CustomName);
        Assert.Equal("Zuhause", (await Repository.GetStationAsync(station.Id))!.CustomName);
        Assert.Equal("Westfalen", result.Station.Brand);
    }

    /// <summary>
    /// Prüft, dass ein eigener Name auch in einer Live-Umkreissuche erhalten bleibt.
    /// </summary>
    [Fact]
    public async Task SearchNearbyAsync_Live_KeepsCustomName()
    {
        var station = Fresh(StationFactory.Create(1, Clock.UtcNow, (FuelType.Diesel, 1.60m)));
        Client.SearchResult.Add(station);
        var query = new StationSearchQuery(StationFactory.CenterLatitude, StationFactory.CenterLongitude, 10, AllFuels);
        await Service.SearchNearbyAsync(query);
        await Service.SetCustomNameAsync(station.Id, "Zuhause");
        Clock.Advance(TimeSpan.FromHours(1));

        var result = await Service.SearchNearbyAsync(query);

        Assert.Equal(PriceDataSource.Live, result.Source);
        Assert.Equal("Zuhause", Assert.Single(result.Stations).CustomName);
    }

    /// <summary>
    /// Prüft Vergeben, Entfernen und die Ablehnungen (zu lang, unbekannt) über den Dienst.
    /// </summary>
    [Fact]
    public async Task SetCustomNameAsync_SetsRemovesAndRejects()
    {
        var station = Fresh(StationFactory.Create(2, Clock.UtcNow, (FuelType.Diesel, 1.60m)));
        Client.DetailResult = station;
        await Service.GetStationDetailAsync(station.Id);

        Assert.Equal(StationNameResult.Ok, await Service.SetCustomNameAsync(station.Id.ToUpperInvariant(), " Arbeit "));
        Assert.Equal("Arbeit", (await Repository.GetStationAsync(station.Id))!.CustomName);
        Assert.Equal(StationNameResult.TooLong, await Service.SetCustomNameAsync(station.Id, new string('x', StationNaming.MaxCustomNameLength + 1)));
        Assert.Equal("Arbeit", (await Repository.GetStationAsync(station.Id))!.CustomName);
        Assert.Equal(StationNameResult.Ok, await Service.SetCustomNameAsync(station.Id, "  "));
        Assert.Null((await Repository.GetStationAsync(station.Id))!.CustomName);
        Assert.Equal(StationNameResult.NotFound, await Service.SetCustomNameAsync("99999999-0000-4000-8000-000000000000", "Name"));
    }

    /// <summary>
    /// Prüft, dass ein Lesefehler der eigenen Namen die frisch abgerufenen Daten nicht verhindert.
    /// </summary>
    [Fact]
    public async Task GetStationDetailAsync_NamesUnreadable_StillReturnsFreshData()
    {
        var station = Fresh(StationFactory.Create(4, Clock.UtcNow, (FuelType.Diesel, 1.60m)));
        Client.DetailResult = station;
        var service = new MAUI.Services.Pricing.FuelPriceService(
            Client,
            new NamesFailingRepository(Repository),
            Connection,
            new MAUI.Services.Pricing.PriceApiOptions(),
            Clock,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MAUI.Services.Pricing.FuelPriceService>.Instance);

        var result = await service.GetStationDetailAsync(station.Id);

        Assert.Equal(PriceDataSource.Live, result.Source);
        Assert.Equal("Westfalen", result.Station!.DisplayName);
    }

    private sealed class NamesFailingRepository(MAUI.Services.Pricing.IPriceRepository inner) : MAUI.Services.Pricing.IPriceRepository
    {
        public Task SaveAsync(IReadOnlyCollection<StationInfo> stations, CancellationToken cancellationToken = default)
        {
            return inner.SaveAsync(stations, cancellationToken);
        }

        public Task<StationInfo?> GetStationAsync(string stationId, CancellationToken cancellationToken = default)
        {
            return inner.GetStationAsync(stationId, cancellationToken);
        }

        public Task<IReadOnlyDictionary<string, StationInfo>> GetKnownDetailsAsync(IReadOnlyCollection<string> stationIds, CancellationToken cancellationToken = default)
        {
            return inner.GetKnownDetailsAsync(stationIds, cancellationToken);
        }

        public Task<IReadOnlyList<StationInfo>> FindNearbyAsync(double latitude, double longitude, double radiusKm, CancellationToken cancellationToken = default)
        {
            return inner.FindNearbyAsync(latitude, longitude, radiusKm, cancellationToken);
        }

        public Task<IReadOnlyList<StationInfo>> GetStationsAsync(IReadOnlyCollection<string> stationIds, CancellationToken cancellationToken = default)
        {
            return inner.GetStationsAsync(stationIds, cancellationToken);
        }

        public Task SavePricesAsync(IReadOnlyDictionary<string, IReadOnlyList<FuelPrice>> prices, CancellationToken cancellationToken = default)
        {
            return inner.SavePricesAsync(prices, cancellationToken);
        }

        public Task<IReadOnlyDictionary<string, string>> GetCustomNamesAsync(IReadOnlyCollection<string> stationIds, CancellationToken cancellationToken = default)
        {
            return Task.FromException<IReadOnlyDictionary<string, string>>(new InvalidOperationException("Lesen fehlgeschlagen."));
        }

        public Task<StationNameResult> SetCustomNameAsync(string stationId, string? name, CancellationToken cancellationToken = default)
        {
            return inner.SetCustomNameAsync(stationId, name, cancellationToken);
        }
    }
}

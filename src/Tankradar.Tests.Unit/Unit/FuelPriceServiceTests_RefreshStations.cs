using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Preisaktualisierung nach Kennungen: Anfragegruppen, Frische, Offline-Rückfall, Teilerfolg und Speicherfehler.
/// </summary>
public class FuelPriceServiceTests_RefreshStations : FuelPriceServiceTestBase
{
    private static string Id(int number)
    {
        return StationFactory.Create(number, DateTime.UtcNow).Id;
    }

    private async Task SeedAsync(int count)
    {
        var stations = Enumerable.Range(1, count)
            .Select(n => StationFactory.Create(n, Clock.UtcNow.AddHours(-3), (FuelType.SuperE5, 1.50m), (FuelType.Diesel, 1.40m)))
            .ToList();
        await Repository.SaveAsync(stations);
    }

    private void SetFetched(int number, decimal e5)
    {
        Client.PricesResult[Id(number)] = [new FuelPrice(FuelType.SuperE5, e5, Clock.UtcNow)];
    }

    /// <summary>
    /// Prüft, dass 25 Kennungen in Anfragen zu 10, 10 und 5 Kennungen abgerufen werden.
    /// </summary>
    [Fact]
    public async Task Refresh_FetchesPricesInChunksOfTen()
    {
        await SeedAsync(25);

        var result = await Service.RefreshStationsAsync(Enumerable.Range(1, 25).Select(Id).ToList(), force: false);

        Assert.Equal([10, 10, 5], Client.PricesRequests.Select(r => r.Count));
        Assert.Equal(PriceDataSource.Live, result.Source);
        Assert.Equal(25, result.Stations.Count);
    }

    /// <summary>
    /// Prüft, dass erfolgreich aktualisierte Tankstellen innerhalb der Frist übersprungen werden, mit <c>force</c> aber nicht.
    /// </summary>
    [Fact]
    public async Task Refresh_SkipsFreshStationsUnlessForced()
    {
        await SeedAsync(2);
        var ids = new[] { Id(1), Id(2) };
        await Service.RefreshStationsAsync(ids, force: false);

        Clock.Advance(TimeSpan.FromMinutes(2));
        var cached = await Service.RefreshStationsAsync(ids, force: false);
        await Service.RefreshStationsAsync(ids, force: true);
        Clock.Advance(TimeSpan.FromMinutes(6));
        await Service.RefreshStationsAsync(ids, force: false);

        Assert.Equal(PriceDataSource.Cache, cached.Source);
        Assert.Equal(3, Client.PricesRequests.Count);
    }

    /// <summary>
    /// Prüft, dass ohne Verbindung kein Abruf erfolgt und der lokale Stand als Offline-Rückfall geliefert wird.
    /// </summary>
    [Fact]
    public async Task Refresh_Offline_ReturnsLocalWithOfflineFallback()
    {
        await SeedAsync(1);
        Connection.IsOnline = false;

        var result = await Service.RefreshStationsAsync([Id(1)], force: true);

        Assert.Equal(PriceDataSource.OfflineFallback, result.Source);
        Assert.Equal(PriceFailure.Offline, result.Failure);
        Assert.Empty(Client.PricesRequests);
        Assert.Equal(2, Assert.Single(result.Stations).Prices.Count);
    }

    /// <summary>
    /// Prüft, dass ein Abruffehler zum Rückfall auf den lokalen Stand mit dem Fehlergrund führt.
    /// </summary>
    [Fact]
    public async Task Refresh_ApiFailure_FallsBackWithFailure()
    {
        await SeedAsync(1);
        Client.Failure = new PriceApiException(PriceFailure.Unreachable, "nicht erreichbar");

        var result = await Service.RefreshStationsAsync([Id(1)], force: false);

        Assert.Equal(PriceDataSource.OfflineFallback, result.Source);
        Assert.Equal(PriceFailure.Unreachable, result.Failure);
        Assert.Equal(1.50m, Assert.Single(result.Stations).Prices.Single(p => p.FuelType == FuelType.SuperE5).Price);
    }

    /// <summary>
    /// Prüft, dass bei einem Fehler der zweiten Gruppe die Daten der ersten Gruppe erhalten bleiben und der Fehler gemeldet wird.
    /// </summary>
    [Fact]
    public async Task Refresh_PartialFailure_KeepsSuccessfulChunk()
    {
        await SeedAsync(12);
        SetFetched(1, 1.99m);
        Client.PricesFailureFromCall = 1;
        Client.PricesFailure = new PriceApiException(PriceFailure.Unreachable, "nicht erreichbar");

        var result = await Service.RefreshStationsAsync(Enumerable.Range(1, 12).Select(Id).ToList(), force: false);

        Assert.Equal(PriceDataSource.Live, result.Source);
        Assert.Equal(PriceFailure.Unreachable, result.Failure);
        Assert.Equal(1.99m, result.Stations[0].Prices.Single(p => p.FuelType == FuelType.SuperE5).Price);
        Assert.Equal(1.50m, result.Stations[11].Prices.Single(p => p.FuelType == FuelType.SuperE5).Price);
    }

    /// <summary>
    /// Prüft, dass ein fehlgeschlagener Abruf keine Frische setzt und der nächste Aufruf erneut abruft.
    /// </summary>
    [Fact]
    public async Task Refresh_FailureDoesNotMarkFresh()
    {
        await SeedAsync(1);
        Client.Failure = new PriceApiException(PriceFailure.Unreachable, "nicht erreichbar");
        await Service.RefreshStationsAsync([Id(1)], force: false);
        Client.Failure = null;

        var result = await Service.RefreshStationsAsync([Id(1)], force: false);

        Assert.Equal(2, Client.PricesRequests.Count);
        Assert.Equal(PriceDataSource.Live, result.Source);
    }

    /// <summary>
    /// Prüft, dass ungültige und doppelte Kennungen ausgelassen werden und eine leere Liste keinen Abruf auslöst.
    /// </summary>
    [Fact]
    public async Task Refresh_InvalidIds_AreIgnored()
    {
        await SeedAsync(1);

        var empty = await Service.RefreshStationsAsync([], force: true);
        var result = await Service.RefreshStationsAsync(["kaputt", Id(1), Id(1), "1&x=2"], force: true);

        Assert.Empty(empty.Stations);
        Assert.Equal([Id(1)], Assert.Single(Client.PricesRequests));
        Assert.Single(result.Stations);
    }

    /// <summary>
    /// Prüft, dass die frisch abgerufenen Preise trotz Speicherfehler geliefert werden und die Frische nicht gesetzt wird.
    /// </summary>
    [Fact]
    public async Task Refresh_StorageFailure_StillReturnsFreshPrices()
    {
        await SeedAsync(1);
        SetFetched(1, 1.77m);
        var service = new FuelPriceService(Client, new SavePricesFailingRepository(Repository), Connection, new PriceApiOptions(), Clock, NullLogger<FuelPriceService>.Instance);

        var first = await service.RefreshStationsAsync([Id(1)], force: false);
        await service.RefreshStationsAsync([Id(1)], force: false);

        Assert.Equal(PriceDataSource.Live, first.Source);
        Assert.Equal(1.77m, Assert.Single(first.Stations).Prices.Single(p => p.FuelType == FuelType.SuperE5).Price);
        Assert.Equal(2, Client.PricesRequests.Count);
    }

    /// <summary>
    /// Prüft, dass die lokale Abfrage nie den Client aufruft und unbekannte Kennungen fehlen.
    /// </summary>
    [Fact]
    public async Task GetKnownStations_NeverCallsClient()
    {
        await SeedAsync(2);

        var stations = await Service.GetKnownStationsAsync([Id(2), Id(1), Id(9)]);

        Assert.Equal([Id(2), Id(1)], stations.Select(s => s.Id));
        Assert.Empty(Client.PricesRequests);
        Assert.Equal(0, Client.SearchCalls);
    }

    private sealed class SavePricesFailingRepository(IPriceRepository inner) : IPriceRepository
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
            return Task.FromException(new InvalidOperationException("Speichern fehlgeschlagen."));
        }
    }
}

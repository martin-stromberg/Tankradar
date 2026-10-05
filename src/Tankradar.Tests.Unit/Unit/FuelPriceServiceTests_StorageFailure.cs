using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass ein Speicherfehler frisch abgerufene Daten nicht verloren gehen lässt.
/// </summary>
public class FuelPriceServiceTests_StorageFailure : BaseTest
{
    /// <summary>
    /// Prüft, dass die Live-Daten trotz fehlgeschlagener Speicherung geliefert werden und der nächste Abruf erneut live erfolgt.
    /// </summary>
    [Fact]
    public async Task SearchNearbyAsync_SaveFails_StillReturnsLiveData()
    {
        var clock = new ManualTimeProvider();
        var client = new StubTankerkoenigClient();
        client.SearchResult.Add(StationFactory.Create(1, clock.UtcNow, (FuelType.Diesel, 1.7m)));
        var service = new FuelPriceService(client, new FailingRepository(), new FakeConnectionMonitor(), new PriceApiOptions(), clock, NullLogger<FuelPriceService>.Instance);
        var query = new StationSearchQuery(StationFactory.CenterLatitude, StationFactory.CenterLongitude, 5, [FuelType.Diesel]);

        var first = await service.SearchNearbyAsync(query);
        await service.SearchNearbyAsync(query);

        Assert.Equal(PriceDataSource.Live, first.Source);
        Assert.Single(first.Stations);
        Assert.Equal(2, client.SearchCalls);
    }

    private sealed class FailingRepository : IPriceRepository
    {
        public int SaveCalls { get; private set; }

        public Task SaveAsync(IReadOnlyCollection<StationInfo> stations, CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return stations.Count > 0 ? Task.FromException(new InvalidOperationException("Speichern fehlgeschlagen.")) : Task.CompletedTask;
        }

        public Task<StationInfo?> GetStationAsync(string stationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<StationInfo?>(null);
        }

        public Task<IReadOnlyList<StationInfo>> FindNearbyAsync(double latitude, double longitude, double radiusKm, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<StationInfo>>([]);
        }
    }
}

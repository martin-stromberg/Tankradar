using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft das Lesen bekannter Detailangaben (Öffnungszeiten) aus dem Preis-Cache für die Umkreissuche, auch bei vielen Kennungen.
/// </summary>
public class PriceRepositoryTests_KnownDetails : IDisposable
{
    private static readonly DateTime T0 = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);
    private const string WithDetailsId = "11111111-1111-4111-8111-111111111111";
    private const string ListOnlyId = "22222222-2222-4222-8222-222222222222";

    private readonly TestDatabase _database = new();

    private PriceRepository CreateRepository()
    {
        var factory = _database.CreateFactory();
        return new PriceRepository(factory, _database.CreateInitializer(factory));
    }

    /// <summary>
    /// Prüft, dass nur Tankstellen mit Detailangaben geliefert werden und eine leere Liste ein leeres Ergebnis ergibt.
    /// </summary>
    [Fact]
    public async Task GetKnownDetailsAsync_ReturnsOnlyStationsWithDetails()
    {
        var repository = CreateRepository();
        await repository.SaveAsync(
        [
            new StationInfo { Id = WithDetailsId, Name = "A", Latitude = 52.5, Longitude = 13.4, WholeDay = true, DetailsUpdatedUtc = T0, Prices = [new FuelPrice(FuelType.Diesel, 1.6m, T0)] },
            new StationInfo { Id = ListOnlyId, Name = "B", Latitude = 52.5, Longitude = 13.4, Prices = [new FuelPrice(FuelType.Diesel, 1.7m, T0)] },
        ]);

        var known = await repository.GetKnownDetailsAsync([WithDetailsId, ListOnlyId]);

        Assert.Equal([WithDetailsId], known.Keys);
        Assert.True(known[WithDetailsId].WholeDay);
        Assert.Empty(await repository.GetKnownDetailsAsync([]));
    }

    /// <summary>
    /// Prüft, dass auch eine sehr lange Kennungsliste (mehr als 1000 Einträge) fehlerfrei abgefragt wird.
    /// </summary>
    [Fact]
    public async Task GetKnownDetailsAsync_HandlesVeryLongIdLists()
    {
        var repository = CreateRepository();
        await repository.SaveAsync([new StationInfo { Id = WithDetailsId, Name = "A", Latitude = 52.5, Longitude = 13.4, WholeDay = false, DetailsUpdatedUtc = T0 }]);
        var ids = Enumerable.Range(1, 1500).Select(i => new Guid(i, 0, 0, [0, 0, 0, 0, 0, 0, 0, 1]).ToString("D")).Append(WithDetailsId).ToList();

        var known = await repository.GetKnownDetailsAsync(ids);

        Assert.Equal([WithDetailsId], known.Keys);
    }

    /// <summary>
    /// Prüft, dass <see cref="StationInfo.WithDetails"/> nur die Detailangaben übernimmt und die Live-Preise behält.
    /// </summary>
    [Fact]
    public void WithDetails_TakesDetailsAndKeepsLivePrices()
    {
        var live = new StationInfo { Id = WithDetailsId, Name = "A", Latitude = 1, Longitude = 2, DistanceKm = 1.5, Prices = [new FuelPrice(FuelType.Diesel, 1.5m, T0)] };
        var details = new StationInfo { Id = WithDetailsId, Name = "A", Latitude = 1, Longitude = 2, WholeDay = true, OpeningTimes = [new OpeningTimeEntry("Mo-So", "00:00:00", "24:00:00")], DetailsUpdatedUtc = T0 };

        var merged = live.WithDetails(details);

        Assert.True(merged.WholeDay);
        Assert.Single(merged.OpeningTimes);
        Assert.Equal(1.5m, merged.Prices.Single().Price);
        Assert.Equal(1.5, merged.DistanceKm);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _database.Dispose();
    }
}

using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft den lokalen Preis-Cache auf einer echten, migrierten SQLite-Datei: Speicherung mit Zeitstempel, erhaltene Historie, Umkreis und Details.
/// </summary>
public class PriceRepositoryTests_Persistence : IDisposable
{
    private const string StationId = "11111111-1111-4111-8111-111111111111";
    private static readonly DateTime T0 = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

    private readonly TestDatabase _database = new();

    private PriceRepository CreateRepository(out Microsoft.EntityFrameworkCore.IDbContextFactory<Tankradar.MAUI.Data.TankradarDbContext> factory)
    {
        factory = _database.CreateFactory();
        return new PriceRepository(factory, _database.CreateInitializer(factory));
    }

    private static StationInfo Station(string id, double lat, double lng, DateTime time, decimal e5, decimal? diesel = null)
    {
        var prices = new List<FuelPrice> { new(FuelType.SuperE5, e5, time) };
        if (diesel is { } d)
        {
            prices.Add(new FuelPrice(FuelType.Diesel, d, time));
        }

        return new StationInfo { Id = id, Name = "Station " + id[..2], Latitude = lat, Longitude = lng, Prices = prices };
    }

    /// <summary>
    /// Prüft, dass Preise mit ihrem Zeitstempel gespeichert werden und ältere Stände erhalten bleiben.
    /// </summary>
    [Fact]
    public async Task SaveAsync_KeepsHistoryAndReturnsLatestPrices()
    {
        var repository = CreateRepository(out var factory);

        await repository.SaveAsync([Station(StationId, 52.52, 13.40, T0, 1.80m, 1.60m)]);
        await repository.SaveAsync([Station(StationId, 52.52, 13.40, T0.AddMinutes(10), 1.85m)]);

        await using var context = factory.CreateDbContext();
        Assert.Equal(3, await context.PriceEntries.CountAsync());
        var latest = await repository.GetStationAsync(StationId);
        var e5 = latest!.Prices.Single(p => p.FuelType == FuelType.SuperE5);
        Assert.Equal(1.85m, e5.Price);
        Assert.Equal(T0.AddMinutes(10), e5.RetrievedUtc);
        Assert.Equal(DateTimeKind.Utc, e5.RetrievedUtc.Kind);
        var diesel = latest.Prices.Single(p => p.FuelType == FuelType.Diesel);
        Assert.Equal(T0, diesel.RetrievedUtc);
        var history = await context.PriceEntries.Where(p => p.FuelTypeKey == "SuperE5").OrderBy(p => p.RetrievedUtc).Select(p => p.Price).ToListAsync();
        Assert.Equal([1.80m, 1.85m], history);
    }

    /// <summary>
    /// Prüft, dass doppelte Kennungen im selben Stapel nicht zu einer Schlüsselverletzung führen.
    /// </summary>
    [Fact]
    public async Task SaveAsync_DuplicateIdsInBatch_AreStoredOnce()
    {
        var repository = CreateRepository(out var factory);

        await repository.SaveAsync([Station(StationId, 52.52, 13.40, T0, 1.8m), Station(StationId, 52.52, 13.40, T0, 1.8m)]);

        await using var context = factory.CreateDbContext();
        Assert.Equal(1, await context.Stations.CountAsync());
    }

    /// <summary>
    /// Prüft, dass ein späterer Listenabruf die zuvor gespeicherten Details (Öffnungszeiten) nicht überschreibt.
    /// </summary>
    [Fact]
    public async Task SaveAsync_ListUpdateKeepsStoredDetails()
    {
        var repository = CreateRepository(out _);
        var detailed = new StationInfo
        {
            Id = StationId,
            Name = "Alpha",
            Latitude = 52.52,
            Longitude = 13.40,
            WholeDay = true,
            OpeningTimes = [new OpeningTimeEntry("Mo-So", "00:00:00", "24:00:00")],
            DetailsUpdatedUtc = T0,
            Prices = [new FuelPrice(FuelType.Diesel, 1.6m, T0)],
        };
        await repository.SaveAsync([detailed]);

        await repository.SaveAsync([Station(StationId, 52.52, 13.40, T0.AddMinutes(5), 1.9m)]);

        var stored = await repository.GetStationAsync(StationId);
        Assert.True(stored!.WholeDay);
        Assert.Single(stored.OpeningTimes);
        Assert.Equal(T0, stored.DetailsUpdatedUtc);
    }

    /// <summary>
    /// Prüft die Umkreissuche aus dem Cache: Radius, Sortierung nach Entfernung und berechnete Entfernung.
    /// </summary>
    [Fact]
    public async Task FindNearbyAsync_ReturnsStationsInRadiusSortedByDistance()
    {
        var repository = CreateRepository(out _);
        await repository.SaveAsync(
        [
            Station("33333333-3333-4333-8333-333333333333", 52.60, 13.405, T0, 1.9m),
            Station("22222222-2222-4222-8222-222222222222", 52.53, 13.405, T0, 1.8m),
            Station("44444444-4444-4444-8444-444444444444", 48.14, 11.58, T0, 1.7m),
        ]);

        var nearby = await repository.FindNearbyAsync(52.52, 13.405, 15);

        Assert.Equal(["22222222-2222-4222-8222-222222222222", "33333333-3333-4333-8333-333333333333"], nearby.Select(s => s.Id));
        Assert.InRange(nearby[0].DistanceKm!.Value, 1.0, 1.2);
        Assert.InRange(nearby[1].DistanceKm!.Value, 8.8, 9.0);
        Assert.Empty(await repository.FindNearbyAsync(52.52, 13.405, 1));
    }

    /// <summary>
    /// Prüft, dass eine unbekannte Station <see langword="null"/> liefert und leeres Speichern nichts bewirkt.
    /// </summary>
    [Fact]
    public async Task GetStationAsync_Unknown_ReturnsNull()
    {
        var repository = CreateRepository(out _);

        await repository.SaveAsync([]);

        Assert.Null(await repository.GetStationAsync(StationId));
    }

    /// <summary>
    /// Prüft, dass in der Datenbank keine Nutzerposition abgelegt wird: Die Tabellen enthalten nur Stationsdaten.
    /// </summary>
    [Fact]
    public async Task Schema_ContainsNoUserPositionColumns()
    {
        var repository = CreateRepository(out var factory);
        await repository.SaveAsync([Station(StationId, 52.52, 13.40, T0, 1.8m)]);
        await repository.FindNearbyAsync(50.0, 10.0, 25);

        await using var context = factory.CreateDbContext();
        var columns = context.Model.GetEntityTypes()
            .Where(e => e.GetTableName() is "Stations" or "PriceEntries" or "UserSettings" or "FuelTypeSettings")
            .SelectMany(e => e.GetProperties().Select(p => e.GetTableName() + "." + p.Name))
            .ToList();
        Assert.DoesNotContain(columns, c => c.Contains("User", StringComparison.Ordinal) && c.Contains("Latitude", StringComparison.Ordinal));
        Assert.Equal(1, await context.Stations.CountAsync());
        Assert.Equal(52.52, (await context.Stations.SingleAsync()).Latitude);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _database.Dispose();
        GC.SuppressFinalize(this);
    }
}

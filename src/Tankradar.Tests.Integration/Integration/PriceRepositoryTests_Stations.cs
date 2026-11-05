using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft den Zugriff des Preis-Caches auf Tankstellen nach Kennungen (Favoriten) auf einer echten, migrierten SQLite-Datei.
/// </summary>
public class PriceRepositoryTests_Stations : IDisposable
{
    private const string StationA = "11111111-1111-4111-8111-111111111111";
    private const string StationB = "22222222-2222-4222-8222-222222222222";
    private const string Unknown = "99999999-9999-4999-8999-999999999999";
    private static readonly DateTime T0 = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

    private readonly TestDatabase _database = new();

    private PriceRepository CreateRepository(out IDbContextFactory<TankradarDbContext> factory)
    {
        factory = _database.CreateFactory();
        return new PriceRepository(factory, _database.CreateInitializer(factory));
    }

    private static StationInfo Station(string id, string name, double lat, decimal e5)
    {
        return new StationInfo
        {
            Id = id,
            Name = name,
            Street = "Weg",
            Latitude = lat,
            Longitude = 13.40,
            Prices = [new FuelPrice(FuelType.SuperE5, e5, T0)],
        };
    }

    /// <summary>
    /// Prüft, dass Stammdaten, Position und jüngste Preise in der Reihenfolge der Eingabe geliefert werden und Unbekanntes fehlt.
    /// </summary>
    [Fact]
    public async Task GetStationsAsync_ReturnsLatestPricesAndPositions()
    {
        var repository = CreateRepository(out _);
        await repository.SaveAsync([Station(StationA, "Alpha", 52.52, 1.80m), Station(StationB, "Beta", 52.53, 1.90m)]);
        await repository.SavePricesAsync(new Dictionary<string, IReadOnlyList<FuelPrice>> { [StationA] = [new FuelPrice(FuelType.SuperE5, 1.85m, T0.AddMinutes(5))] });

        var stations = await repository.GetStationsAsync([StationB, Unknown, StationA]);

        Assert.Equal(["Beta", "Alpha"], stations.Select(s => s.Name));
        Assert.Equal(52.53, stations[0].Latitude);
        Assert.Equal("Weg", stations[1].Street);
        Assert.Equal(1.85m, stations[1].Prices.Single().Price);
        Assert.Equal(T0.AddMinutes(5), stations[1].Prices.Single().RetrievedUtc);
        Assert.Null(stations[0].DistanceKm);
    }

    /// <summary>
    /// Prüft, dass Preiszeilen nur für lokal bekannte Tankstellen gespeichert und unbekannte Kennungen ignoriert werden.
    /// </summary>
    [Fact]
    public async Task SavePricesAsync_AppendsOnlyForKnownStations()
    {
        var repository = CreateRepository(out var factory);
        await repository.SaveAsync([Station(StationA, "Alpha", 52.52, 1.80m)]);

        await repository.SavePricesAsync(new Dictionary<string, IReadOnlyList<FuelPrice>>
        {
            [StationA] = [new FuelPrice(FuelType.Diesel, 1.60m, T0.AddMinutes(1))],
            [Unknown] = [new FuelPrice(FuelType.Diesel, 1.50m, T0.AddMinutes(1))],
        });

        await using var context = factory.CreateDbContext();
        Assert.Equal(2, await context.PriceEntries.CountAsync());
        Assert.Equal(1, await context.Stations.CountAsync());
        Assert.DoesNotContain(await context.PriceEntries.ToListAsync(), p => p.StationId == Unknown);
    }

    /// <summary>
    /// Prüft, dass das Speichern von Preisen die Stammdaten und Detailangaben unverändert lässt.
    /// </summary>
    [Fact]
    public async Task SavePricesAsync_KeepsMasterData()
    {
        var repository = CreateRepository(out var factory);
        var detailed = new StationInfo
        {
            Id = StationA,
            Name = "Alpha",
            Brand = "ALPHA",
            Latitude = 52.52,
            Longitude = 13.40,
            WholeDay = true,
            OpeningTimes = [new OpeningTimeEntry("Mo-So", "00:00:00", "24:00:00")],
            DetailsUpdatedUtc = T0,
            Prices = [new FuelPrice(FuelType.SuperE5, 1.80m, T0)],
        };
        await repository.SaveAsync([detailed]);

        await repository.SavePricesAsync(new Dictionary<string, IReadOnlyList<FuelPrice>> { [StationA] = [new FuelPrice(FuelType.SuperE5, 1.70m, T0.AddMinutes(9))] });

        await using var context = factory.CreateDbContext();
        var entity = await context.Stations.SingleAsync();
        Assert.Equal("Alpha", entity.Name);
        Assert.Equal("ALPHA", entity.Brand);
        Assert.Equal(T0, entity.DetailsUpdatedUtc);
        Assert.True(entity.WholeDay);
        var loaded = (await repository.GetStationsAsync([StationA])).Single();
        Assert.Equal("Mo-So", Assert.Single(loaded.OpeningTimes).Text);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _database.Dispose();
    }
}

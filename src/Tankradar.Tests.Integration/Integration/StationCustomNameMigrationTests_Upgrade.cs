using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft die Migration <c>AddStationCustomName</c> auf einer echten SQLite-Datei mit Bestandsdaten des Stands 0.1.19: Alle Daten bleiben erhalten,
/// bestehende Tankstellen haben keinen eigenen Namen, die Marke bleibt, danach lässt sich ein Name vergeben.
/// </summary>
public class StationCustomNameMigrationTests_Upgrade : IDisposable
{
    private const string StationId = "00000001-0000-4000-8000-000000000000";

    private readonly TestDatabase _database = new();

    private async Task MigrateToBeforeCustomNameAsync()
    {
        await using var oldContext = _database.CreateFactory().CreateDbContext();
        var before = oldContext.Database.GetMigrations().Single(m => m.EndsWith("_AddTankbookEntryDetails", StringComparison.Ordinal));
        await oldContext.GetService<IMigrator>().MigrateAsync(before);
        await oldContext.Database.ExecuteSqlRawAsync("INSERT INTO UserSettings (Id, GpsUsage, ResultView, ResultSortOrder, ShowNearby) VALUES (1, 'Never', 'Map', 'Name', 1)");
        await oldContext.Database.ExecuteSqlRawAsync(
            $"INSERT INTO Stations (Id, Name, Brand, Place, Latitude, Longitude) VALUES ('{StationId}', 'Klaus Heinrich', 'Westfalen', 'Berlin', 52.5, 13.4)");
        await oldContext.Database.ExecuteSqlRawAsync(
            $"INSERT INTO PriceEntries (StationId, FuelTypeKey, Price, RetrievedUtc) VALUES ('{StationId}', 'Diesel', '1.699', '2026-10-05 10:00:00')");
        await oldContext.Database.ExecuteSqlRawAsync("INSERT INTO FavoriteGroups (Name, CreatedUtc) VALUES ('Arbeitsweg', '2026-10-05 10:00:00')");
        await oldContext.Database.ExecuteSqlRawAsync(
            $"INSERT INTO FavoriteEntries (GroupId, StationId, Priority, AddedUtc) VALUES (1, '{StationId}', 'High', '2026-10-05 10:00:00')");
        Assert.DoesNotContain(await oldContext.Database.GetAppliedMigrationsAsync(), m => m.EndsWith("_AddStationCustomName", StringComparison.Ordinal));
    }

    /// <summary>
    /// Prüft das Update einer Datenbank mit Tankstelle, Preisen und Favoriten: Daten bleiben, der eigene Name ist leer, die Anzeige zeigt die Marke.
    /// </summary>
    [Fact]
    public async Task Initialize_FromSchemaBeforeCustomName_KeepsDataWithoutCustomName()
    {
        await MigrateToBeforeCustomNameAsync();
        var factory = _database.CreateFactory();

        await _database.CreateInitializer(factory).InitializeAsync();

        await using var context = factory.CreateDbContext();
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        var station = await context.Stations.SingleAsync();
        Assert.Equal(("Klaus Heinrich", "Westfalen", "Berlin"), (station.Name, station.Brand, station.Place));
        Assert.Null(station.CustomName);
        Assert.Equal(1.699m, (await context.PriceEntries.SingleAsync()).Price);
        Assert.Equal(StationId, (await context.FavoriteEntries.SingleAsync()).StationId);
        Assert.True((await context.UserSettings.SingleAsync()).ShowNearby);

        var repository = new PriceRepository(factory, _database.CreateInitializer(factory));
        var stored = await repository.GetStationAsync(StationId);
        Assert.Equal("Westfalen", stored!.DisplayName);
        Assert.Equal(StationNameResult.Ok, await repository.SetCustomNameAsync(StationId, "Zuhause"));
        Assert.Equal("Zuhause", (await repository.GetStationAsync(StationId))!.DisplayName);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _database.Dispose();
        GC.SuppressFinalize(this);
    }
}

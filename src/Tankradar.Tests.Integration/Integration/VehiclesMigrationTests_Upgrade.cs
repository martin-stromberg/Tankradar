using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Services;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft die Migration <c>AddVehicles</c> auf einer echten SQLite-Datei: Bestehende Daten bleiben erhalten, die neuen Tabellen sind leer,
/// das Löschen eines Fahrzeugs nimmt nur dessen Vorgänge mit (<c>ON DELETE CASCADE</c>) und Fahrzeugnamen sind ohne Beachtung der Groß- und Kleinschreibung eindeutig.
/// </summary>
public class VehiclesMigrationTests_Upgrade : IDisposable
{
    private const string StationId = "00000001-0000-4000-8000-000000000000";

    private readonly TestDatabase _database = new();

    private static async Task<List<string>> QueryAsync(TankradarDbContext context, string sql, int column)
    {
        var connection = context.Database.GetDbConnection();
        await context.Database.OpenConnectionAsync();
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        var values = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            values.Add(Convert.ToString(reader.GetValue(column), System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
        }

        return values;
    }

    private async Task MigrateToBeforeVehiclesAsync(IDbContextFactory<TankradarDbContext> factory)
    {
        await using var oldContext = factory.CreateDbContext();
        var before = oldContext.Database.GetMigrations().Single(m => m.EndsWith("_AddNearbySetting", StringComparison.Ordinal));
        await oldContext.GetService<IMigrator>().MigrateAsync(before);
        await oldContext.Database.ExecuteSqlRawAsync("INSERT INTO UserSettings (Id, GpsUsage, ResultView, ResultSortOrder, ShowNearby) VALUES (1, 'Never', 'Map', 'Name', 1)");
        await oldContext.Database.ExecuteSqlRawAsync("INSERT INTO FuelTypeSettings (FuelTypeKey, SortOrder, IsSelected) VALUES ('Diesel', 0, 1)");
        await oldContext.Database.ExecuteSqlRawAsync(
            $"INSERT INTO Stations (Id, Name, Latitude, Longitude) VALUES ('{StationId}', 'Alpha Tankstelle', 52.5, 13.4)");
        await oldContext.Database.ExecuteSqlRawAsync(
            $"INSERT INTO PriceEntries (StationId, FuelTypeKey, Price, RetrievedUtc) VALUES ('{StationId}', 'Diesel', '1.699', '2026-10-05 10:00:00')");
        await oldContext.Database.ExecuteSqlRawAsync("INSERT INTO FavoriteGroups (Name, CreatedUtc) VALUES ('Arbeitsweg', '2026-10-05 10:00:00')");
        await oldContext.Database.ExecuteSqlRawAsync(
            $"INSERT INTO FavoriteEntries (GroupId, StationId, Priority, AddedUtc) VALUES (1, '{StationId}', 'High', '2026-10-05 10:00:00')");
        Assert.DoesNotContain(await oldContext.Database.GetAppliedMigrationsAsync(), m => m.EndsWith("_AddVehicles", StringComparison.Ordinal));
    }

    private async Task<IDbContextFactory<TankradarDbContext>> UpgradeAsync()
    {
        await MigrateToBeforeVehiclesAsync(_database.CreateFactory());
        var factory = _database.CreateFactory();
        await _database.CreateInitializer(factory).InitializeAsync();
        return factory;
    }

    /// <summary>
    /// Prüft, dass Einstellungen, Spritsorten, Tankstelle, Preis und Favoriten nach dem Update erhalten bleiben und beide neuen Tabellen leer sind.
    /// </summary>
    [Fact]
    public async Task Initialize_FromSchemaBeforeVehicles_KeepsExistingDataAndAddsTables()
    {
        var factory = await UpgradeAsync();

        await using var context = factory.CreateDbContext();
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.True((await context.UserSettings.SingleAsync()).ShowNearby);
        Assert.Equal("Diesel", (await context.FuelTypeSettings.SingleAsync()).FuelTypeKey);
        Assert.Equal("Alpha Tankstelle", (await context.Stations.SingleAsync()).Name);
        Assert.Equal(1.699m, (await context.PriceEntries.SingleAsync()).Price);
        Assert.Equal("Arbeitsweg", (await context.FavoriteGroups.SingleAsync()).Name);
        Assert.Equal(StationId, (await context.FavoriteEntries.SingleAsync()).StationId);
        Assert.Empty(context.Vehicles);
        Assert.Empty(context.TankbookEntries);
        Assert.Contains("OccurredAt", await QueryAsync(context, "PRAGMA table_info(TankbookEntries)", 1));
        var settings = await new SettingsService(factory, _database.CreateInitializer(factory)).LoadAsync();
        Assert.True(settings.ShowNearby);
    }

    /// <summary>
    /// Prüft per SQL, dass das Löschen eines Fahrzeugs nur dessen Vorgänge entfernt und der Fremdschlüssel kaskadiert.
    /// </summary>
    [Fact]
    public async Task Database_CascadesVehicleDeleteToEntries()
    {
        var factory = await UpgradeAsync();
        await using var context = factory.CreateDbContext();
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO Vehicles (Id, Name, FuelKind, StartOdometerKm, CreatedUtc) VALUES (1, 'Golf', 'Diesel', 0, '2026-10-11 10:00:00'), (2, 'Polo', 'Petrol', 0, '2026-10-11 10:00:00')");
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO TankbookEntries (VehicleId, CreatedUtc) VALUES (1, '2026-10-11 10:00:00'), (1, '2026-10-11 10:00:00'), (2, '2026-10-11 10:00:00')");

        await context.Database.ExecuteSqlRawAsync("DELETE FROM Vehicles WHERE Id = 1");

        Assert.Equal(["2"], await QueryAsync(context, "SELECT VehicleId FROM TankbookEntries", 0));
        var onDelete = await QueryAsync(context, "PRAGMA foreign_key_list(TankbookEntries)", 6);
        Assert.Equal(["CASCADE"], onDelete);
    }

    /// <summary>
    /// Prüft den eindeutigen Index mit <c>NOCASE</c> auf dem Fahrzeugnamen.
    /// </summary>
    [Fact]
    public async Task Database_RejectsDuplicateVehicleNamesCaseInsensitive()
    {
        var factory = await UpgradeAsync();
        await using var context = factory.CreateDbContext();
        context.Vehicles.Add(new VehicleEntity { Name = "Golf", FuelKind = "Diesel", CreatedUtc = DateTime.UtcNow });
        await context.SaveChangesAsync();

        await using var second = factory.CreateDbContext();
        second.Vehicles.Add(new VehicleEntity { Name = "GOLF", FuelKind = "Petrol", CreatedUtc = DateTime.UtcNow });

        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _database.Dispose();
        GC.SuppressFinalize(this);
    }
}

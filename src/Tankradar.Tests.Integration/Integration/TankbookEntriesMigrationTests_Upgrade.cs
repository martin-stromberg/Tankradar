using System.Data.Common;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;
using Tankradar.Tests.Integration.Integration.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft die Migration <c>AddTankbookEntryDetails</c> auf einer echten SQLite-Datei mit Bestandsdaten des Stands 0.1.16: Alle Daten bleiben erhalten,
/// Altvorgänge erhalten den Anlagezeitpunkt als minutengenaue lokale Zeit, Kilometerstand und Art aus ihrem Fahrzeug, die übrigen Spalten ihre Standardwerte.
/// </summary>
public class TankbookEntriesMigrationTests_Upgrade : IDisposable
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
            values.Add(Convert.ToString(reader.GetValue(column), CultureInfo.InvariantCulture) ?? string.Empty);
        }

        return values;
    }

    private static DateTime ToLocalMinute(DateTime utc)
    {
        var local = DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
        return new DateTime(local.Year, local.Month, local.Day, local.Hour, local.Minute, 0, DateTimeKind.Unspecified);
    }

    private async Task MigrateToBeforeEntryDetailsAsync()
    {
        await using var oldContext = _database.CreateFactory().CreateDbContext();
        var before = oldContext.Database.GetMigrations().Single(m => m.EndsWith("_AddVehicles", StringComparison.Ordinal));
        await oldContext.GetService<IMigrator>().MigrateAsync(before);
        await oldContext.Database.ExecuteSqlRawAsync("INSERT INTO UserSettings (Id, GpsUsage, ResultView, ResultSortOrder, ShowNearby) VALUES (1, 'Never', 'Map', 'Name', 1)");
        await oldContext.Database.ExecuteSqlRawAsync("INSERT INTO FuelTypeSettings (FuelTypeKey, SortOrder, IsSelected) VALUES ('Diesel', 0, 1)");
        await oldContext.Database.ExecuteSqlRawAsync(
            $"INSERT INTO Stations (Id, Name, Place, Latitude, Longitude) VALUES ('{StationId}', 'Alpha Tankstelle', 'Berlin', 52.5, 13.4)");
        await oldContext.Database.ExecuteSqlRawAsync(
            $"INSERT INTO PriceEntries (StationId, FuelTypeKey, Price, RetrievedUtc) VALUES ('{StationId}', 'Diesel', '1.699', '2026-10-05 10:00:00')");
        await oldContext.Database.ExecuteSqlRawAsync("INSERT INTO FavoriteGroups (Name, CreatedUtc) VALUES ('Arbeitsweg', '2026-10-05 10:00:00')");
        await oldContext.Database.ExecuteSqlRawAsync(
            $"INSERT INTO FavoriteEntries (GroupId, StationId, Priority, AddedUtc) VALUES (1, '{StationId}', 'High', '2026-10-05 10:00:00')");
        await oldContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO Vehicles (Id, Name, FuelKind, StartOdometerKm, StartConsumption, CreatedUtc) VALUES "
            + "(1, 'Golf', 'Petrol', 84350, '5.4', '2026-10-11 09:00:00'), (2, 'Zoe', 'Electric', 12000, NULL, '2026-10-11 09:00:00')");
        await oldContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO TankbookEntries (VehicleId, CreatedUtc) VALUES (1, '2026-10-11 10:00:42.1234567'), (1, '2026-10-11 11:00:00'), (2, '2026-10-11 12:00:00')");
        Assert.DoesNotContain(await oldContext.Database.GetAppliedMigrationsAsync(), m => m.EndsWith("_AddTankbookEntryDetails", StringComparison.Ordinal));
    }

    /// <summary>
    /// Prüft das Update einer Datenbank mit Fahrzeugen (Benzin, Elektro) samt Altvorgängen, Favoriten, Preisen und Einstellungen.
    /// </summary>
    [Fact]
    public async Task Initialize_FromSchemaBeforeEntryDetails_KeepsDataAndFillsDefaults()
    {
        await MigrateToBeforeEntryDetailsAsync();
        var factory = _database.CreateFactory();

        await _database.CreateInitializer(factory).InitializeAsync();

        await using var context = factory.CreateDbContext();
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.True((await context.UserSettings.SingleAsync()).ShowNearby);
        Assert.Equal("Diesel", (await context.FuelTypeSettings.SingleAsync()).FuelTypeKey);
        Assert.Equal(1.699m, (await context.PriceEntries.SingleAsync()).Price);
        Assert.Equal(StationId, (await context.FavoriteEntries.SingleAsync()).StationId);
        Assert.Equal(5.4m, (await context.Vehicles.SingleAsync(v => v.Name == "Golf")).StartConsumption);

        var entries = await context.TankbookEntries.AsNoTracking().OrderBy(e => e.Id).ToListAsync();
        Assert.Equal(3, entries.Count);
        Assert.All(entries, entry => Assert.Equal(ToLocalMinute(entry.CreatedUtc), entry.OccurredAt));
        Assert.Equal([84350, 84350, 12000], entries.Select(entry => entry.OdometerKm));
        Assert.Equal(["Fuel", "Fuel", "Electricity"], entries.Select(entry => entry.Kind));
        Assert.All(entries, entry =>
        {
            Assert.Equal(0m, entry.Quantity);
            Assert.Equal(0m, entry.TotalAmount);
            Assert.False(entry.IsPartial);
            Assert.Equal(string.Empty, entry.StationName);
            Assert.Null(entry.StationId);
            Assert.Null(entry.ReceiptPhotoFileName);
        });

        var indexes = await QueryAsync(context, "PRAGMA index_list(TankbookEntries)", 1);
        Assert.Contains("IX_TankbookEntries_VehicleId_OccurredAt", indexes);
        Assert.DoesNotContain("IX_TankbookEntries_VehicleId", indexes);
        Assert.Equal(["CASCADE"], await QueryAsync(context, "PRAGMA foreign_key_list(TankbookEntries)", 6));

        var service = new TankbookEntryService(factory, _database.CreateInitializer(factory), _database.CreatePhotoStore(), TimeProvider.System, new RecordingLogger<TankbookEntryService>());
        var golfEntries = await service.GetEntriesAsync(1);
        Assert.Equal(2, golfEntries.Count);
        Assert.Equal(TankbookConsumptionState.NoDistance, golfEntries[0].ConsumptionState);
        var known = await service.GetKnownStationsAsync();
        Assert.Equal([new KnownStation(StationId, "Alpha Tankstelle", "Berlin")], known);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _database.Dispose();
        GC.SuppressFinalize(this);
    }
}

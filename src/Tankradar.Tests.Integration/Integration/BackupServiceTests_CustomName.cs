using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tankradar.MAUI.Models.Backup;
using Tankradar.Tests.Integration.Integration.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft, dass der eigene Anzeigename einer Tankstelle Teil der Sicherung ist (Wiederherstellung auf dem Stand der Sicherung) und dass eine Sicherung
/// aus der Zeit vor dem eigenen Anzeigenamen (ohne die Spalte) weiter eingespielt und migriert wird, ohne Daten zu verlieren.
/// </summary>
public class BackupServiceTests_CustomName : IDisposable
{
    private static readonly byte[] Photo = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

    private readonly BackupTestEnvironment _environment = new();

    /// <summary>
    /// Prüft, dass ein gesicherter eigener Name nach Änderung und Entfernen durch die Wiederherstellung zurückkehrt.
    /// </summary>
    [Fact]
    public async Task Restore_BringsBackCustomName()
    {
        await _environment.SeedAsync(Photo);
        await using (var context = _environment.CreateFactory().CreateDbContext())
        {
            (await context.Stations.SingleAsync()).CustomName = "Zuhause tanken";
            await context.SaveChangesAsync();
        }

        var service = _environment.CreateService();
        var created = await service.CreateAsync();
        await using (var context = _environment.CreateFactory().CreateDbContext())
        {
            (await context.Stations.SingleAsync()).CustomName = null;
            await context.SaveChangesAsync();
        }

        var result = await service.RestoreAsync(created.File!.FileName);

        Assert.Equal(BackupResult.Ok, result);
        await using var check = _environment.CreateFactory().CreateDbContext();
        var station = await check.Stations.SingleAsync();
        Assert.Equal("Zuhause tanken", station.CustomName);
        Assert.Equal(("Alpha Tankstelle", "Alpha"), (station.Name, station.Brand));
    }

    /// <summary>
    /// Prüft, dass eine Sicherung ohne die Spalte (Stand vor dem eigenen Anzeigenamen) eingespielt, migriert und ohne eigenen Namen wiederhergestellt wird.
    /// </summary>
    [Fact]
    public async Task Restore_BackupBeforeCustomName_IsMigratedWithoutCustomName()
    {
        byte[] oldDatabase;
        string migration;
        using (var old = new TestDatabase())
        {
            await using (var context = old.CreateFactory().CreateDbContext())
            {
                migration = context.Database.GetMigrations().Single(m => m.EndsWith("_AddTankbookEntryDetails", StringComparison.Ordinal));
                await context.GetService<IMigrator>().MigrateAsync(migration);
                await context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO Stations (Id, Name, Brand, Place, Latitude, Longitude) VALUES ('00000001-0000-4000-8000-000000000000', 'Klaus Heinrich', 'Westfalen', 'Berlin', 52.5, 13.4)");
                await context.Database.ExecuteSqlRawAsync("INSERT INTO Vehicles (Id, Name, FuelKind, StartOdometerKm, StartConsumption, CreatedUtc) VALUES (1, 'Altwagen', 'Petrol', 1000, NULL, '2026-10-11 09:00:00')");
            }

            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            oldDatabase = await File.ReadAllBytesAsync(old.DatabasePath);
        }

        Directory.CreateDirectory(_environment.BackupDirectory);
        var name = "tankatlas-backup-20261001-100000.zip";
        BackupTestArchive.Create(Path.Combine(_environment.BackupDirectory, name), oldDatabase, migration: migration, appVersion: "0.1.19");

        var result = await _environment.CreateService().RestoreAsync(name);

        Assert.Equal(BackupResult.Ok, result);
        await using var check = _environment.CreateFactory().CreateDbContext();
        Assert.Empty(await check.Database.GetPendingMigrationsAsync());
        var station = await check.Stations.SingleAsync();
        Assert.Equal(("Klaus Heinrich", "Westfalen", "Berlin"), (station.Name, station.Brand, station.Place));
        Assert.Null(station.CustomName);
        Assert.Equal(["Altwagen"], await _environment.GetVehicleNamesAsync());
    }

    /// <summary>
    /// Prüft, dass ein eigener Name genau an der Grenze von 60 Zeichen wiederhergestellt wird (die Ablehnung darüber prüfen die Schema-Tests).
    /// </summary>
    [Fact]
    public async Task Restore_CustomNameAtLimit_IsAccepted()
    {
        await _environment.SeedAsync(Photo);
        var longName = new string('x', MAUI.Models.Pricing.StationNaming.MaxCustomNameLength);
        await using (var context = _environment.CreateFactory().CreateDbContext())
        {
            (await context.Stations.SingleAsync()).CustomName = longName;
            await context.SaveChangesAsync();
        }

        var service = _environment.CreateService();
        var created = await service.CreateAsync();

        Assert.Equal(BackupResult.Ok, await service.RestoreAsync(created.File!.FileName));
        await using var check = _environment.CreateFactory().CreateDbContext();
        Assert.Equal(longName, (await check.Stations.SingleAsync()).CustomName);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _environment.Dispose();
        GC.SuppressFinalize(this);
    }
}

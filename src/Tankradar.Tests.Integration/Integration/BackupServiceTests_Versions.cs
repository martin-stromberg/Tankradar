using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models.Backup;
using Tankradar.Tests.Integration.Integration.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft Sicherungen anderer App-Versionen sowie ungültige Dateien: Ältere Stände werden eingespielt und migriert, zu neue und ungültige Dateien
/// werden mit verständlichem Ergebnis abgelehnt, ohne dass Datenbank oder Fotos verändert werden.
/// </summary>
public class BackupServiceTests_Versions : IDisposable
{
    private static readonly byte[] Photo = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

    private readonly BackupTestEnvironment _environment = new();

    private async Task<byte[]> CreateOldDatabaseBytesAsync(string migrationSuffix)
    {
        using var old = new TestDatabase();
        await using (var context = old.CreateFactory().CreateDbContext())
        {
            var target = context.Database.GetMigrations().Single(m => m.EndsWith(migrationSuffix, StringComparison.Ordinal));
            await context.GetService<IMigrator>().MigrateAsync(target);
            await context.Database.ExecuteSqlRawAsync("INSERT INTO Vehicles (Id, Name, FuelKind, StartOdometerKm, StartConsumption, CreatedUtc) VALUES (1, 'Altwagen', 'Petrol', 1000, NULL, '2026-10-11 09:00:00')");
            await context.Database.ExecuteSqlRawAsync("INSERT INTO TankbookEntries (VehicleId, CreatedUtc) VALUES (1, '2026-10-11 10:00:00')");
        }

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        return await File.ReadAllBytesAsync(old.DatabasePath);
    }

    private string PlaceArchive(Action<string> create, string name = "tankatlas-backup-20261001-100000.zip")
    {
        Directory.CreateDirectory(_environment.BackupDirectory);
        create(Path.Combine(_environment.BackupDirectory, name));
        return name;
    }

    /// <summary>
    /// Prüft, dass eine Sicherung eines älteren Migrationsstands wiederhergestellt, auf den aktuellen Stand migriert wird und ihre Daten erhalten bleiben.
    /// </summary>
    [Fact]
    public async Task Restore_OlderMigration_IsMigratedAndKeepsData()
    {
        var oldDatabase = await CreateOldDatabaseBytesAsync("_AddVehicles");
        var migration = (await _environment.CreateFactory().CreateDbContextAsync()).Database.GetMigrations().Single(m => m.EndsWith("_AddVehicles", StringComparison.Ordinal));
        var name = PlaceArchive(path => BackupTestArchive.Create(path, oldDatabase, migration: migration, appVersion: "0.1.15"));
        var service = _environment.CreateService();

        var result = await service.RestoreAsync(name);

        Assert.Equal(BackupResult.Ok, result);
        Assert.Equal(["Altwagen"], await _environment.GetVehicleNamesAsync());
        await using var context = _environment.CreateFactory().CreateDbContext();
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        var entry = await context.TankbookEntries.SingleAsync();
        Assert.Equal(1000, entry.OdometerKm);
        Assert.Equal("Fuel", entry.Kind);
    }

    /// <summary>
    /// Prüft, dass eine Sicherung mit neuerer Migration oder neuerer Formatversion als zu neu abgelehnt wird und die Livedaten bytegleich bleiben.
    /// </summary>
    /// <param name="migration">Die Migration im Manifest.</param>
    /// <param name="schemaVersion">Die Formatversion im Manifest.</param>
    [Theory]
    [InlineData("29991231000000_Future", 1)]
    [InlineData(BackupTestArchive.DefaultMigration, 99)]
    public async Task Restore_TooNewBackup_IsRejected_AndKeepsLiveData(string migration, int schemaVersion)
    {
        await _environment.SeedAsync(Photo);
        var database = await File.ReadAllBytesAsync(Path.Combine(_environment.DataDirectory, "tankatlas.db"));
        var name = PlaceArchive(path => BackupTestArchive.Create(path, database, migration: migration, schemaVersion: schemaVersion));
        var before = _environment.Snapshot();

        var result = await _environment.CreateService().RestoreAsync(name);

        Assert.Equal(BackupResult.TooNew, result);
        Assert.Equal(before, _environment.Snapshot());
        Assert.False(Directory.Exists(Path.Combine(_environment.DataDirectory, BackupFormat.StagingDirectoryName)));
        Assert.Equal(0, _environment.CacheInvalidations);
    }

    /// <summary>
    /// Prüft, dass ungültige oder manipulierte Dateien als ungültig erkannt werden und die Livedaten unverändert bleiben.
    /// </summary>
    [Fact]
    public async Task Restore_InvalidFiles_AreRejected_AndKeepLiveData()
    {
        await _environment.SeedAsync(Photo);
        var database = await File.ReadAllBytesAsync(Path.Combine(_environment.DataDirectory, "tankatlas.db"));
        var before = _environment.Snapshot();
        var cases = new Dictionary<string, Action<string>>
        {
            ["tankatlas-backup-20261001-000001.zip"] = path => File.WriteAllText(path, "kein Archiv"),
            ["tankatlas-backup-20261001-000002.zip"] = path => File.WriteAllBytes(path, []),
            ["tankatlas-backup-20261001-000003.zip"] = path =>
            {
                BackupTestArchive.Create(path, database, new Dictionary<string, byte[]> { [BackupTestEnvironment.PhotoName] = Photo });
                BackupTestArchive.Truncate(path);
            },
            ["tankatlas-backup-20261001-000004.zip"] = path => BackupTestArchive.CreateWithoutManifest(path, database),
            ["tankatlas-backup-20261001-000005.zip"] = path => BackupTestArchive.Create(path, database, wrongDatabaseHash: true),
            ["tankatlas-backup-20261001-000006.zip"] = path => BackupTestArchive.Create(path, "keine Datenbank"u8.ToArray()),
            ["tankatlas-backup-20261001-000007.zip"] = path => BackupTestArchive.Create(path, database, extraEntries: [("../evil.txt", [1, 2, 3])], listExtraEntriesInManifest: true),
            ["tankatlas-backup-20261001-000008.zip"] = path => BackupTestArchive.Create(path, database, new Dictionary<string, byte[]> { [BackupTestEnvironment.PhotoName] = "kein Foto"u8.ToArray() }),
        };
        foreach (var (name, create) in cases)
        {
            PlaceArchive(create, name);
        }

        var service = _environment.CreateService();
        foreach (var name in cases.Keys)
        {
            Assert.Equal(BackupResult.InvalidFile, await service.RestoreAsync(name));
            Assert.Equal(before, _environment.Snapshot());
        }

        Assert.False(File.Exists(Path.Combine(_environment.DataDirectory, "..", "evil.txt")));
        Assert.False(Directory.Exists(Path.Combine(_environment.DataDirectory, BackupFormat.StagingDirectoryName)));
        Assert.Equal(0, _environment.CacheInvalidations);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _environment.Dispose();
        GC.SuppressFinalize(this);
    }
}

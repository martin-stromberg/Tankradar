using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Services.Backup;
using Tankradar.Tests.Integration.Integration.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft den Neuaufbau der Datenbank beim Einspielen: Präparierte Objekte der Sicherung (Trigger auch mit reserviertem Präfix, View, <c>CHECK</c>, Zähler, Migrationssperre,
/// Kollation, zusätzliche Spalte, Tabelle und Index) gelangen nie in die Livedatenbank, die Daten bleiben vollständig, die App funktioniert danach, und nichts hängt.
/// </summary>
public class BackupServiceTests_Rebuild : IDisposable
{
    private static readonly byte[] Photo = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(60);

    private readonly BackupTestEnvironment _environment = new();

    /// <summary>
    /// Alle Manipulationen, deren Wirkung beim Neuaufbau verloren geht.
    /// </summary>
    public static TheoryData<BackupTamper> CleanedTampers
    {
        get
        {
            var data = new TheoryData<BackupTamper>();
            foreach (var tamper in Enum.GetValues<BackupTamper>().Where(tamper => !BackupTestTampering.IsRejected(tamper)))
            {
                data.Add(tamper);
            }

            return data;
        }
    }

    private static List<(string Type, string Name, string Sql)> ReadSchema(string databasePath)
    {
        using var connection = new SqliteConnection(TankradarDbContext.BuildConnectionString(databasePath, pooling: false));
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT type, name, COALESCE(sql, '') FROM sqlite_master WHERE name NOT LIKE '%Lock' ORDER BY type, name";
        using var reader = command.ExecuteReader();
        var list = new List<(string, string, string)>();
        while (reader.Read())
        {
            list.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        }

        return list;
    }

    private static string Scalar(string databasePath, string sql)
    {
        using var connection = new SqliteConnection(TankradarDbContext.BuildConnectionString(databasePath, pooling: false));
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToString(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static byte[] ReadDatabase(string zipPath)
    {
        using var archive = System.IO.Compression.ZipFile.OpenRead(zipPath);
        using var stream = archive.GetEntry("database/tankatlas.db")!.Open();
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private async Task<string> CreateTamperedArchiveAsync(BackupTamper tamper)
    {
        await _environment.SeedAsync(Photo);
        var created = await _environment.CreateService().CreateAsync();
        var source = Path.Combine(_environment.BackupDirectory, created.File!.FileName);
        var target = Path.Combine(_environment.BackupDirectory, "tankatlas-backup-20261001-100000.zip");
        BackupTestTampering.CreateTamperedCopy(source, target, tamper);
        File.Delete(source);
        return Path.GetFileName(target);
    }

    private async Task AssertCleanAndUsableAsync()
    {
        var databasePath = TankradarDbContext.GetDatabasePath(_environment.DataDirectory);
        SqliteConnection.ClearAllPools();

        // Das Schema entspricht dem einer frisch angelegten Datenbank: keine fremden Objekte, keine veränderten Tabellentexte.
        using var fresh = new TestDatabase();
        await fresh.CreateInitializer(fresh.CreateFactory()).InitializeAsync();
        SqliteConnection.ClearAllPools();
        Assert.Equal(ReadSchema(fresh.DatabasePath), ReadSchema(databasePath));
        Assert.Equal("0", Scalar(databasePath, "SELECT COUNT(*) FROM sqlite_master WHERE type IN ('trigger', 'view')"));
        Assert.Equal("0", Scalar(databasePath, "SELECT COUNT(*) FROM __EFMigrationsLock"));

        // Der Autoinkrement-Zähler ergibt sich aus den Daten.
        Assert.Equal("1", Scalar(databasePath, "SELECT seq FROM sqlite_sequence WHERE name = 'Vehicles'"));
        Assert.Equal("1", Scalar(databasePath, "SELECT seq FROM sqlite_sequence WHERE name = 'TankbookEntries'"));

        // Die Bestandsdaten sind vollständig, und die App kann danach Fahrzeuge und Vorgänge anlegen; vorhandene Vorgänge bleiben.
        await using var context = _environment.CreateFactory().CreateDbContext();
        Assert.Equal(["Golf"], await context.Vehicles.Select(v => v.Name).ToListAsync());
        Assert.Equal(1, await context.TankbookEntries.CountAsync());
        var vehicle = new VehicleEntity { Name = "Neu", FuelKind = "Petrol", StartOdometerKm = 10, CreatedUtc = DateTime.UtcNow };
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync();
        context.TankbookEntries.Add(new TankbookEntryEntity { Vehicle = vehicle, CreatedUtc = DateTime.UtcNow, OccurredAt = new DateTime(2026, 10, 12, 8, 0, 0), Kind = "Fuel", OdometerKm = 20 });
        await context.SaveChangesAsync();
        Assert.Equal(2, await context.TankbookEntries.CountAsync());
        Assert.Equal(2, await context.Vehicles.CountAsync());
    }

    /// <summary>
    /// Prüft, dass die Wiederherstellung einer präparierten Datenbank nur die Daten übernimmt und eine saubere, voll funktionsfähige Datenbank ergibt.
    /// </summary>
    /// <param name="tamper">Die Art der Manipulation.</param>
    [Theory]
    [MemberData(nameof(CleanedTampers))]
    public async Task Restore_PreparedDatabase_IsRebuiltCleanly(BackupTamper tamper)
    {
        var name = await CreateTamperedArchiveAsync(tamper);
        await using (var context = _environment.CreateFactory().CreateDbContext())
        {
            context.Vehicles.Add(new VehicleEntity { Name = "Opel", FuelKind = "Petrol", CreatedUtc = DateTime.UtcNow });
            await context.SaveChangesAsync();
        }

        var result = await _environment.CreateService().RestoreAsync(name).WaitAsync(Limit);

        Assert.Equal(BackupResult.Ok, result);
        await AssertCleanAndUsableAsync();
        Assert.False(Directory.Exists(Path.Combine(_environment.DataDirectory, BackupFormat.StagingDirectoryName)));
    }

    /// <summary>
    /// Prüft, dass schon das Einspielen einer präparierten Datei sauber gelingt und danach die Wiederherstellung dasselbe Ergebnis liefert.
    /// </summary>
    /// <param name="tamper">Die Art der Manipulation.</param>
    [Theory]
    [MemberData(nameof(CleanedTampers))]
    public async Task Import_PreparedDatabase_IsAcceptedAndRestoredCleanly(BackupTamper tamper)
    {
        var name = await CreateTamperedArchiveAsync(tamper);
        var external = Path.Combine(_environment.DataDirectory, "extern.zip");
        File.Move(Path.Combine(_environment.BackupDirectory, name), external);
        var service = _environment.CreateService();

        var outcome = await service.ImportAsync(new PickedBackupFile("extern.zip", () => Task.FromResult<Stream>(File.OpenRead(external)))).WaitAsync(Limit);
        Assert.Equal(BackupResult.Ok, outcome.Result);
        var result = await service.RestoreAsync(outcome.File!.FileName).WaitAsync(Limit);

        Assert.Equal(BackupResult.Ok, result);
        await AssertCleanAndUsableAsync();
    }

    /// <summary>
    /// Wächter für die Tests: Die präparierten Dateien enthalten die Manipulation tatsächlich (sonst bewiese das Einspielen nichts).
    /// </summary>
    /// <param name="tamper">Die Art der Manipulation.</param>
    [Theory]
    [MemberData(nameof(CleanedTampers))]
    public async Task TamperedArchive_ReallyContainsTheManipulation(BackupTamper tamper)
    {
        var name = await CreateTamperedArchiveAsync(tamper);
        var path = Path.Combine(Path.GetTempPath(), "tankatlas-guard-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            await File.WriteAllBytesAsync(path, ReadDatabase(Path.Combine(_environment.BackupDirectory, name)));
            var objects = ReadSchema(path);
            var marker = tamper switch
            {
                BackupTamper.Trigger => Scalar(path, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'trigger' AND name = 'evil_trigger'"),
                BackupTamper.PrefixedTrigger => Scalar(path, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'trigger' AND name = 'sqlite_evil'"),
                BackupTamper.View => Scalar(path, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'view'"),
                BackupTamper.ExtraCheck => Scalar(path, "SELECT COUNT(*) FROM sqlite_master WHERE name = 'Vehicles' AND sql LIKE '%CHECK (Id < 2)%'"),
                BackupTamper.ManipulatedSequence => Scalar(path, "SELECT COUNT(*) FROM sqlite_sequence WHERE name = 'Vehicles' AND seq = 9223372036854775807"),
                BackupTamper.MigrationLockRow => Scalar(path, "SELECT COUNT(*) FROM __EFMigrationsLock"),
                BackupTamper.RemovedCollation => Scalar(path, "SELECT COUNT(*) FROM sqlite_master WHERE name = 'Vehicles' AND sql NOT LIKE '%NOCASE%'"),
                BackupTamper.ExtraColumn => Scalar(path, "SELECT COUNT(*) FROM pragma_table_info('Vehicles') WHERE name = 'EvilColumn'"),
                BackupTamper.ExtraTable => Scalar(path, "SELECT COUNT(*) FROM sqlite_master WHERE name = 'EvilTable'"),
                BackupTamper.ExtraIndex => Scalar(path, "SELECT COUNT(*) FROM sqlite_master WHERE name = 'evil_index'"),
                _ => throw new ArgumentOutOfRangeException(nameof(tamper)),
            };
            Assert.Equal("1", marker);
            Assert.NotEmpty(objects);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(path);
        }
    }

    /// <summary>
    /// Prüft, dass die Zählwerte der Rückfrage aus der geprüften Datenbank stammen und nicht aus dem (hier bewusst falschen) Manifest.
    /// </summary>
    [Fact]
    public async Task Inspect_CountsComeFromTheVerifiedDatabase_NotFromTheManifest()
    {
        await _environment.SeedAsync(Photo);
        var created = await _environment.CreateService().CreateAsync();
        var path = Path.Combine(_environment.BackupDirectory, created.File!.FileName);
        var database = ReadDatabase(path);
        File.Delete(path);

        // Das Hilfsarchiv schreibt feste, falsche Zählwerte (1/2/3/4/5/6) ins Manifest.
        var name = "tankatlas-backup-20261001-100000.zip";
        BackupTestArchive.Create(Path.Combine(_environment.BackupDirectory, name), database, new Dictionary<string, byte[]> { [BackupTestEnvironment.PhotoName] = Photo });

        var outcome = await _environment.CreateService().InspectAsync(name).WaitAsync(Limit);

        Assert.Equal(BackupResult.Ok, outcome.Result);
        var counts = outcome.Manifest!.Counts;
        Assert.Equal((1, 1, 1, 1, 1, 1, 1), (counts.Vehicles, counts.Entries, counts.Groups, counts.Favorites, counts.Stations, counts.Prices, counts.Photos));
        Assert.False(Directory.Exists(Path.Combine(_environment.DataDirectory, BackupFormat.StagingDirectoryName)));
    }

    /// <summary>
    /// Prüft, dass die Rückfrage zu einer unbrauchbaren Datenbank mit <see cref="BackupResult.InvalidFile"/> endet statt gefälschte Zählwerte zu nennen.
    /// </summary>
    [Fact]
    public async Task Inspect_UnusableDatabase_IsInvalid()
    {
        var name = await CreateTamperedArchiveAsync(BackupTamper.InvalidDate);

        var outcome = await _environment.CreateService().InspectAsync(name).WaitAsync(Limit);

        Assert.Equal(BackupResult.InvalidFile, outcome.Result);
        Assert.Null(outcome.Manifest);
    }

    /// <summary>
    /// Prüft, dass eine Sicherung eines älteren Migrationsstands (Schema der Version 0.1.14) mit Zeile in der Migrationssperre vollständig übernommen, migriert und nicht blockiert wird.
    /// </summary>
    [Fact]
    public async Task Restore_Version0114Schema_WithLockRow_IsMigratedAndComplete()
    {
        byte[] oldDatabase;
        string migration;
        using (var old = new TestDatabase())
        {
            await using (var context = old.CreateFactory().CreateDbContext())
            {
                migration = context.Database.GetMigrations().Single(m => m.EndsWith("_AddFavorites", StringComparison.Ordinal));
                await context.GetService<IMigrator>().MigrateAsync(migration);
                await context.Database.ExecuteSqlRawAsync("INSERT INTO FavoriteGroups (Name, CreatedUtc) VALUES ('Arbeitsweg', '2026-10-06 08:00:00')");
                await context.Database.ExecuteSqlRawAsync("INSERT INTO UserSettings (Id, GpsUsage, ResultView, ResultSortOrder) VALUES (1, 'Never', 'Map', 'Name')");
                await context.Database.ExecuteSqlRawAsync("INSERT INTO __EFMigrationsLock (Id, Timestamp) VALUES (1, '2026-10-06 08:00:00')");
            }

            SqliteConnection.ClearAllPools();
            oldDatabase = await File.ReadAllBytesAsync(old.DatabasePath);
        }

        await _environment.SeedAsync(Photo);
        Directory.CreateDirectory(_environment.BackupDirectory);
        var name = "tankatlas-backup-20261001-100000.zip";
        BackupTestArchive.Create(Path.Combine(_environment.BackupDirectory, name), oldDatabase, migration: migration, appVersion: "0.1.14");

        var result = await _environment.CreateService().RestoreAsync(name).WaitAsync(Limit);

        Assert.Equal(BackupResult.Ok, result);
        await using var live = _environment.CreateFactory().CreateDbContext();
        Assert.Empty(await live.Database.GetPendingMigrationsAsync());
        Assert.Equal(["Arbeitsweg"], await live.FavoriteGroups.Select(g => g.Name).ToListAsync());
        Assert.Equal("Never", (await live.UserSettings.SingleAsync()).GpsUsage);
        Assert.False((await live.UserSettings.SingleAsync()).ShowNearby);
        Assert.Empty(await live.Vehicles.ToListAsync());
        live.Vehicles.Add(new VehicleEntity { Name = "Neu", FuelKind = "Petrol", CreatedUtc = DateTime.UtcNow });
        await live.SaveChangesAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _environment.Dispose();
        GC.SuppressFinalize(this);
    }
}

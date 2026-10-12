using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models.Backup;
using Tankradar.Tests.Integration.Integration.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft das Erstellen einer Sicherung auf einer echten SQLite-Datei: Umfang, Manifest, Dateischutz, Aufräumen und Vollständigkeit nach der Wiederherstellung.
/// </summary>
public class BackupServiceTests_Create : IDisposable
{
    private static readonly byte[] Photo = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

    private readonly BackupTestEnvironment _environment = new();

    /// <summary>
    /// Prüft, dass das Archiv genau Manifest, Datenbank und Fotos enthält, das Manifest stimmt und kein Rest zurückbleibt.
    /// </summary>
    [Fact]
    public async Task Create_ContainsOnlyManifestDatabaseAndPhotos_WithMatchingManifest()
    {
        await _environment.SeedAsync(Photo);
        var service = _environment.CreateService();

        var outcome = await service.CreateAsync();

        Assert.Equal(BackupResult.Ok, outcome.Result);
        var path = service.GetFullPath(outcome.File!.FileName)!;
        using var archive = ZipFile.OpenRead(path);
        Assert.Equal(
            ["database/tankatlas.db", "manifest.json", "receipts/" + BackupTestEnvironment.PhotoName],
            archive.Entries.Select(e => e.FullName).Order(StringComparer.Ordinal).ToArray());
        var manifest = Tankradar.MAUI.Services.Backup.BackupArchiveValidator.ReadManifest(path)!;
        Assert.Equal(BackupFormat.FormatName, manifest.Format);
        Assert.Equal(BackupFormat.SchemaVersion, manifest.SchemaVersion);
        Assert.Equal(1, manifest.Counts.Vehicles);
        Assert.Equal(1, manifest.Counts.Entries);
        Assert.Equal(1, manifest.Counts.Groups);
        Assert.Equal(1, manifest.Counts.Favorites);
        Assert.Equal(1, manifest.Counts.Stations);
        Assert.Equal(1, manifest.Counts.Prices);
        Assert.EndsWith("_AddTankbookEntryDetails", manifest.DatabaseMigration, StringComparison.Ordinal);
        var photoEntry = manifest.Files.Single(f => f.Path.StartsWith("receipts/", StringComparison.Ordinal));
        Assert.Equal(BackupTestArchive.Hash(Photo), photoEntry.Sha256);
        Assert.Equal(Photo.Length, photoEntry.Size);
        Assert.Contains(path, _environment.ProtectedFiles);
        Assert.Empty(Directory.GetFiles(_environment.BackupDirectory, "*.tmp"));
        Assert.Empty(Directory.GetDirectories(_environment.BackupDirectory));
    }

    /// <summary>
    /// Prüft, dass die Sicherung keine anderen Tabellen als die bekannten und keine Schlüssel- oder Standortspalten enthält.
    /// </summary>
    [Fact]
    public async Task Create_DatabaseHasOnlyKnownTables()
    {
        await _environment.SeedAsync(Photo);
        var service = _environment.CreateService();
        var outcome = await service.CreateAsync();

        var restoredDirectory = Path.Combine(_environment.DataDirectory, "inspect");
        Directory.CreateDirectory(restoredDirectory);
        using (var archive = ZipFile.OpenRead(service.GetFullPath(outcome.File!.FileName)!))
        {
            archive.GetEntry("database/tankatlas.db")!.ExtractToFile(Path.Combine(restoredDirectory, "x.db"));
        }

        var connectionString = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = Path.Combine(restoredDirectory, "x.db"), Pooling = false }.ToString();
        var tables = new List<string>();
        await using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

        Assert.Equal(
            ["FavoriteEntries", "FavoriteGroups", "FuelTypeSettings", "PriceEntries", "Stations", "TankbookEntries", "UserSettings", "Vehicles", "__EFMigrationsHistory", "__EFMigrationsLock"],
            tables);
    }

    /// <summary>
    /// Prüft, dass eine Sicherung ohne Fotos und mit Fotos nacheinander eigene Dateien erhalten und die Datenbank währenddessen unverändert bleibt.
    /// </summary>
    [Fact]
    public async Task Create_Twice_CreatesTwoFilesAndLeavesLiveDataUnchanged()
    {
        await _environment.SeedAsync(Photo);
        var service = _environment.CreateService();
        var before = _environment.Snapshot();

        var first = await service.CreateAsync();
        var second = await service.CreateAsync();

        Assert.NotEqual(first.File!.FileName, second.File!.FileName);
        Assert.Equal(2, (await service.ListAsync()).Count);
        Assert.Equal(before["receipts/" + BackupTestEnvironment.PhotoName], _environment.Snapshot()["receipts/" + BackupTestEnvironment.PhotoName]);
    }

    /// <summary>
    /// Prüft die Vollständigkeit: Alle Entitäten samt Zeitstempeln und Foto-Bytes sind nach dem Wiederherstellen in einem neuen Datenverzeichnis identisch.
    /// </summary>
    [Fact]
    public async Task Completeness_AllEntitiesTimestampsAndPhotoBytes_SurviveRestore()
    {
        await _environment.SeedAsync(Photo);
        var created = await _environment.CreateService().CreateAsync();
        var archivePath = Path.Combine(_environment.BackupDirectory, created.File!.FileName);

        using var target = new BackupTestEnvironment();
        var targetService = target.CreateService();
        Directory.CreateDirectory(target.BackupDirectory);
        var copy = Path.Combine(target.BackupDirectory, created.File.FileName);
        File.Copy(archivePath, copy);

        Assert.Equal(BackupResult.Ok, await targetService.RestoreAsync(created.File.FileName));

        await using var context = target.CreateFactory().CreateDbContext();
        var settings = await context.UserSettings.SingleAsync();
        Assert.Equal(("Never", "Map", "Name", false), (settings.GpsUsage, settings.ResultView, settings.ResultSortOrder, settings.ShowNearby));
        Assert.Equal("Diesel", (await context.FuelTypeSettings.SingleAsync()).FuelTypeKey);
        var station = await context.Stations.SingleAsync();
        Assert.Equal(("Alpha Tankstelle", "Alpha", "Berlin", 52.5, 13.4), (station.Name, station.Brand, station.Place, station.Latitude, station.Longitude));
        Assert.Equal(new DateTime(2026, 10, 5, 10, 0, 0, DateTimeKind.Utc), DateTime.SpecifyKind(station.DetailsUpdatedUtc!.Value, DateTimeKind.Utc));
        var price = await context.PriceEntries.SingleAsync();
        Assert.Equal(1.699m, price.Price);
        Assert.Equal(new DateTime(2026, 10, 5, 10, 0, 7), price.RetrievedUtc);
        var group = await context.FavoriteGroups.SingleAsync();
        Assert.Equal(("Arbeitsweg", "Täglich"), (group.Name, group.Description));
        var favorite = await context.FavoriteEntries.SingleAsync();
        Assert.Equal(("Günstig", "High"), (favorite.Note, favorite.Priority));
        var vehicle = await context.Vehicles.SingleAsync();
        Assert.Equal(("Golf", 84350, 5.4m), (vehicle.Name, vehicle.StartOdometerKm, vehicle.StartConsumption));
        var entry = await context.TankbookEntries.SingleAsync();
        Assert.Equal((40.123m, 1.699m, 68.17m, 84900, BackupTestEnvironment.PhotoName), (entry.Quantity, entry.UnitPrice, entry.TotalAmount, entry.OdometerKm, entry.ReceiptPhotoFileName));
        Assert.Equal(new DateTime(2026, 10, 11, 12, 30, 0), entry.OccurredAt);
        Assert.Equal(Photo, await File.ReadAllBytesAsync(Path.Combine(target.ReceiptsDirectory, BackupTestEnvironment.PhotoName)));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _environment.Dispose();
        GC.SuppressFinalize(this);
    }
}

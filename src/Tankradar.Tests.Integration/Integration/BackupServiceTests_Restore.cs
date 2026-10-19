using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Models.Backup;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft das Wiederherstellen auf einer echten SQLite-Datei: Überschreiben der geänderten Daten, Ersetzen der Fotos, Dateischutz, Zwischenspeicher und Aufräumen.
/// </summary>
public class BackupServiceTests_Restore : IDisposable
{
    private static readonly byte[] Photo = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

    private readonly BackupTestEnvironment _environment = new();

    /// <summary>
    /// Prüft, dass nach der Sicherung geänderte Daten und Fotos durch den gesicherten Stand ersetzt werden, auch bei nicht freigegebenen Verbindungen.
    /// </summary>
    [Fact]
    public async Task Restore_ReplacesChangedDataAndPhotos()
    {
        await _environment.SeedAsync(Photo);
        var service = _environment.CreateService();
        var created = await service.CreateAsync();
        await using (var context = _environment.CreateFactory().CreateDbContext())
        {
            context.Vehicles.Add(new Tankradar.MAUI.Data.VehicleEntity { Name = "Opel", FuelKind = "Petrol", CreatedUtc = DateTime.UtcNow });
            context.UserSettings.Single().ShowNearby = true;
            await context.SaveChangesAsync();
        }

        await File.WriteAllBytesAsync(Path.Combine(_environment.ReceiptsDirectory, BackupTestEnvironment.PhotoName), [0xFF, 0xD8, 0xFF, 9, 9, 9]);
        await File.WriteAllBytesAsync(Path.Combine(_environment.ReceiptsDirectory, "fedcba9876543210fedcba9876543210.jpg"), [0xFF, 0xD8, 0xFF, 7]);
        Assert.Equal(["Golf", "Opel"], await _environment.GetVehicleNamesAsync());

        var result = await service.RestoreAsync(created.File!.FileName);

        Assert.Equal(BackupResult.Ok, result);
        Assert.Equal(["Golf"], await _environment.GetVehicleNamesAsync());
        await using (var check = _environment.CreateFactory().CreateDbContext())
        {
            Assert.False((await check.UserSettings.SingleAsync()).ShowNearby);
        }

        Assert.Equal(Photo, await File.ReadAllBytesAsync(Path.Combine(_environment.ReceiptsDirectory, BackupTestEnvironment.PhotoName)));
        Assert.Single(Directory.GetFiles(_environment.ReceiptsDirectory));
        Assert.Equal(1, _environment.CacheInvalidations);
        Assert.Contains(Path.Combine(_environment.DataDirectory, "tankatlas.db"), _environment.ProtectedDatabases);
        Assert.Contains(Path.Combine(_environment.ReceiptsDirectory, BackupTestEnvironment.PhotoName), _environment.ProtectedFiles);
        AssertNoLeftovers();
        Assert.Single(await service.ListAsync());
    }

    /// <summary>
    /// Prüft, dass eine Sicherung ohne Fotos die vorhandenen Fotos entfernt und der Name einer fehlenden Datei als nicht gefunden gilt.
    /// </summary>
    [Fact]
    public async Task Restore_UnknownFile_IsNotFound_AndKeepsData()
    {
        await _environment.SeedAsync(Photo);
        var service = _environment.CreateService();
        var before = _environment.Snapshot();

        Assert.Equal(BackupResult.NotFound, await service.RestoreAsync("tankatlas-backup-20990101-000000.zip"));
        Assert.Equal(BackupResult.NotFound, await service.RestoreAsync(@"..\tankatlas.db"));

        Assert.Equal(before, _environment.Snapshot());
        Assert.Equal(0, _environment.CacheInvalidations);
    }

    private void AssertNoLeftovers()
    {
        Assert.False(Directory.Exists(Path.Combine(_environment.DataDirectory, BackupFormat.StagingDirectoryName)));
        Assert.False(Directory.Exists(Path.Combine(_environment.DataDirectory, BackupFormat.RollbackDirectoryName)));
        Assert.False(File.Exists(Path.Combine(_environment.DataDirectory, BackupFormat.JournalFileName)));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _environment.Dispose();
        GC.SuppressFinalize(this);
    }
}

using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Services.Backup;
using Tankradar.Tests.Integration.Integration.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft, dass eine Sicherung mit präparierter oder fremder Datenbank (gültiges Manifest, gültige Prüfsummen) beim Wiederherstellen und beim Einspielen abgelehnt wird
/// und der Bestand unverändert bleibt: zusätzlicher Trigger, zusätzliche View, fehlende Tabelle, zusätzliche Spalte, zusätzliche Tabelle, zusätzlicher Index.
/// </summary>
public class BackupServiceTests_Schema : IDisposable
{
    private static readonly byte[] Photo = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

    private readonly BackupTestEnvironment _environment = new();

    /// <summary>
    /// Alle Arten der Schemamanipulation für die Theorien.
    /// </summary>
    public static TheoryData<BackupTamper> Tampers
    {
        get
        {
            var data = new TheoryData<BackupTamper>();
            foreach (var tamper in Enum.GetValues<BackupTamper>())
            {
                data.Add(tamper);
            }

            return data;
        }
    }

    private async Task<string> CreateTamperedArchiveAsync(BackupTamper tamper, string archiveName)
    {
        await _environment.SeedAsync(Photo);
        var created = await _environment.CreateService().CreateAsync();
        var source = Path.Combine(_environment.BackupDirectory, created.File!.FileName);
        var target = Path.Combine(_environment.BackupDirectory, archiveName);
        BackupTestTampering.CreateTamperedCopy(source, target, tamper);
        File.Delete(source);
        return target;
    }

    private static PickedBackupFile Pick(string path)
    {
        return new PickedBackupFile(Path.GetFileName(path), () => Task.FromResult<Stream>(File.OpenRead(path)));
    }

    /// <summary>
    /// Prüft, dass die Wiederherstellung einer manipulierten Datenbank mit <see cref="BackupResult.InvalidFile"/> scheitert und Datenbank, Fotos und Zwischenspeicher unberührt bleiben.
    /// </summary>
    /// <param name="tamper">Die Art der Manipulation.</param>
    [Theory]
    [MemberData(nameof(Tampers))]
    public async Task Restore_TamperedSchema_IsRejected_AndKeepsLiveData(BackupTamper tamper)
    {
        var archive = await CreateTamperedArchiveAsync(tamper, "tankatlas-backup-20261001-100000.zip");
        await using (var context = _environment.CreateFactory().CreateDbContext())
        {
            context.Vehicles.Add(new Tankradar.MAUI.Data.VehicleEntity { Name = "Opel", FuelKind = "Petrol", CreatedUtc = DateTime.UtcNow });
            await context.SaveChangesAsync();
        }

        var before = _environment.Snapshot();

        var result = await _environment.CreateService().RestoreAsync(Path.GetFileName(archive));

        Assert.Equal(BackupResult.InvalidFile, result);
        Assert.Equal(before, _environment.Snapshot());
        Assert.Equal(["Golf", "Opel"], await _environment.GetVehicleNamesAsync());
        Assert.False(Directory.Exists(Path.Combine(_environment.DataDirectory, BackupFormat.StagingDirectoryName)));
        Assert.False(RestoreJournal.Exists(_environment.DataDirectory));
        Assert.Equal(0, _environment.CacheInvalidations);
    }

    /// <summary>
    /// Prüft, dass schon das Einspielen einer manipulierten Datei mit <see cref="BackupResult.InvalidFile"/> scheitert, nichts in die Liste gelangt und kein Rest bleibt.
    /// </summary>
    /// <param name="tamper">Die Art der Manipulation.</param>
    [Theory]
    [MemberData(nameof(Tampers))]
    public async Task Import_TamperedSchema_IsRejected_WithoutLeftovers(BackupTamper tamper)
    {
        var archive = await CreateTamperedArchiveAsync(tamper, "tankatlas-backup-20261001-100000.zip");
        var external = Path.Combine(_environment.DataDirectory, "extern.zip");
        File.Move(archive, external);
        var before = _environment.Snapshot();
        var service = _environment.CreateService();

        var outcome = await service.ImportAsync(Pick(external));

        Assert.Equal(BackupResult.InvalidFile, outcome.Result);
        Assert.Null(outcome.File);
        Assert.Empty(await service.ListAsync());
        Assert.Empty(Directory.GetFileSystemEntries(_environment.BackupDirectory));
        Assert.Equal(before, _environment.Snapshot());
        Assert.False(Directory.Exists(Path.Combine(_environment.DataDirectory, BackupFormat.StagingDirectoryName)));
    }

    /// <summary>
    /// Prüft, dass eine unveränderte, gültige Datei weiterhin eingespielt wird (kein Fehlalarm des Schemavergleichs).
    /// </summary>
    [Fact]
    public async Task Import_UntamperedFile_IsAccepted()
    {
        await _environment.SeedAsync(Photo);
        var service = _environment.CreateService();
        var created = await service.CreateAsync();
        var external = Path.Combine(_environment.DataDirectory, "extern.zip");
        File.Copy(Path.Combine(_environment.BackupDirectory, created.File!.FileName), external);
        File.Delete(Path.Combine(_environment.BackupDirectory, created.File.FileName));

        var outcome = await service.ImportAsync(Pick(external));

        Assert.Equal(BackupResult.Ok, outcome.Result);
        Assert.False(Directory.Exists(Path.Combine(_environment.DataDirectory, BackupFormat.StagingDirectoryName)));
    }

    /// <summary>
    /// Prüft, dass eine Datei mit unbrauchbarer Datenbank (Zufallsbytes, gültige Prüfsummen) schon beim Einspielen abgelehnt wird und nicht erst beim Wiederherstellen.
    /// </summary>
    [Fact]
    public async Task Import_UnusableDatabase_IsRejectedBeforeEnteringTheList()
    {
        await _environment.SeedAsync(Photo);
        var external = Path.Combine(_environment.DataDirectory, "garbage.zip");
        BackupTestArchive.Create(external, new byte[4096].Select((_, i) => (byte)((i * 31) + 7)).ToArray());
        var service = _environment.CreateService();

        var outcome = await service.ImportAsync(Pick(external));

        Assert.Equal(BackupResult.InvalidFile, outcome.Result);
        Assert.Empty(await service.ListAsync());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _environment.Dispose();
        GC.SuppressFinalize(this);
    }
}

using Tankradar.MAUI.Models.Backup;
using Tankradar.Tests.Integration.Integration.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft Liste, Löschen (mit Pfadschutz) und Import externer Dateien des <see cref="Tankradar.MAUI.Services.Backup.BackupService"/> auf einer echten Datei.
/// </summary>
[Collection(BackupCollection.Name)]
public class BackupServiceTests_ListDeleteImport : IDisposable
{
    private static readonly byte[] Photo = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

    private readonly BackupTestEnvironment _environment = new();

    private async Task<byte[]> DatabaseBytesAsync()
    {
        await _environment.SeedAsync(Photo);
        await _environment.CreateService().CreateAsync();
        var created = Directory.GetFiles(_environment.BackupDirectory, "*.zip").Single();
        using var memory = new MemoryStream();
        using (var archive = System.IO.Compression.ZipFile.OpenRead(created))
        using (var stream = archive.GetEntry("database/tankatlas.db")!.Open())
        {
            await stream.CopyToAsync(memory);
        }

        File.Delete(created);
        return memory.ToArray();
    }

    private static PickedBackupFile Pick(string path)
    {
        return new PickedBackupFile(Path.GetFileName(path), () => Task.FromResult<Stream>(File.OpenRead(path)));
    }

    /// <summary>
    /// Prüft Reihenfolge (neueste zuerst), Datum aus dem Manifest, Größe, Kennzeichnung unlesbarer Dateien und das Ignorieren und Bereinigen von Fremd- und Temporärdateien.
    /// </summary>
    [Fact]
    public async Task List_OrdersNewestFirst_MarksUnreadable_IgnoresAndCleansTemporaryFiles()
    {
        var database = await DatabaseBytesAsync();
        Directory.CreateDirectory(_environment.BackupDirectory);
        BackupTestArchive.Create(Path.Combine(_environment.BackupDirectory, "tankatlas-backup-20261001-100000.zip"), database, createdUtc: new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc));
        BackupTestArchive.Create(Path.Combine(_environment.BackupDirectory, "tankatlas-backup-20261003-100000.zip"), database, createdUtc: new DateTime(2026, 10, 3, 10, 0, 0, DateTimeKind.Utc));
        var broken = Path.Combine(_environment.BackupDirectory, "tankatlas-backup-20261002-100000.zip");
        File.WriteAllText(broken, "kaputt");
        File.SetLastWriteTimeUtc(broken, new DateTime(2026, 10, 2, 10, 0, 0, DateTimeKind.Utc));
        File.WriteAllText(Path.Combine(_environment.BackupDirectory, "tankatlas-backup-20261004-100000.zip.tmp"), "halb");
        File.WriteAllText(Path.Combine(_environment.BackupDirectory, "notizen.txt"), "fremd");
        Directory.CreateDirectory(Path.Combine(_environment.BackupDirectory, ".work-abc"));

        var list = await _environment.CreateService().ListAsync();

        Assert.Equal(
            ["tankatlas-backup-20261003-100000.zip", "tankatlas-backup-20261002-100000.zip", "tankatlas-backup-20261001-100000.zip"],
            list.Select(i => i.FileName).ToArray());
        Assert.Equal([true, false, true], list.Select(i => i.IsReadable).ToArray());
        Assert.Equal(new DateTime(2026, 10, 3, 10, 0, 0, DateTimeKind.Utc), list[0].CreatedUtc);
        Assert.All(list, i => Assert.True(i.SizeBytes > 0));
        Assert.False(File.Exists(Path.Combine(_environment.BackupDirectory, "tankatlas-backup-20261004-100000.zip.tmp")));
        Assert.False(Directory.Exists(Path.Combine(_environment.BackupDirectory, ".work-abc")));
        Assert.True(File.Exists(Path.Combine(_environment.BackupDirectory, "notizen.txt")));
    }

    /// <summary>
    /// Prüft, dass eine fehlende Sicherungsliste leer ist.
    /// </summary>
    [Fact]
    public async Task List_WithoutBackupDirectory_IsEmpty()
    {
        Assert.Empty(await _environment.CreateService().ListAsync());
    }

    /// <summary>
    /// Prüft, dass Löschen die Datei entfernt und Pfadangaben oder unbekannte Namen als nicht gefunden gelten, ohne dass etwas außerhalb gelöscht wird.
    /// </summary>
    [Fact]
    public async Task Delete_RemovesFile_AndRejectsPaths()
    {
        var database = await DatabaseBytesAsync();
        Directory.CreateDirectory(_environment.BackupDirectory);
        var name = "tankatlas-backup-20261001-100000.zip";
        BackupTestArchive.Create(Path.Combine(_environment.BackupDirectory, name), database);
        var outside = Path.Combine(_environment.DataDirectory, "tankatlas-backup-20261001-100000.zip");
        File.WriteAllText(outside, "darf bleiben");
        var service = _environment.CreateService();

        Assert.Equal(BackupResult.NotFound, await service.DeleteAsync(@"..\tankatlas-backup-20261001-100000.zip"));
        Assert.Equal(BackupResult.NotFound, await service.DeleteAsync(outside));
        Assert.Equal(BackupResult.NotFound, await service.DeleteAsync("tankatlas-backup-20990101-000000.zip"));
        Assert.True(File.Exists(outside));
        Assert.Single(Directory.GetFiles(_environment.BackupDirectory));

        Assert.Equal(BackupResult.Ok, await service.DeleteAsync(name));
        Assert.Empty(Directory.GetFiles(_environment.BackupDirectory));
        Assert.Equal(BackupResult.NotFound, await service.DeleteAsync(name));
    }

    /// <summary>
    /// Prüft, dass eine gültige externe Datei in die Liste übernommen wird, auch bei gleichem Zeitstempel (Zähler), und kein Rest bleibt.
    /// </summary>
    [Fact]
    public async Task Import_ValidFile_IsAddedToList_WithCounterOnCollision()
    {
        var database = await DatabaseBytesAsync();
        var external = Path.Combine(_environment.DataDirectory, "extern.zip");
        BackupTestArchive.Create(external, database, new Dictionary<string, byte[]> { [BackupTestEnvironment.PhotoName] = Photo }, createdUtc: new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc));
        var service = _environment.CreateService();

        var first = await service.ImportAsync(Pick(external));
        var second = await service.ImportAsync(Pick(external));

        Assert.Equal(BackupResult.Ok, first.Result);
        Assert.Equal("tankatlas-backup-20261001-100000.zip", first.File!.FileName);
        Assert.Equal(BackupResult.Ok, second.Result);
        Assert.Equal("tankatlas-backup-20261001-100000-2.zip", second.File!.FileName);
        Assert.Equal(2, (await service.ListAsync()).Count);
        Assert.Contains(Path.Combine(_environment.BackupDirectory, first.File.FileName), _environment.ProtectedFiles);
        Assert.Empty(Directory.GetFiles(_environment.BackupDirectory, "*.tmp"));
    }

    /// <summary>
    /// Prüft, dass ungültige, manipulierte und zu neue externe Dateien nicht in die Liste gelangen und keine temporäre Datei zurückbleibt.
    /// </summary>
    [Fact]
    public async Task Import_InvalidAndTooNewFiles_AreRejected_WithoutLeftovers()
    {
        var database = await DatabaseBytesAsync();
        var garbage = Path.Combine(_environment.DataDirectory, "text.zip");
        File.WriteAllText(garbage, "kein Archiv");
        var wrongHash = Path.Combine(_environment.DataDirectory, "hash.zip");
        BackupTestArchive.Create(wrongHash, database, wrongDatabaseHash: true);
        var slip = Path.Combine(_environment.DataDirectory, "slip.zip");
        BackupTestArchive.Create(slip, database, extraEntries: [("..\\evil.txt", [1])], listExtraEntriesInManifest: true);
        var tooNew = Path.Combine(_environment.DataDirectory, "neu.zip");
        BackupTestArchive.Create(tooNew, database, schemaVersion: 99);
        var service = _environment.CreateService();

        Assert.Equal(BackupResult.InvalidFile, (await service.ImportAsync(Pick(garbage))).Result);
        Assert.Equal(BackupResult.InvalidFile, (await service.ImportAsync(Pick(wrongHash))).Result);
        Assert.Equal(BackupResult.InvalidFile, (await service.ImportAsync(Pick(slip))).Result);
        Assert.Equal(BackupResult.TooNew, (await service.ImportAsync(Pick(tooNew))).Result);

        Assert.Empty(await service.ListAsync());
        Assert.Empty(Directory.GetFiles(_environment.BackupDirectory));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _environment.Dispose();
        GC.SuppressFinalize(this);
    }
}

using System.IO.Compression;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Services.Backup;
using Tankradar.Tests.Integration.Integration.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft die passwortverschlüsselte Sicherung auf einer echten SQLite-Datei: Erstellen, Kennzeichnung, Wiederherstellung, Fehlerfälle und zweistufiges Einspielen (ADR 0019).
/// </summary>
[Collection(BackupCollection.Name)]
public class BackupServiceTests_Encryption : IDisposable
{
    private const string Password = "geheim-123";
    private static readonly byte[] Photo = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

    private readonly BackupTestEnvironment _environment = new();

    /// <summary>
    /// Prüft, dass eine Sicherung mit Passwort als Container abgelegt und in der Liste als verschlüsselt gekennzeichnet wird.
    /// </summary>
    [Fact]
    public async Task Create_WithPassword_ProducesEncryptedMarkedFile()
    {
        await _environment.SeedAsync(Photo);
        var service = _environment.CreateService();

        var outcome = await service.CreateAsync(Password);

        Assert.Equal(BackupResult.Ok, outcome.Result);
        Assert.True(outcome.File!.IsEncrypted);
        Assert.True(outcome.File.IsReadable);
        var path = service.GetFullPath(outcome.File.FileName)!;
        using (var stream = File.OpenRead(path))
        {
            Assert.True(BackupCrypto.IsEncrypted(stream));
        }

        var entry = Assert.Single(await service.ListAsync());
        Assert.True(entry.IsEncrypted);
        Assert.Equal(string.Empty, entry.AppVersion);
        Assert.Contains(path, _environment.ProtectedFiles);
        Assert.Empty(Directory.GetFiles(_environment.BackupDirectory, "*.tmp"));
        Assert.Empty(Directory.GetDirectories(_environment.BackupDirectory));
    }

    /// <summary>
    /// Prüft, dass eine unverschlüsselte Sicherung weiterhin ein ZIP-Archiv ohne Kennzeichnung bleibt.
    /// </summary>
    [Fact]
    public async Task Create_WithoutPassword_StaysPlainZip()
    {
        await _environment.SeedAsync(Photo);
        var service = _environment.CreateService();

        var outcome = await service.CreateAsync();

        Assert.Equal(BackupResult.Ok, outcome.Result);
        Assert.False(outcome.File!.IsEncrypted);
        using var stream = File.OpenRead(service.GetFullPath(outcome.File.FileName)!);
        Assert.False(BackupCrypto.IsEncrypted(stream));
    }

    /// <summary>
    /// Prüft den vollen Kreislauf: mit Passwort erstellen, Daten verändern, mit Passwort wiederherstellen.
    /// </summary>
    [Fact]
    public async Task CreateAndRestore_WithPassword_ReplacesData()
    {
        await _environment.SeedAsync(Photo);
        var service = _environment.CreateService();
        var created = await service.CreateAsync(Password);
        var before = _environment.Snapshot();
        await using (var context = _environment.CreateFactory().CreateDbContext())
        {
            context.Vehicles.Add(new VehicleEntity { Name = "Opel", FuelKind = "Petrol", CreatedUtc = DateTime.UtcNow });
            await context.SaveChangesAsync();
        }

        Assert.Equal(BackupResult.Ok, await service.RestoreAsync(created.File!.FileName, Password));

        Assert.Equal(["Golf"], await _environment.GetVehicleNamesAsync());
        Assert.Equal(before["db"], _environment.Snapshot()["db"]);
    }

    /// <summary>
    /// Prüft, dass Prüfung und Wiederherstellung ohne Passwort <see cref="BackupResult.PasswordRequired"/> liefern, ohne die Daten anzufassen.
    /// </summary>
    [Fact]
    public async Task Encrypted_MissingPassword_RequiresPassword()
    {
        await _environment.SeedAsync(Photo);
        var service = _environment.CreateService();
        var created = await service.CreateAsync(Password);
        var before = _environment.Snapshot();

        Assert.Equal(BackupResult.PasswordRequired, (await service.InspectAsync(created.File!.FileName)).Result);
        Assert.Equal(BackupResult.PasswordRequired, await service.RestoreAsync(created.File.FileName));

        Assert.Equal(before["db"], _environment.Snapshot()["db"]);
        Assert.Empty(Directory.GetFiles(_environment.BackupDirectory, "*.tmp"));
    }

    /// <summary>
    /// Prüft, dass ein falsches Passwort als <see cref="BackupResult.WrongPassword"/> sichtbar wird und nichts verändert.
    /// </summary>
    [Fact]
    public async Task Encrypted_WrongPassword_FailsWithoutChanges()
    {
        await _environment.SeedAsync(Photo);
        var service = _environment.CreateService();
        var created = await service.CreateAsync(Password);
        var before = _environment.Snapshot();

        Assert.Equal(BackupResult.WrongPassword, (await service.InspectAsync(created.File!.FileName, "falsch")).Result);
        Assert.Equal(BackupResult.WrongPassword, await service.RestoreAsync(created.File.FileName, "falsch"));

        Assert.Equal(before["db"], _environment.Snapshot()["db"]);
        Assert.Empty(Directory.GetFiles(_environment.BackupDirectory, "*.tmp"));
    }

    /// <summary>
    /// Prüft, dass das richtige Passwort die Prüfung mit dem Manifest durchlässt.
    /// </summary>
    [Fact]
    public async Task Inspect_Encrypted_CorrectPassword_ReturnsManifest()
    {
        await _environment.SeedAsync(Photo);
        var service = _environment.CreateService();
        var created = await service.CreateAsync(Password);

        var outcome = await service.InspectAsync(created.File!.FileName, Password);

        Assert.Equal(BackupResult.Ok, outcome.Result);
        Assert.NotNull(outcome.Manifest);
        Assert.Equal(1, outcome.Manifest!.Counts.Vehicles);
        Assert.Equal(1, outcome.Manifest.Counts.Photos);
        Assert.True(outcome.File!.IsEncrypted);
        Assert.Empty(Directory.GetFiles(_environment.BackupDirectory, "*.tmp"));
    }

    /// <summary>
    /// Prüft, dass ein manipulierter Container wie ein falsches Passwort behandelt wird.
    /// </summary>
    [Fact]
    public async Task Encrypted_TamperedContainer_WrongPassword()
    {
        await _environment.SeedAsync(Photo);
        var service = _environment.CreateService();
        var created = await service.CreateAsync(Password);
        BackupEncryptedTestArchive.FlipLastByte(service.GetFullPath(created.File!.FileName)!);
        var before = _environment.Snapshot();

        Assert.Equal(BackupResult.WrongPassword, await service.RestoreAsync(created.File.FileName, Password));
        Assert.Equal(before["db"], _environment.Snapshot()["db"]);
    }

    /// <summary>
    /// Prüft, dass ein gekürzter Container wie ein falsches Passwort behandelt wird.
    /// </summary>
    [Fact]
    public async Task Encrypted_TruncatedContainer_WrongPassword()
    {
        await _environment.SeedAsync(Photo);
        var service = _environment.CreateService();
        var created = await service.CreateAsync(Password);
        var path = service.GetFullPath(created.File!.FileName)!;
        BackupEncryptedTestArchive.Truncate(path, new FileInfo(path).Length - 5);

        Assert.Equal(BackupResult.WrongPassword, await service.RestoreAsync(created.File.FileName, Password));
    }

    /// <summary>
    /// Prüft, dass eine höhere Containerversion als <see cref="BackupResult.TooNew"/> gemeldet wird, noch vor dem Passwort.
    /// </summary>
    [Fact]
    public async Task Encrypted_TooNewContainer_TooNew()
    {
        await _environment.SeedAsync(Photo);
        var service = _environment.CreateService();
        var created = await service.CreateAsync(Password);
        BackupEncryptedTestArchive.WriteVersion(service.GetFullPath(created.File!.FileName)!, 99);

        Assert.Equal(BackupResult.TooNew, (await service.InspectAsync(created.File.FileName)).Result);
        Assert.Equal(BackupResult.TooNew, await service.RestoreAsync(created.File.FileName, Password));
    }

    /// <summary>
    /// Prüft, dass ein Container mit defektem Header in der Liste als nicht lesbar, aber verschlüsselt erscheint.
    /// </summary>
    [Fact]
    public async Task List_BrokenEncryptedHeader_IsMarkedUnreadable()
    {
        await _environment.SeedAsync(Photo);
        var service = _environment.CreateService();
        var created = await service.CreateAsync(Password);
        BackupEncryptedTestArchive.Truncate(service.GetFullPath(created.File!.FileName)!, 20);

        var entry = Assert.Single(await service.ListAsync());

        Assert.True(entry.IsEncrypted);
        Assert.False(entry.IsReadable);
    }

    /// <summary>
    /// Prüft das zweistufige Einspielen: ohne Passwort erst <see cref="BackupResult.PasswordRequired"/>, mit Passwort landet der verschlüsselte Container in der Liste.
    /// </summary>
    [Fact]
    public async Task Import_Encrypted_TwoStep_ImportsContainer()
    {
        await _environment.SeedAsync(Photo);
        var service = _environment.CreateService();
        var created = await service.CreateAsync(Password);
        var external = Path.Combine(Path.GetTempPath(), "import-" + Guid.NewGuid().ToString("N") + ".zip");
        File.Copy(service.GetFullPath(created.File!.FileName)!, external);
        try
        {
            await service.DeleteAsync(created.File.FileName);

            Assert.Equal(BackupResult.PasswordRequired, (await service.ImportAsync(Pick(external))).Result);
            Assert.Empty(await service.ListAsync());
            Assert.Empty(Directory.GetFiles(_environment.BackupDirectory, "*.tmp"));

            var outcome = await service.ImportAsync(Pick(external), Password);

            Assert.Equal(BackupResult.Ok, outcome.Result);
            var entry = Assert.Single(await service.ListAsync());
            Assert.True(entry.IsEncrypted);
            using var stream = File.OpenRead(service.GetFullPath(entry.FileName)!);
            Assert.True(BackupCrypto.IsEncrypted(stream));
            Assert.Empty(Directory.GetFiles(_environment.BackupDirectory, "*.tmp"));
        }
        finally
        {
            File.Delete(external);
        }
    }

    /// <summary>
    /// Prüft, dass ein falsches Passwort beim Einspielen <see cref="BackupResult.WrongPassword"/> liefert und keine Datei hinterlässt.
    /// </summary>
    [Fact]
    public async Task Import_Encrypted_WrongPassword_NoLeftovers()
    {
        await _environment.SeedAsync(Photo);
        var service = _environment.CreateService();
        var created = await service.CreateAsync(Password);
        var external = Path.Combine(Path.GetTempPath(), "import-" + Guid.NewGuid().ToString("N") + ".zip");
        File.Copy(service.GetFullPath(created.File!.FileName)!, external);
        try
        {
            await service.DeleteAsync(created.File.FileName);

            Assert.Equal(BackupResult.WrongPassword, (await service.ImportAsync(Pick(external), "falsch")).Result);
            Assert.Empty(await service.ListAsync());
            Assert.Empty(Directory.GetFiles(_environment.BackupDirectory, "*.tmp"));
        }
        finally
        {
            File.Delete(external);
        }
    }

    /// <summary>
    /// Prüft, dass die Erkennung und das Einspielen auch dann funktionieren, wenn der Strom der Dateiauswahl nicht seekbar ist.
    /// </summary>
    [Fact]
    public async Task Import_Encrypted_NonSeekableStream_ImportsContainer()
    {
        await _environment.SeedAsync(Photo);
        var service = _environment.CreateService();
        var created = await service.CreateAsync(Password);
        var external = Path.Combine(Path.GetTempPath(), "import-" + Guid.NewGuid().ToString("N") + ".zip");
        File.Copy(service.GetFullPath(created.File!.FileName)!, external);
        try
        {
            await service.DeleteAsync(created.File.FileName);

            Assert.Equal(BackupResult.PasswordRequired, (await service.ImportAsync(PickNonSeekable(external))).Result);
            Assert.Empty(await service.ListAsync());
            Assert.Empty(Directory.GetFiles(_environment.BackupDirectory, "*.tmp"));

            var outcome = await service.ImportAsync(PickNonSeekable(external), Password);

            Assert.Equal(BackupResult.Ok, outcome.Result);
            var entry = Assert.Single(await service.ListAsync());
            Assert.True(entry.IsEncrypted);
        }
        finally
        {
            File.Delete(external);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _environment.Dispose();
        GC.SuppressFinalize(this);
    }

    private static PickedBackupFile Pick(string path)
    {
        return new PickedBackupFile(Path.GetFileName(path), () => Task.FromResult<Stream>(File.OpenRead(path)));
    }

    private static PickedBackupFile PickNonSeekable(string path)
    {
        return new PickedBackupFile(Path.GetFileName(path), () => Task.FromResult<Stream>(OpenNonSeekable(path)));
    }

    /// <summary>
    /// Liefert den Dateiinhalt über einen echten nicht-seekbaren Strom (GZip-Dekompression), wie ihn manche Picker bereitstellen.
    /// </summary>
    /// <param name="path">Pfad der zu lesenden Datei.</param>
    /// <returns>Ein lesbarer, nicht seekbarer Strom mit dem Dateiinhalt.</returns>
    private static Stream OpenNonSeekable(string path)
    {
        var compressed = new MemoryStream();
        using (var source = File.OpenRead(path))
        using (var gzip = new GZipStream(compressed, CompressionMode.Compress, leaveOpen: true))
        {
            source.CopyTo(gzip);
        }

        compressed.Position = 0;
        return new GZipStream(compressed, CompressionMode.Decompress);
    }
}

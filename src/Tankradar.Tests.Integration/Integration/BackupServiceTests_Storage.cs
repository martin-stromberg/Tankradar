using System.IO.Compression;
using System.Text;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models.Backup;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft den Zustand der Dateien nach Erstellen und Wiederherstellen: WAL-Modus der Livedatenbank, keine gelöschten Inhalte im Schnappschuss und
/// keine Sicherung, die die Grenzen der Wiederherstellung überschreitet.
/// </summary>
public class BackupServiceTests_Storage : IDisposable
{
    private static readonly byte[] Photo = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

    private readonly BackupTestEnvironment _environment = new();

    private static (byte Write, byte Read) ReadJournalVersionBytes(string databasePath)
    {
        using var stream = new FileStream(databasePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var header = new byte[20];
        stream.ReadExactly(header);
        return (header[18], header[19]);
    }

    /// <summary>
    /// Prüft, dass die Datenbank nach einer Wiederherstellung wieder im WAL-Modus läuft (Kopfbytes 18 und 19 sind 2), wie vor der Wiederherstellung.
    /// </summary>
    [Fact]
    public async Task Restore_LeavesDatabaseInWalMode()
    {
        await _environment.SeedAsync(Photo);
        var databasePath = TankradarDbContext.GetDatabasePath(_environment.DataDirectory);
        Assert.Equal((2, 2), ReadJournalVersionBytes(databasePath));
        var service = _environment.CreateService();
        var created = await service.CreateAsync();

        var result = await service.RestoreAsync(created.File!.FileName);

        Assert.Equal(BackupResult.Ok, result);
        Assert.Equal((2, 2), ReadJournalVersionBytes(databasePath));
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection(TankradarDbContext.BuildConnectionString(databasePath, pooling: false));
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode";
        Assert.Equal("wal", command.ExecuteScalar());
    }

    /// <summary>
    /// Prüft, dass ein gelöschter Datensatz nicht mehr in der Datenbank der Sicherung lesbar bleibt (Schnappschuss per <c>VACUUM INTO</c>).
    /// </summary>
    [Fact]
    public async Task Create_DoesNotContainDeletedContent()
    {
        await _environment.SeedAsync(Photo);
        const string Secret = "GeloeschtesFahrzeug-Geheimnis-4711";
        await using (var context = _environment.CreateFactory().CreateDbContext())
        {
            var vehicle = new VehicleEntity { Name = Secret, FuelKind = "Petrol", CreatedUtc = DateTime.UtcNow };
            context.Vehicles.Add(vehicle);
            await context.SaveChangesAsync();
            context.Vehicles.Remove(vehicle);
            await context.SaveChangesAsync();
        }

        var live = File.ReadAllBytes(TankradarDbContext.GetDatabasePath(_environment.DataDirectory));
        Assert.True(Contains(live, Secret), "Voraussetzung: Der gelöschte Inhalt liegt noch in der Livedatei.");

        var created = await _environment.CreateService().CreateAsync();

        using var archive = ZipFile.OpenRead(Path.Combine(_environment.BackupDirectory, created.File!.FileName));
        using var memory = new MemoryStream();
        using (var stream = archive.GetEntry("database/tankatlas.db")!.Open())
        {
            await stream.CopyToAsync(memory);
        }

        Assert.False(Contains(memory.ToArray(), Secret));
    }

    /// <summary>
    /// Prüft, dass die Erstellung keine Sicherung ablegt, die die Wiederherstellung wegen der Größengrenze eines Belegfotos ablehnen würde.
    /// </summary>
    [Fact]
    public async Task Create_PhotoOverRestoreLimit_FailsWithoutLeavingFiles()
    {
        await _environment.SeedAsync(Photo);
        var huge = new byte[(int)Tankradar.MAUI.Models.Tankbook.TankbookEntryLimits.MaxReceiptPhotoBytes + 1];
        huge[0] = 0xFF;
        huge[1] = 0xD8;
        huge[2] = 0xFF;
        await File.WriteAllBytesAsync(Path.Combine(_environment.ReceiptsDirectory, "fedcba9876543210fedcba9876543210.jpg"), huge);
        var service = _environment.CreateService();

        var outcome = await service.CreateAsync();

        Assert.Equal(BackupResult.Failed, outcome.Result);
        Assert.Empty(await service.ListAsync());
        Assert.Empty(Directory.GetFileSystemEntries(_environment.BackupDirectory));
    }

    private static bool Contains(byte[] data, string text)
    {
        return data.AsSpan().IndexOf(Encoding.UTF8.GetBytes(text)) >= 0;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _environment.Dispose();
        GC.SuppressFinalize(this);
    }
}

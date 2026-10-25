using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Services.Backup;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft die Robustheit der Wiederherstellung: bestehendes Journal vor einer neuen Wiederherstellung, fehlgeschlagenes Zurücknehmen,
/// kurzzeitig gehaltene Datenbankdatei und eine nicht lesbare Datei in der Liste. Die Sperren-Tests gelten nur unter Windows (dort erzwingt das Betriebssystem Dateisperren).
/// </summary>
[Collection(BackupCollection.Name)]
public class BackupServiceTests_Resilience : IDisposable
{
    private static readonly byte[] Photo = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

    private readonly BackupTestEnvironment _environment = new();

    private string RollbackPath()
    {
        return Path.Combine(_environment.DataDirectory, BackupFormat.RollbackDirectoryName);
    }

    private async Task<(BackupService Service, string Name)> SeedBackupAndAddVehicleAsync()
    {
        await _environment.SeedAsync(Photo);
        var service = _environment.CreateService();
        var created = await service.CreateAsync();
        await using (var context = _environment.CreateFactory().CreateDbContext())
        {
            context.Vehicles.Add(new VehicleEntity { Name = "Opel", FuelKind = "Petrol", CreatedUtc = DateTime.UtcNow });
            await context.SaveChangesAsync();
        }

        return (service, created.File!.FileName);
    }

    private void CreateInterruptedSwap()
    {
        var databasePath = TankradarDbContext.GetDatabasePath(_environment.DataDirectory);
        Directory.CreateDirectory(RollbackPath());
        RestoreJournal.Write(_environment.DataDirectory, new RestoreJournal { HadDatabase = true, HadReceipts = true });
        File.Move(databasePath, Path.Combine(RollbackPath(), "tankatlas.db"));
        Directory.Move(_environment.ReceiptsDirectory, Path.Combine(RollbackPath(), "receipts"));
        File.WriteAllText(databasePath, "halb geschriebene neue Datenbank");
    }

    /// <summary>
    /// Prüft, dass eine Wiederherstellung bei vorhandenem Journal zuerst den alten Stand zurückholt (statt die Rücksicherung zu löschen) und dann erfolgreich ist.
    /// </summary>
    [Fact]
    public async Task Restore_WithLeftoverJournal_UndoesFirstThenSucceeds()
    {
        var (service, name) = await SeedBackupAndAddVehicleAsync();
        CreateInterruptedSwap();

        var result = await service.RestoreAsync(name);

        Assert.Equal(BackupResult.Ok, result);
        Assert.Equal(["Golf"], await _environment.GetVehicleNamesAsync());
        Assert.False(RestoreJournal.Exists(_environment.DataDirectory));
        Assert.False(Directory.Exists(RollbackPath()));
        Assert.True(File.Exists(Path.Combine(_environment.ReceiptsDirectory, BackupTestEnvironment.PhotoName)));
    }

    /// <summary>
    /// Prüft, dass eine zweite Wiederherstellung nach fehlgeschlagenem Zurücknehmen abbricht, die Rücksicherung samt Journal behält und der nächste Start den alten Stand zurückholt.
    /// </summary>
    [Fact]
    public async Task Restore_WhenUndoFails_KeepsRollbackAndJournal()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var (service, name) = await SeedBackupAndAddVehicleAsync();
        var before = _environment.Snapshot();
        CreateInterruptedSwap();
        var saved = Path.Combine(RollbackPath(), "tankatlas.db");

        BackupResult result;
        using (new FileStream(saved, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            result = await service.RestoreAsync(name);

            Assert.Equal(BackupResult.Failed, result);
            Assert.True(RestoreJournal.Exists(_environment.DataDirectory));
            Assert.True(File.Exists(saved));
        }

        new RestoreRecovery().RecoverIfNeeded(_environment.DataDirectory);

        Assert.Equal(before, _environment.Snapshot());
        Assert.Equal(["Golf", "Opel"], await _environment.GetVehicleNamesAsync());
        Assert.False(RestoreJournal.Exists(_environment.DataDirectory));
        Assert.False(Directory.Exists(RollbackPath()));
    }

    /// <summary>
    /// Prüft, dass der Start mit einem nicht zurücknehmbaren Austausch klar scheitert (keine Migration auf dem Zwischenstand) und die Rücksicherung für den nächsten Start behält.
    /// </summary>
    [Fact]
    public async Task Recovery_WhenUndoFails_ThrowsAndKeepsRollback()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var (_, _) = await SeedBackupAndAddVehicleAsync();
        CreateInterruptedSwap();
        var saved = Path.Combine(RollbackPath(), "tankatlas.db");

        using (new FileStream(saved, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var exception = Record.Exception(() => new RestoreRecovery().RecoverIfNeeded(_environment.DataDirectory));

            Assert.IsType<IOException>(exception);
            Assert.True(RestoreJournal.Exists(_environment.DataDirectory));
            Assert.True(File.Exists(saved));
        }

        new RestoreRecovery().RecoverIfNeeded(_environment.DataDirectory);

        Assert.Equal(["Golf", "Opel"], await _environment.GetVehicleNamesAsync());
        Assert.False(RestoreJournal.Exists(_environment.DataDirectory));
    }

    /// <summary>
    /// Prüft, dass ein Undo ohne lesbares Journal den lebenden Bestand nie löscht.
    /// </summary>
    [Fact]
    public async Task Undo_WithUnreadableJournal_KeepsLiveData()
    {
        var (_, _) = await SeedBackupAndAddVehicleAsync();
        var before = _environment.Snapshot();
        File.WriteAllText(RestoreJournal.GetPath(_environment.DataDirectory), "kein json");

        new RestoreRecovery().RecoverIfNeeded(_environment.DataDirectory);

        Assert.Equal(before, _environment.Snapshot());
        Assert.Equal(["Golf", "Opel"], await _environment.GetVehicleNamesAsync());
    }

    /// <summary>
    /// Prüft, dass ein Undo ohne lesbares Journal die Rücksicherung zurückgibt, ohne zusätzlich etwas zu löschen.
    /// </summary>
    [Fact]
    public async Task Undo_WithUnreadableJournal_ReturnsRollbackOnly()
    {
        var (_, _) = await SeedBackupAndAddVehicleAsync();
        CreateInterruptedSwap();
        File.WriteAllText(RestoreJournal.GetPath(_environment.DataDirectory), "kein json");

        new RestoreRecovery().RecoverIfNeeded(_environment.DataDirectory);

        Assert.Equal(["Golf", "Opel"], await _environment.GetVehicleNamesAsync());
        Assert.True(File.Exists(Path.Combine(_environment.ReceiptsDirectory, BackupTestEnvironment.PhotoName)));
        Assert.False(RestoreJournal.Exists(_environment.DataDirectory));
    }

    /// <summary>
    /// Prüft, dass ein Fehler beim Schreiben des Journals den Bestand unverändert lässt (kein Undo mit Löschen).
    /// </summary>
    [Fact]
    public async Task Restore_WhenJournalWriteFails_KeepsLiveData()
    {
        var (service, name) = await SeedBackupAndAddVehicleAsync();
        var before = _environment.Snapshot();
        Directory.CreateDirectory(RestoreJournal.GetPath(_environment.DataDirectory) + BackupFormat.TempExtension);

        var result = await service.RestoreAsync(name);

        Assert.Equal(BackupResult.Failed, result);
        Assert.Equal(before, _environment.Snapshot());
        Assert.Equal(["Golf", "Opel"], await _environment.GetVehicleNamesAsync());
        Assert.False(RestoreJournal.Exists(_environment.DataDirectory));
    }

    /// <summary>
    /// Prüft, dass eine kurzzeitig gehaltene Datenbankdatei den Austausch nicht scheitern lässt, weil das Verschieben wiederholt wird.
    /// </summary>
    [Fact]
    public async Task Restore_DatabaseBrieflyLocked_SucceedsByRetry()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        await _environment.SeedAsync(Photo);
        var created = await _environment.CreateService().CreateAsync();
        await using (var context = _environment.CreateFactory().CreateDbContext())
        {
            context.Vehicles.Add(new VehicleEntity { Name = "Opel", FuelKind = "Petrol", CreatedUtc = DateTime.UtcNow });
            await context.SaveChangesAsync();
        }

        var databasePath = TankradarDbContext.GetDatabasePath(_environment.DataDirectory);
        var service = _environment.CreateService(step =>
        {
            if (step == RestoreSwap.StepJournal)
            {
                var held = new FileStream(databasePath, FileMode.Open, FileAccess.Read, FileShare.None);
                _ = Task.Run(async () =>
                {
                    await Task.Delay(250);
                    await held.DisposeAsync();
                });
            }
        });

        var result = await service.RestoreAsync(created.File!.FileName);

        Assert.Equal(BackupResult.Ok, result);
        Assert.Equal(["Golf"], await _environment.GetVehicleNamesAsync());
    }

    /// <summary>
    /// Prüft, dass eine Wiederherstellung gelingt, wenn eine gepoolte Verbindung noch -wal/-shm hält und diese erst beim Leeren der Pools verschwinden.
    /// </summary>
    [Fact]
    public async Task Restore_WithPooledConnectionHoldingWal_Succeeds()
    {
        await _environment.SeedAsync(Photo);
        var created = await _environment.CreateService().CreateAsync();
        var databasePath = TankradarDbContext.GetDatabasePath(_environment.DataDirectory);
        await using (var context = _environment.CreateFactory().CreateDbContext())
        {
            context.Vehicles.Add(new VehicleEntity { Name = "Opel", FuelKind = "Petrol", CreatedUtc = DateTime.UtcNow });
            await context.SaveChangesAsync();
        }

        using (var pooled = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=" + databasePath + ";Pooling=True"))
        {
            pooled.Open();
            using var command = pooled.CreateCommand();
            command.CommandText = "PRAGMA journal_mode=WAL; CREATE TABLE IF NOT EXISTS WalProbe (Id INTEGER); INSERT INTO WalProbe VALUES (1);";
            command.ExecuteNonQuery();
        }

        Assert.True(File.Exists(databasePath + "-wal"));
        var service = _environment.CreateService();
        var result = await service.RestoreAsync(created.File!.FileName);

        Assert.Equal(BackupResult.Ok, result);
        Assert.Equal(["Golf"], await _environment.GetVehicleNamesAsync());
        Assert.True(File.Exists(databasePath));
    }

    /// <summary>
    /// Prüft, dass eine gesperrte Sicherungsdatei die Liste nicht verhindert.
    /// </summary>
    [Fact]
    public async Task List_WithLockedFile_StillListsAllFiles()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        await _environment.SeedAsync(Photo);
        var service = _environment.CreateService();
        var first = await service.CreateAsync();
        var second = await service.CreateAsync();

        using (new FileStream(Path.Combine(_environment.BackupDirectory, first.File!.FileName), FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var list = await service.ListAsync();

            Assert.Equal(2, list.Count);
            Assert.Contains(list, info => info.FileName == second.File!.FileName && info.IsReadable);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _environment.Dispose();
        GC.SuppressFinalize(this);
    }
}

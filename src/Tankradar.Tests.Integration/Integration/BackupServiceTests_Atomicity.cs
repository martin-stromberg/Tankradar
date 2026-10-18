using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Services.Backup;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft die Atomarität der Wiederherstellung: Bei einem Fehler an jedem Schritt des Austauschs ist der alte Stand vollständig wiederhergestellt,
/// und nach einem simulierten Absturz stellt die Wiederaufnahme beim Start den alten Stand her.
/// </summary>
public class BackupServiceTests_Atomicity : IDisposable
{
    private static readonly byte[] Photo = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

    private readonly BackupTestEnvironment _environment = new();

    private async Task<string> SeedAndBackupAsync()
    {
        await _environment.SeedAsync(Photo);
        var created = await _environment.CreateService().CreateAsync();
        await using (var context = _environment.CreateFactory().CreateDbContext())
        {
            context.Vehicles.Add(new VehicleEntity { Name = "Opel", FuelKind = "Petrol", CreatedUtc = DateTime.UtcNow });
            await context.SaveChangesAsync();
        }

        await File.WriteAllBytesAsync(Path.Combine(_environment.ReceiptsDirectory, "fedcba9876543210fedcba9876543210.jpg"), [0xFF, 0xD8, 0xFF, 7]);
        return created.File!.FileName;
    }

    /// <summary>
    /// Prüft, dass ein Fehler nach jedem Schritt des Austauschs den bisherigen Stand byte-genau zurückgibt und nichts zurückbleibt.
    /// </summary>
    /// <param name="failingStep">Der Schritt, nach dem die Fehlerinjektion eine Ausnahme wirft.</param>
    [Theory]
    [InlineData(RestoreSwap.StepJournal)]
    [InlineData(RestoreSwap.StepDatabaseMoved)]
    [InlineData(RestoreSwap.StepReceiptsMoved)]
    [InlineData(RestoreSwap.StepDatabasePlaced)]
    [InlineData(RestoreSwap.StepReceiptsPlaced)]
    [InlineData(RestoreSwap.StepProtected)]
    [InlineData(RestoreSwap.StepBeforeCommit)]
    public async Task Restore_FailureAtStep_RestoresOldStateCompletely(string failingStep)
    {
        var name = await SeedAndBackupAsync();
        var before = _environment.Snapshot();
        var service = _environment.CreateService(step =>
        {
            if (step == failingStep)
            {
                throw new IOException("Fehlerinjektion");
            }
        });

        var result = await service.RestoreAsync(name);

        Assert.Equal(BackupResult.Failed, result);
        Assert.Equal(before, _environment.Snapshot());
        Assert.Equal(["Golf", "Opel"], await _environment.GetVehicleNamesAsync());
        Assert.False(File.Exists(Path.Combine(_environment.DataDirectory, BackupFormat.JournalFileName)));
        Assert.False(Directory.Exists(Path.Combine(_environment.DataDirectory, BackupFormat.StagingDirectoryName)));
        Assert.False(Directory.Exists(Path.Combine(_environment.DataDirectory, BackupFormat.RollbackDirectoryName)));
        Assert.Equal(0, _environment.CacheInvalidations);
    }

    /// <summary>
    /// Prüft, dass nach einem simulierten Absturz mitten im Austausch (Journal bleibt bestehen) die Wiederaufnahme den alten Stand wiederherstellt.
    /// </summary>
    [Fact]
    public async Task Crash_DuringSwap_IsRecoveredAtNextStart()
    {
        var name = await SeedAndBackupAsync();
        var before = _environment.Snapshot();
        // Ein Prozessabbruch lässt sich nicht über eine Ausnahme simulieren (der Rollback würde greifen); deshalb wird der halbe Austausch mit Journal von Hand erzeugt.
        var databasePath = TankradarDbContext.GetDatabasePath(_environment.DataDirectory);
        var rollback = Path.Combine(_environment.DataDirectory, BackupFormat.RollbackDirectoryName);
        Directory.CreateDirectory(rollback);
        RestoreJournal.Write(_environment.DataDirectory, new RestoreJournal { HadDatabase = true, HadReceipts = true });
        File.Move(databasePath, Path.Combine(rollback, "tankatlas.db"));
        Directory.Move(_environment.ReceiptsDirectory, Path.Combine(rollback, "receipts"));
        File.WriteAllText(databasePath, "halb geschriebene neue Datenbank");
        Assert.Equal("halb geschriebene neue Datenbank", File.ReadAllText(databasePath));

        new RestoreRecovery().RecoverIfNeeded(_environment.DataDirectory);

        Assert.Equal(before, _environment.Snapshot());
        Assert.Equal(["Golf", "Opel"], await _environment.GetVehicleNamesAsync());
        Assert.False(File.Exists(Path.Combine(_environment.DataDirectory, BackupFormat.JournalFileName)));
        Assert.False(Directory.Exists(rollback));
        Assert.Single(Directory.GetFiles(_environment.BackupDirectory, "*.zip"));
        Assert.True(File.Exists(Path.Combine(_environment.BackupDirectory, name)));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _environment.Dispose();
        GC.SuppressFinalize(this);
    }
}

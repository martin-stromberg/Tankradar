using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Services.Backup;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Reihenfolge von <see cref="RestoreSwap.Undo"/> bei einem Doppelfehler: Eine bereits zurückgegebene -wal darf vom zweiten Durchlauf nicht gelöscht werden.
/// </summary>
public class RestoreSwapUndoTests : BaseTest
{
    private readonly TempDirectory _temp = new();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _temp.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Prüft, dass ein Undo, das nach der Hauptdatei an der -wal scheitert, beim zweiten Durchlauf alte Hauptdatei und alte -wal vollständig zurückgibt.
    /// </summary>
    [Fact]
    public void Undo_FailingBetweenMainAndWal_IsCompletedBySecondRun()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var rollback = _temp.Combine(BackupFormat.RollbackDirectoryName);
        Directory.CreateDirectory(rollback);
        File.WriteAllText(Path.Combine(rollback, "tankatlas.db"), "alt-db");
        File.WriteAllText(Path.Combine(rollback, "tankatlas.db-wal"), "alt-wal");
        File.WriteAllText(_temp.Combine("tankatlas.db"), "neu-db");
        RestoreJournal.Write(_temp.Path, new RestoreJournal { HadDatabase = true, HadReceipts = false });

        using (new FileStream(Path.Combine(rollback, "tankatlas.db-wal"), FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.ThrowsAny<IOException>(() => RestoreSwap.Undo(_temp.Path));
            Assert.Equal("alt-db", File.ReadAllText(_temp.Combine("tankatlas.db")));
        }

        RestoreSwap.Undo(_temp.Path);

        Assert.Equal("alt-db", File.ReadAllText(_temp.Combine("tankatlas.db")));
        Assert.Equal("alt-wal", File.ReadAllText(_temp.Combine("tankatlas.db-wal")));
        Assert.False(RestoreJournal.Exists(_temp.Path));
        Assert.False(Directory.Exists(rollback));
    }
}

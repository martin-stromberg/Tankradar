using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Services.Backup;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Journal und Wiederaufnahme nach einer unterbrochenen Wiederherstellung auf einem Temp-Verzeichnis.
/// </summary>
public class RestoreRecoveryTests : BaseTest
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

    private string Db()
    {
        return _temp.Combine("tankatlas.db");
    }

    /// <summary>
    /// Prüft Schreiben, Lesen, Existenz und Löschen des Journals.
    /// </summary>
    [Fact]
    public void Journal_WriteReadDelete()
    {
        Assert.False(RestoreJournal.Exists(_temp.Path));
        Assert.Null(RestoreJournal.Read(_temp.Path));

        RestoreJournal.Write(_temp.Path, new RestoreJournal { HadDatabase = true, HadReceipts = false });

        Assert.True(RestoreJournal.Exists(_temp.Path));
        var read = RestoreJournal.Read(_temp.Path)!;
        Assert.True(read.HadDatabase);
        Assert.False(read.HadReceipts);

        RestoreJournal.Delete(_temp.Path);
        Assert.False(RestoreJournal.Exists(_temp.Path));
    }

    /// <summary>
    /// Prüft, dass ein beschädigtes Journal als nicht lesbar gilt, aber weiter als vorhanden erkannt wird.
    /// </summary>
    [Fact]
    public void Journal_Corrupt_ReadsNull()
    {
        File.WriteAllText(RestoreJournal.GetPath(_temp.Path), "kein json");

        Assert.True(RestoreJournal.Exists(_temp.Path));
        Assert.Null(RestoreJournal.Read(_temp.Path));
    }

    /// <summary>
    /// Prüft, dass ohne Journal die Livedaten unangetastet bleiben und nur verwaiste Verzeichnisse entfernt werden.
    /// </summary>
    [Fact]
    public void Recover_WithoutJournal_RemovesOnlyLeftovers()
    {
        File.WriteAllText(Db(), "live");
        Directory.CreateDirectory(_temp.Combine("receipts"));
        File.WriteAllText(_temp.Combine("receipts", "a.jpg"), "foto");
        Directory.CreateDirectory(_temp.Combine(BackupFormat.StagingDirectoryName, "x"));
        Directory.CreateDirectory(_temp.Combine(BackupFormat.RollbackDirectoryName));
        File.WriteAllText(_temp.Combine(BackupFormat.RollbackDirectoryName, "tankatlas.db"), "alt");

        new RestoreRecovery().RecoverIfNeeded(_temp.Path);

        Assert.Equal("live", File.ReadAllText(Db()));
        Assert.True(File.Exists(_temp.Combine("receipts", "a.jpg")));
        Assert.False(Directory.Exists(_temp.Combine(BackupFormat.StagingDirectoryName)));
        Assert.False(Directory.Exists(_temp.Combine(BackupFormat.RollbackDirectoryName)));
    }

    /// <summary>
    /// Prüft, dass mit Journal die Rücksicherung zurückkommt und die neu platzierten Dateien entfernt werden.
    /// </summary>
    [Fact]
    public void Recover_WithJournal_RestoresPreviousState()
    {
        RestoreJournal.Write(_temp.Path, new RestoreJournal { HadDatabase = true, HadReceipts = true });
        Directory.CreateDirectory(_temp.Combine(BackupFormat.RollbackDirectoryName, "receipts"));
        File.WriteAllText(_temp.Combine(BackupFormat.RollbackDirectoryName, "tankatlas.db"), "alt");
        File.WriteAllText(_temp.Combine(BackupFormat.RollbackDirectoryName, "tankatlas.db-wal"), "alt-wal");
        File.WriteAllText(_temp.Combine(BackupFormat.RollbackDirectoryName, "receipts", "alt.jpg"), "altfoto");
        File.WriteAllText(Db(), "neu");
        Directory.CreateDirectory(_temp.Combine("receipts"));
        File.WriteAllText(_temp.Combine("receipts", "neu.jpg"), "neufoto");

        new RestoreRecovery().RecoverIfNeeded(_temp.Path);

        Assert.Equal("alt", File.ReadAllText(Db()));
        Assert.Equal("alt-wal", File.ReadAllText(Db() + "-wal"));
        Assert.True(File.Exists(_temp.Combine("receipts", "alt.jpg")));
        Assert.False(File.Exists(_temp.Combine("receipts", "neu.jpg")));
        Assert.False(RestoreJournal.Exists(_temp.Path));
        Assert.False(Directory.Exists(_temp.Combine(BackupFormat.RollbackDirectoryName)));
    }

    /// <summary>
    /// Prüft, dass ein Abbruch vor dem Verschieben (Journal ohne Rücksicherung) die noch vorhandenen Originale behält.
    /// </summary>
    [Fact]
    public void Recover_JournalBeforeAnyMove_KeepsOriginals()
    {
        RestoreJournal.Write(_temp.Path, new RestoreJournal { HadDatabase = true, HadReceipts = true });
        Directory.CreateDirectory(_temp.Combine(BackupFormat.RollbackDirectoryName));
        File.WriteAllText(Db(), "original");
        Directory.CreateDirectory(_temp.Combine("receipts"));
        File.WriteAllText(_temp.Combine("receipts", "o.jpg"), "foto");

        new RestoreRecovery().RecoverIfNeeded(_temp.Path);

        Assert.Equal("original", File.ReadAllText(Db()));
        Assert.True(File.Exists(_temp.Combine("receipts", "o.jpg")));
        Assert.False(RestoreJournal.Exists(_temp.Path));
    }

    /// <summary>
    /// Prüft, dass bei einer zuvor leeren Installation die neu platzierten Dateien wieder entfernt werden.
    /// </summary>
    [Fact]
    public void Recover_NoPreviousData_RemovesNewFiles()
    {
        RestoreJournal.Write(_temp.Path, new RestoreJournal { HadDatabase = false, HadReceipts = false });
        File.WriteAllText(Db(), "neu");
        Directory.CreateDirectory(_temp.Combine("receipts"));
        File.WriteAllText(_temp.Combine("receipts", "neu.jpg"), "foto");

        new RestoreRecovery().RecoverIfNeeded(_temp.Path);

        Assert.False(File.Exists(Db()));
        Assert.False(Directory.Exists(_temp.Combine("receipts")));
    }

    /// <summary>
    /// Prüft, dass ein nicht vorhandenes Datenverzeichnis ohne Fehler bleibt.
    /// </summary>
    [Fact]
    public void Recover_MissingDirectory_DoesNothing()
    {
        new RestoreRecovery().RecoverIfNeeded(_temp.Combine("fehlt"));

        Assert.False(Directory.Exists(_temp.Combine("fehlt")));
    }
}

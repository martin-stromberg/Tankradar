using Microsoft.Data.Sqlite;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.MAUI.Services.Backup;

/// <summary>
/// Tauscht Datenbank und Belegfotos des Datenverzeichnisses gegen den vorbereiteten Stand im Staging-Verzeichnis aus. Der Austausch besteht nur aus Umbenennungen
/// innerhalb eines Volumens; der bisherige Stand wird bis zum Commit (Löschen des Journals) in <c>restore-rollback</c> aufbewahrt und bei jedem Fehler zurückgegeben.
/// </summary>
public sealed class RestoreSwap
{
    /// <summary>
    /// Schritt: Journal geschrieben.
    /// </summary>
    public const string StepJournal = "journal";

    /// <summary>
    /// Schritt: bisherige Datenbank in die Rücksicherung verschoben.
    /// </summary>
    public const string StepDatabaseMoved = "database-moved";

    /// <summary>
    /// Schritt: bisherige Belegfotos in die Rücksicherung verschoben.
    /// </summary>
    public const string StepReceiptsMoved = "receipts-moved";

    /// <summary>
    /// Schritt: neue Datenbank an ihren Platz verschoben.
    /// </summary>
    public const string StepDatabasePlaced = "database-placed";

    /// <summary>
    /// Schritt: neue Belegfotos an ihren Platz verschoben.
    /// </summary>
    public const string StepReceiptsPlaced = "receipts-placed";

    /// <summary>
    /// Schritt: Dateischutz gesetzt.
    /// </summary>
    public const string StepProtected = "protected";

    /// <summary>
    /// Schritt: unmittelbar vor dem Commit.
    /// </summary>
    public const string StepBeforeCommit = "before-commit";

    private const int MoveAttempts = 5;
    private const int MoveRetryDelayMilliseconds = 100;

    private static readonly string[] DatabaseSuffixes = ["-wal", "-shm", string.Empty];

    private readonly string _dataDirectory;
    private readonly IDatabaseFileProtector _protector;
    private readonly Action<string>? _afterStep;

    /// <summary>
    /// Erstellt den Austausch.
    /// </summary>
    /// <param name="dataDirectory">Das Datenverzeichnis.</param>
    /// <param name="protector">Setzt den Dateischutz auf die neuen Dateien.</param>
    /// <param name="afterStep">Wird nach jedem Schritt mit dem Schrittnamen aufgerufen; eine Ausnahme löst den Rollback aus (Fehlerinjektion für Tests).</param>
    public RestoreSwap(string dataDirectory, IDatabaseFileProtector protector, Action<string>? afterStep = null)
    {
        ArgumentNullException.ThrowIfNull(protector);
        _dataDirectory = dataDirectory;
        _protector = protector;
        _afterStep = afterStep;
    }

    /// <summary>
    /// Führt den Austausch aus. Bei einer Ausnahme ist der bisherige Stand wiederhergestellt (soweit möglich; sonst holt ihn der nächste Start über das Journal zurück).
    /// </summary>
    /// <param name="stagingDirectory">Das Staging-Verzeichnis mit <c>tankatlas.db</c> und optional <c>receipts</c>.</param>
    public void Execute(string stagingDirectory)
    {
        var databasePath = TankradarDbContext.GetDatabasePath(_dataDirectory);
        var receiptsPath = Path.Combine(_dataDirectory, ReceiptPhotoStore.DirectoryName);
        var rollbackPath = Path.Combine(_dataDirectory, BackupFormat.RollbackDirectoryName);
        var rollbackReceipts = Path.Combine(rollbackPath, ReceiptPhotoStore.DirectoryName);

        if (RestoreJournal.Exists(_dataDirectory))
        {
            // Ein früherer Austausch wurde nicht abgeschlossen und konnte nicht zurückgenommen werden: Die Rücksicherung ist dann die einzige Kopie des alten Stands.
            // Sie darf nicht gelöscht werden; scheitert auch dieses Zurücknehmen, bricht die Wiederherstellung ab, und Journal und Rücksicherung bleiben erhalten.
            Undo(_dataDirectory);
        }

        DeleteDirectoryIfExists(rollbackPath);
        Directory.CreateDirectory(rollbackPath);
        try
        {
            RestoreJournal.Write(_dataDirectory, new RestoreJournal { HadDatabase = File.Exists(databasePath), HadReceipts = Directory.Exists(receiptsPath) });
            Step(StepJournal);

            foreach (var suffix in DatabaseSuffixes)
            {
                if (File.Exists(databasePath + suffix))
                {
                    var source = databasePath + suffix;
                    var target = Path.Combine(rollbackPath, TankradarDbContext.DatabaseFileName + suffix);
                    MoveWithRetry(() => File.Move(source, target));
                }
            }

            Step(StepDatabaseMoved);

            if (Directory.Exists(receiptsPath))
            {
                MoveWithRetry(() => Directory.Move(receiptsPath, rollbackReceipts));
            }

            Step(StepReceiptsMoved);

            MoveWithRetry(() => File.Move(Path.Combine(stagingDirectory, TankradarDbContext.DatabaseFileName), databasePath));
            Step(StepDatabasePlaced);

            var stagedReceipts = Path.Combine(stagingDirectory, ReceiptPhotoStore.DirectoryName);
            if (Directory.Exists(stagedReceipts))
            {
                MoveWithRetry(() => Directory.Move(stagedReceipts, receiptsPath));
            }

            Step(StepReceiptsPlaced);

            _protector.Protect(databasePath);
            if (Directory.Exists(receiptsPath))
            {
                foreach (var photo in Directory.EnumerateFiles(receiptsPath))
                {
                    _protector.ProtectFile(photo);
                }
            }

            Step(StepProtected);
            Step(StepBeforeCommit);
        }
        catch
        {
            TryUndo(_dataDirectory);
            throw;
        }

        RestoreJournal.Delete(_dataDirectory);
        BackupFileCleanup.DeleteDirectory(rollbackPath);
        BackupFileCleanup.DeleteDirectory(stagingDirectory);
    }

    /// <summary>
    /// Nimmt einen unterbrochenen oder fehlgeschlagenen Austausch zurück: Dateien aus der Rücksicherung kommen an ihren Platz zurück, neu platzierte Dateien werden entfernt.
    /// </summary>
    /// <param name="dataDirectory">Das Datenverzeichnis.</param>
    public static void Undo(string dataDirectory)
    {
        SqliteConnection.ClearAllPools();
        var journal = RestoreJournal.Read(dataDirectory);
        var databasePath = TankradarDbContext.GetDatabasePath(dataDirectory);
        var receiptsPath = Path.Combine(dataDirectory, ReceiptPhotoStore.DirectoryName);
        var rollbackPath = Path.Combine(dataDirectory, BackupFormat.RollbackDirectoryName);
        var rollbackReceipts = Path.Combine(rollbackPath, ReceiptPhotoStore.DirectoryName);

        var mainMoved = File.Exists(Path.Combine(rollbackPath, TankradarDbContext.DatabaseFileName));
        var receiptsMoved = Directory.Exists(rollbackReceipts);
        var hadDatabase = journal?.HadDatabase ?? mainMoved;
        var hadReceipts = journal?.HadReceipts ?? receiptsMoved;

        foreach (var suffix in DatabaseSuffixes)
        {
            var saved = Path.Combine(rollbackPath, TankradarDbContext.DatabaseFileName + suffix);
            if (File.Exists(saved))
            {
                File.Delete(databasePath + suffix);
                File.Move(saved, databasePath + suffix);
            }
            else if (mainMoved || !hadDatabase)
            {
                File.Delete(databasePath + suffix);
            }
        }

        if (receiptsMoved)
        {
            DeleteDirectoryIfExists(receiptsPath);
            Directory.Move(rollbackReceipts, receiptsPath);
        }
        else if (!hadReceipts)
        {
            DeleteDirectoryIfExists(receiptsPath);
        }

        RestoreJournal.Delete(dataDirectory);
        BackupFileCleanup.DeleteDirectory(rollbackPath);
    }

    private static void TryUndo(string dataDirectory)
    {
        try
        {
            Undo(dataDirectory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Das Journal bleibt bestehen; der nächste Start nimmt den Austausch zurück.
        }
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    /// <summary>
    /// Führt eine Verschiebung aus und wiederholt sie kurz, wenn eine parallel geöffnete Datenbankverbindung die Datei noch festhält. Vor jedem Versuch werden die
    /// Verbindungspools geleert. Ein darüber hinaus bestehender paralleler Zugriff führt zum Rollback (Ergebnis <c>Failed</c>); siehe ADR 0011.
    /// </summary>
    /// <param name="move">Die Verschiebung.</param>
    private static void MoveWithRetry(Action move)
    {
        for (var attempt = 1; ; attempt++)
        {
            SqliteConnection.ClearAllPools();
            try
            {
                move();
                return;
            }
            catch (Exception ex) when ((ex is IOException or UnauthorizedAccessException) && attempt < MoveAttempts)
            {
                Thread.Sleep(MoveRetryDelayMilliseconds);
            }
        }
    }

    private void Step(string name)
    {
        _afterStep?.Invoke(name);
    }
}

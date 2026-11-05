using System.IO.Compression;
using Microsoft.Data.Sqlite;

namespace Tankradar.TestSupport;

/// <summary>
/// Die Art der Manipulation an der Datenbank einer Sicherung.
/// </summary>
public enum BackupTamper
{
    /// <summary>
    /// Zusätzlicher Trigger, der beim Anlegen eines Fahrzeugs alle Vorgänge löscht.
    /// </summary>
    Trigger,

    /// <summary>
    /// Wie <see cref="Trigger"/>, aber mit dem für SQLite reservierten Namenspräfix <c>sqlite_</c>.
    /// </summary>
    PrefixedTrigger,

    /// <summary>
    /// Zusätzliche View.
    /// </summary>
    View,

    /// <summary>
    /// Zusätzliche <c>CHECK</c>-Einschränkung an <c>Vehicles</c>, die das nächste neue Fahrzeug scheitern lässt.
    /// </summary>
    ExtraCheck,

    /// <summary>
    /// Autoinkrement-Zähler von <c>Vehicles</c> auf den Höchstwert gesetzt.
    /// </summary>
    ManipulatedSequence,

    /// <summary>
    /// Eine Zeile in der Migrationssperre (ließ früher die Migration endlos warten).
    /// </summary>
    MigrationLockRow,

    /// <summary>
    /// Die Kollation <c>NOCASE</c> ist aus dem Tabellentext von <c>Vehicles</c> entfernt.
    /// </summary>
    RemovedCollation,

    /// <summary>
    /// Eine Tabelle der App hat eine zusätzliche Spalte.
    /// </summary>
    ExtraColumn,

    /// <summary>
    /// Zusätzliche, fremde Tabelle.
    /// </summary>
    ExtraTable,

    /// <summary>
    /// Zusätzlicher Index auf einer Tabelle der App.
    /// </summary>
    ExtraIndex,

    /// <summary>
    /// Eine Tabelle der App fehlt.
    /// </summary>
    MissingTable,

    /// <summary>
    /// Eine Spalte einer Tabelle der App fehlt.
    /// </summary>
    MissingColumn,

    /// <summary>
    /// Ungültiger Datumswert in <c>Vehicles.CreatedUtc</c>.
    /// </summary>
    InvalidDate,

    /// <summary>
    /// Unbekannte Kraftstoffart eines Fahrzeugs.
    /// </summary>
    UnknownFuelKind,

    /// <summary>
    /// Der eindeutige Fahrzeugname ist doppelt vorhanden (eindeutiger Index entfernt).
    /// </summary>
    DuplicateName,

    /// <summary>
    /// Eine Tabelle der App ist durch eine View gleichen Namens ersetzt.
    /// </summary>
    ViewAsTable,

    /// <summary>
    /// Ein Vorgang verweist auf ein nicht vorhandenes Fahrzeug.
    /// </summary>
    OrphanEntry,

    /// <summary>
    /// Eigener Anzeigename einer Tankstelle mit 61 Zeichen (eins über der Grenze der App).
    /// </summary>
    OversizedCustomName,

    /// <summary>
    /// Eigener Anzeigename einer Tankstelle mit 200.000 Zeichen (ließ die App bei „Vorgang erfassen“ abstürzen).
    /// </summary>
    HugeCustomName,

    /// <summary>
    /// Name einer Tankstelle mit 200.000 Zeichen.
    /// </summary>
    HugeStationName,

    /// <summary>
    /// Marke einer Tankstelle mit 200.000 Zeichen.
    /// </summary>
    HugeBrand,

    /// <summary>
    /// Name einer Favoriten-Gruppe mit 200.000 Zeichen.
    /// </summary>
    HugeGroupName,

    /// <summary>
    /// Notiz eines Vorgangs mit 200.000 Zeichen.
    /// </summary>
    HugeEntryNote,

    /// <summary>
    /// Tankstellenname eines Vorgangs mit 200.000 Zeichen.
    /// </summary>
    HugeEntryStationName,

    /// <summary>
    /// Eigener Anzeigename als BLOB mit 200.000 Bytes statt als Text.
    /// </summary>
    HugeBlobCustomName,
}

/// <summary>
/// Erzeugt aus einer gültigen Sicherung eine Kopie mit manipulierter Datenbank, aber gültigem Manifest und gültigen Prüfsummen (so, wie sie ein Angreifer baut).
/// </summary>
public static class BackupTestTampering
{
    /// <summary>
    /// Die SQL-Anweisung, die die jeweilige Manipulation ausführt.
    /// </summary>
    /// <param name="tamper">Die Art der Manipulation.</param>
    /// <returns>Die Anweisung.</returns>
    public static string GetSql(BackupTamper tamper)
    {
        const string Huge = "replace(hex(zeroblob(100000)), '0', 'x')";
        const string Evil = "AFTER INSERT ON Vehicles BEGIN DELETE FROM TankbookEntries; END";
        return tamper switch
        {
            BackupTamper.Trigger => "CREATE TRIGGER evil_trigger " + Evil,
            BackupTamper.PrefixedTrigger => "PRAGMA writable_schema=ON; INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('trigger', 'sqlite_evil', 'Vehicles', 0, 'CREATE TRIGGER sqlite_evil " + Evil + "')",
            BackupTamper.View => "CREATE VIEW evil_view AS SELECT Id, Name FROM Vehicles",
            BackupTamper.ExtraCheck => "PRAGMA writable_schema=ON; UPDATE sqlite_master SET sql = substr(sql, 1, length(sql) - 1) || ', CHECK (Id < 2))' WHERE type = 'table' AND name = 'Vehicles'",
            BackupTamper.ManipulatedSequence => "UPDATE sqlite_sequence SET seq = 9223372036854775807 WHERE name = 'Vehicles'",
            BackupTamper.MigrationLockRow => "INSERT INTO __EFMigrationsLock (Id, Timestamp) VALUES (1, '2026-10-12 12:00:00')",
            BackupTamper.RemovedCollation => "PRAGMA writable_schema=ON; UPDATE sqlite_master SET sql = replace(sql, 'COLLATE NOCASE', '') WHERE type = 'table' AND name = 'Vehicles'",
            BackupTamper.ExtraColumn => "ALTER TABLE Vehicles ADD COLUMN EvilColumn TEXT",
            BackupTamper.ExtraTable => "CREATE TABLE EvilTable (Id INTEGER PRIMARY KEY, Payload TEXT)",
            BackupTamper.ExtraIndex => "CREATE INDEX evil_index ON Vehicles (Name)",
            BackupTamper.MissingTable => "DROP TABLE TankbookEntries",
            BackupTamper.MissingColumn => "ALTER TABLE Vehicles DROP COLUMN StartConsumption",
            BackupTamper.InvalidDate => "UPDATE Vehicles SET CreatedUtc = 'kein Datum'",
            BackupTamper.UnknownFuelKind => "UPDATE Vehicles SET FuelKind = 'Plutonium'",
            BackupTamper.DuplicateName => "DROP INDEX IX_Vehicles_Name; INSERT INTO Vehicles (Name, FuelKind, StartOdometerKm, CreatedUtc) VALUES ('GOLF', 'Petrol', 0, '2026-10-11 09:00:00')",
            BackupTamper.ViewAsTable => "DROP TABLE TankbookEntries; CREATE VIEW TankbookEntries AS SELECT 1 AS Id",
            BackupTamper.OrphanEntry => "UPDATE TankbookEntries SET VehicleId = 99",
            BackupTamper.OversizedCustomName => "UPDATE Stations SET CustomName = substr(replace(hex(zeroblob(31)), '0', 'x'), 1, 61)",
            BackupTamper.HugeCustomName => "UPDATE Stations SET CustomName = " + Huge,
            BackupTamper.HugeStationName => "UPDATE Stations SET Name = " + Huge,
            BackupTamper.HugeBrand => "UPDATE Stations SET Brand = " + Huge,
            BackupTamper.HugeGroupName => "UPDATE FavoriteGroups SET Name = " + Huge,
            BackupTamper.HugeEntryNote => "UPDATE TankbookEntries SET Note = " + Huge,
            BackupTamper.HugeEntryStationName => "UPDATE TankbookEntries SET StationName = " + Huge,
            BackupTamper.HugeBlobCustomName => "UPDATE Stations SET CustomName = zeroblob(200000)",
            _ => throw new ArgumentOutOfRangeException(nameof(tamper)),
        };
    }

    /// <summary>
    /// Gibt an, ob die Manipulation die Datei unbrauchbar macht (Ablehnung) oder ob ihre Wirkung beim Neuaufbau der Datenbank verloren geht (saubere Übernahme der Daten).
    /// </summary>
    /// <param name="tamper">Die Art der Manipulation.</param>
    /// <returns><see langword="true"/>, wenn die Datei abgelehnt werden muss.</returns>
    public static bool IsRejected(BackupTamper tamper)
    {
        return tamper is BackupTamper.MissingTable or BackupTamper.MissingColumn or BackupTamper.InvalidDate or BackupTamper.UnknownFuelKind or BackupTamper.OrphanEntry or BackupTamper.DuplicateName or BackupTamper.ViewAsTable
            or BackupTamper.OversizedCustomName or BackupTamper.HugeCustomName or BackupTamper.HugeStationName or BackupTamper.HugeBrand or BackupTamper.HugeGroupName
            or BackupTamper.HugeEntryNote or BackupTamper.HugeEntryStationName
            or BackupTamper.HugeBlobCustomName;
    }

    /// <summary>
    /// Liest die Datenbank, wendet die Manipulation an und schreibt ein neues Archiv mit gleichem Fotoinhalt und passendem Manifest.
    /// </summary>
    /// <param name="sourceZip">Das gültige Archiv.</param>
    /// <param name="targetZip">Das zu schreibende Archiv.</param>
    /// <param name="tamper">Die Art der Manipulation.</param>
    public static void CreateTamperedCopy(string sourceZip, string targetZip, BackupTamper tamper)
    {
        CreateTamperedCopy(sourceZip, targetZip, GetSql(tamper));
    }

    /// <summary>
    /// Liest die Datenbank, führt die Anweisung aus und schreibt ein neues Archiv mit gleichem Fotoinhalt und passendem Manifest.
    /// </summary>
    /// <param name="sourceZip">Das gültige Archiv.</param>
    /// <param name="targetZip">Das zu schreibende Archiv.</param>
    /// <param name="sql">Die SQL-Anweisung, die die Datenbank verändert.</param>
    public static void CreateTamperedCopy(string sourceZip, string targetZip, string sql)
    {
        byte[] database;
        var photos = new Dictionary<string, byte[]>();
        using (var archive = ZipFile.OpenRead(sourceZip))
        {
            database = ReadEntry(archive, "database/tankatlas.db");
            foreach (var entry in archive.Entries.Where(e => e.FullName.StartsWith("receipts/", StringComparison.Ordinal) && e.Length > 0))
            {
                photos[entry.FullName["receipts/".Length..]] = ReadEntry(archive, entry.FullName);
            }
        }

        var tampered = Tamper(database, sql, out var migration);
        BackupTestArchive.Create(targetZip, tampered, photos, migration);
    }

    private static byte[] Tamper(byte[] database, string sql, out string migration)
    {
        var path = Path.Combine(Path.GetTempPath(), "tankatlas-tamper-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            File.WriteAllBytes(path, database);
            using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false, ForeignKeys = false }.ToString()))
            {
                connection.Open();
                using var read = connection.CreateCommand();
                read.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 1";
                migration = (string)read.ExecuteScalar()!;
                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA journal_mode=DELETE";
                command.ExecuteNonQuery();
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }

            return File.ReadAllBytes(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static byte[] ReadEntry(ZipArchive archive, string name)
    {
        using var stream = archive.GetEntry(name)!.Open();
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}

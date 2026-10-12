using System.IO.Compression;
using Microsoft.Data.Sqlite;

namespace Tankradar.TestSupport;

/// <summary>
/// Die Art der Schemamanipulation an der Datenbank einer Sicherung.
/// </summary>
public enum BackupTamper
{
    /// <summary>
    /// Zusätzlicher Trigger, der beim Anlegen eines Fahrzeugs alle Vorgänge löscht.
    /// </summary>
    Trigger,

    /// <summary>
    /// Zusätzliche View.
    /// </summary>
    View,

    /// <summary>
    /// Eine Tabelle der App fehlt.
    /// </summary>
    MissingTable,

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
        return tamper switch
        {
            BackupTamper.Trigger => "CREATE TRIGGER evil_trigger AFTER INSERT ON Vehicles BEGIN DELETE FROM TankbookEntries; END",
            BackupTamper.View => "CREATE VIEW evil_view AS SELECT Id, Name FROM Vehicles",
            BackupTamper.MissingTable => "DROP TABLE TankbookEntries",
            BackupTamper.ExtraColumn => "ALTER TABLE Vehicles ADD COLUMN EvilColumn TEXT",
            BackupTamper.ExtraTable => "CREATE TABLE EvilTable (Id INTEGER PRIMARY KEY, Payload TEXT)",
            BackupTamper.ExtraIndex => "CREATE INDEX evil_index ON Vehicles (Name)",
            _ => throw new ArgumentOutOfRangeException(nameof(tamper)),
        };
    }

    /// <summary>
    /// Liest die Datenbank, wendet die Manipulation an und schreibt ein neues Archiv mit gleichem Fotoinhalt und passendem Manifest.
    /// </summary>
    /// <param name="sourceZip">Das gültige Archiv.</param>
    /// <param name="targetZip">Das zu schreibende Archiv.</param>
    /// <param name="tamper">Die Art der Manipulation.</param>
    public static void CreateTamperedCopy(string sourceZip, string targetZip, BackupTamper tamper)
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

        var tampered = Tamper(database, GetSql(tamper), out var migration);
        BackupTestArchive.Create(targetZip, tampered, photos, migration);
    }

    private static byte[] Tamper(byte[] database, string sql, out string migration)
    {
        var path = Path.Combine(Path.GetTempPath(), "tankatlas-tamper-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            File.WriteAllBytes(path, database);
            using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString()))
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

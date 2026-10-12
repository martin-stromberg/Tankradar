using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models.Backup;

namespace Tankradar.MAUI.Services.Backup;

/// <summary>
/// Prüft und migriert die entpackte Datenbank einer Sicherung, bevor sie die Livedaten ersetzen darf. Eine externe Datei gilt als nicht vertrauenswürdig:
/// Neben der Integrität und dem Migrationsstand muss das Schema nach der Migration exakt dem einer frisch angelegten Datenbank entsprechen,
/// jede Abweichung (fremde oder fehlende Tabellen und Spalten, zusätzliche Indizes, Trigger oder Views) lässt die Datei scheitern.
/// </summary>
public static class BackupDatabasePreparer
{
    /// <summary>
    /// Bereitet die Datenbank vor: Integritätsprüfung, Migrationsstand laut Manifest, Migration auf den aktuellen Stand, Schemavergleich mit einer frisch migrierten Referenz,
    /// Fremdschlüsselprüfung. Die Datenbank bleibt im Journalmodus <c>DELETE</c> und ohne <c>-wal</c>/<c>-shm</c>.
    /// </summary>
    /// <param name="databasePath">Der Pfad der entpackten Datenbank.</param>
    /// <param name="manifest">Das geprüfte Manifest.</param>
    /// <param name="referenceDirectory">Ein Arbeitsverzeichnis für die Referenzdatenbank; es wird angelegt und am Ende gelöscht.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns><see cref="BackupResult.Ok"/> oder <see cref="BackupResult.InvalidFile"/>.</returns>
    public static async Task<BackupResult> PrepareAsync(string databasePath, BackupManifest manifest, string referenceDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        try
        {
            var connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadWrite, Pooling = false }.ToString();
            await using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                if (!await IsIntegrityOkAsync(connection, cancellationToken).ConfigureAwait(false))
                {
                    return BackupResult.InvalidFile;
                }

                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 1";
                var latest = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
                if (!string.Equals(latest, manifest.DatabaseMigration, StringComparison.Ordinal))
                {
                    return BackupResult.InvalidFile;
                }
            }

            await MigrateAsync(databasePath, cancellationToken).ConfigureAwait(false);

            if (!await HasCurrentSchemaAsync(databasePath, referenceDirectory, cancellationToken).ConfigureAwait(false))
            {
                return BackupResult.InvalidFile;
            }

            await using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                await using var check = connection.CreateCommand();
                check.CommandText = "PRAGMA foreign_key_check";
                await using (var reader = await check.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        return BackupResult.InvalidFile;
                    }
                }

                await using var mode = connection.CreateCommand();
                mode.CommandText = "PRAGMA journal_mode=DELETE";
                await mode.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            BackupFileCleanup.DeleteFile(databasePath + "-wal");
            BackupFileCleanup.DeleteFile(databasePath + "-shm");
            return BackupResult.Ok;
        }
        catch (DbException)
        {
            return BackupResult.InvalidFile;
        }
    }

    private static async Task MigrateAsync(string databasePath, CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<TankradarDbContext>().UseSqlite(TankradarDbContext.BuildConnectionString(databasePath, pooling: false)).Options;
        await using var context = new TankradarDbContext(options);
        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> HasCurrentSchemaAsync(string databasePath, string referenceDirectory, CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(referenceDirectory);
            var referencePath = Path.Combine(referenceDirectory, TankradarDbContext.DatabaseFileName);
            await MigrateAsync(referencePath, cancellationToken).ConfigureAwait(false);
            var expected = await BackupSchemaDescriber.DescribeAsync(referencePath, cancellationToken).ConfigureAwait(false);
            var actual = await BackupSchemaDescriber.DescribeAsync(databasePath, cancellationToken).ConfigureAwait(false);
            return expected.SequenceEqual(actual, StringComparer.Ordinal);
        }
        finally
        {
            BackupFileCleanup.DeleteDirectory(referenceDirectory);
        }
    }

    private static async Task<bool> IsIntegrityOkAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check";
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
        return string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase);
    }
}

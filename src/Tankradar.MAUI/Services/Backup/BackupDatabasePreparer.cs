using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Models.Favorites;
using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Backup;

/// <summary>
/// Baut aus der entpackten Datenbank einer Sicherung eine neue, saubere Datenbank auf. Eine externe Datei gilt als nicht vertrauenswürdig:
/// Ihre Objekte (Trigger, Views, Einschränkungen, Indizes, Kollationen, Zähltabelle, Migrationssperre, sonstige Tabellen) werden nie übernommen.
/// Stattdessen entsteht mit der Migrationskette der App eine frische Referenzdatenbank (auf dem Migrationsstand der Sicherung); die Sicherungsdatenbank wird nur gelesen,
/// ihre Zeilen werden tabellenweise mit expliziten Spaltenlisten hineinkopiert, danach wird die Referenz auf den aktuellen Stand migriert und geprüft.
/// </summary>
public static class BackupDatabasePreparer
{
    private const int CancellationCheckInterval = 500;

    /// <summary>
    /// Bereitet die Datenbank vor: Integritätsprüfung und Migrationsstand der Sicherung (nur lesend), Referenz auf diesem Stand anlegen, Daten kopieren, Fremdschlüsselprüfung,
    /// Migration auf den aktuellen Stand, Prüfung (<c>integrity_check</c>, <c>foreign_key_check</c>, jede Entität einmal über Entity Framework gelesen) und Zählung.
    /// Bei Erfolg ersetzt die fertige Datenbank (Journalmodus <c>DELETE</c>, ohne <c>-wal</c>/<c>-shm</c>) die Datei unter <paramref name="databasePath"/>.
    /// </summary>
    /// <param name="databasePath">Der Pfad der entpackten Datenbank.</param>
    /// <param name="manifest">Das geprüfte Manifest.</param>
    /// <param name="workDirectory">Ein Arbeitsverzeichnis für die Referenzdatenbank; es wird angelegt und am Ende gelöscht.</param>
    /// <param name="timeout">Die Höchstdauer der gesamten Vorbereitung.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns><see cref="BackupResult.Ok"/> mit den Zählwerten, <see cref="BackupResult.InvalidFile"/> bei einer unbrauchbaren Datenbank oder <see cref="BackupResult.Failed"/>
    /// bei einer Zeitüberschreitung oder einem Fehler außerhalb der Datei (z. B. Speicher voll).</returns>
    public static async Task<BackupPreparation> PrepareAsync(string databasePath, BackupManifest manifest, string workDirectory, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(timeout);
        try
        {
            Directory.CreateDirectory(workDirectory);
            return await BuildAsync(databasePath, manifest, workDirectory, limit.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new BackupPreparation(BackupResult.Failed);
        }
        catch (SourceFileException)
        {
            // Ein Fehler beim Lesen der Sicherungsdatenbank (auch Ein-/Ausgabe- oder Öffnungsfehler einer abgeschnittenen Datei) liegt an der Datei, nicht an der Umgebung.
            return new BackupPreparation(BackupResult.InvalidFile);
        }
        catch (SqliteException ex) when (IsEnvironmentError(ex))
        {
            return new BackupPreparation(BackupResult.Failed);
        }
        catch (Exception ex) when (ex is DbException or InvalidOperationException or FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            return new BackupPreparation(BackupResult.InvalidFile);
        }
        finally
        {
            BackupFileCleanup.DeleteDirectory(workDirectory);
        }
    }

    private static async Task<BackupPreparation> BuildAsync(string databasePath, BackupManifest manifest, string workDirectory, CancellationToken cancellationToken)
    {
        var referencePath = Path.Combine(workDirectory, TankradarDbContext.DatabaseFileName);
        var sourceConnectionString = new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString();

        // Die Sicherungsdatenbank wird ausschließlich gelesen und nie migriert; eine Zeile in ihrer Migrationssperre kann deshalb nichts blockieren.
        try
        {
            await using var source = new SqliteConnection(sourceConnectionString);
            await source.OpenAsync(cancellationToken).ConfigureAwait(false);
            if (!await IsIntegrityOkAsync(source, cancellationToken).ConfigureAwait(false)
                || !string.Equals(await ReadLatestMigrationAsync(source, cancellationToken).ConfigureAwait(false), manifest.DatabaseMigration, StringComparison.Ordinal))
            {
                return new BackupPreparation(BackupResult.InvalidFile);
            }
        }
        catch (SqliteException)
        {
            // Nur die Sicherungsdatenbank wird hier gelesen: Jeder SQLite-Fehler stammt aus der Datei.
            return new BackupPreparation(BackupResult.InvalidFile);
        }

        try
        {
            await MigrateAsync(referencePath, manifest.DatabaseMigration, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Die Referenz entsteht nur aus der App selbst; scheitert sie, liegt es nie an der Datei.
            return new BackupPreparation(BackupResult.Failed);
        }

        // Fremdschlüssel sind beim Kopieren aus; sie werden danach gesammelt mit foreign_key_check geprüft (Reihenfolge der Tabellen spielt so keine Rolle).
        var referenceConnectionString = new SqliteConnectionStringBuilder { DataSource = referencePath, Mode = SqliteOpenMode.ReadWrite, Pooling = false, ForeignKeys = false }.ToString();
        await using (var reference = new SqliteConnection(referenceConnectionString))
        await using (var source = new SqliteConnection(sourceConnectionString))
        {
            await reference.OpenAsync(cancellationToken).ConfigureAwait(false);
            await source.OpenAsync(cancellationToken).ConfigureAwait(false);
            if (!await CopyTablesAsync(source, reference, cancellationToken).ConfigureAwait(false)
                || await HasForeignKeyViolationAsync(reference, cancellationToken).ConfigureAwait(false))
            {
                return new BackupPreparation(BackupResult.InvalidFile);
            }
        }

        await MigrateAsync(referencePath, null, cancellationToken).ConfigureAwait(false);

        await using (var reference = new SqliteConnection(referenceConnectionString))
        {
            await reference.OpenAsync(cancellationToken).ConfigureAwait(false);
            if (!await IsIntegrityOkAsync(reference, cancellationToken).ConfigureAwait(false)
                || await HasForeignKeyViolationAsync(reference, cancellationToken).ConfigureAwait(false)
                || await HasOversizedTextAsync(reference, cancellationToken).ConfigureAwait(false))
            {
                return new BackupPreparation(BackupResult.InvalidFile);
            }

            await using var mode = reference.CreateCommand();
            mode.CommandText = "PRAGMA journal_mode=DELETE";
            await mode.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        var counts = await ReadAllEntitiesAsync(referencePath, cancellationToken).ConfigureAwait(false);
        if (counts is null)
        {
            return new BackupPreparation(BackupResult.InvalidFile);
        }

        BackupFileCleanup.DeleteFile(referencePath + "-wal");
        BackupFileCleanup.DeleteFile(referencePath + "-shm");
        BackupFileCleanup.DeleteFile(databasePath + "-wal");
        BackupFileCleanup.DeleteFile(databasePath + "-shm");
        File.Move(referencePath, databasePath, overwrite: true);
        return new BackupPreparation(BackupResult.Ok, counts);
    }

    private static async Task MigrateAsync(string databasePath, string? targetMigration, CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<TankradarDbContext>().UseSqlite(TankradarDbContext.BuildConnectionString(databasePath, pooling: false)).Options;
        await using var context = new TankradarDbContext(options);
        if (targetMigration is null)
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        await context.GetService<IMigrator>().MigrateAsync(targetMigration, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> CopyTablesAsync(SqliteConnection source, SqliteConnection reference, CancellationToken cancellationToken)
    {
        // Tabellen und Spalten stammen aus der Referenz (der App selbst), nie aus der Sicherung; deren Tabellenobjekte werden nur nach Namen gesucht.
        var tables = await ReadStringsAsync(
            reference,
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite\\_%' ESCAPE '\\' AND name NOT LIKE '\\_\\_EFMigrations%' ESCAPE '\\' ORDER BY name",
            null,
            cancellationToken).ConfigureAwait(false);

        await using var transaction = (SqliteTransaction)await reference.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        foreach (var table in tables)
        {
            var columns = await ReadStringsAsync(reference, "SELECT name FROM pragma_table_xinfo($name) WHERE hidden = 0 ORDER BY cid", table, cancellationToken).ConfigureAwait(false);
            var present = await ReadSourceStringsAsync(source, "SELECT name FROM sqlite_master WHERE type = 'table' AND name = $name", table, cancellationToken).ConfigureAwait(false);
            if (columns.Count == 0 || present.Count != 1)
            {
                return false;
            }

            var sourceColumns = await ReadSourceStringsAsync(source, "SELECT name FROM pragma_table_xinfo($name) WHERE hidden = 0", table, cancellationToken).ConfigureAwait(false);
            if (columns.Any(column => !sourceColumns.Contains(column, StringComparer.OrdinalIgnoreCase)))
            {
                return false;
            }

            await CopyTableAsync(source, reference, transaction, table, columns, cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static async Task CopyTableAsync(SqliteConnection source, SqliteConnection reference, SqliteTransaction transaction, string table, List<string> columns, CancellationToken cancellationToken)
    {
        var list = string.Join(", ", columns.Select(Quote));
        await using var select = source.CreateCommand();
        select.CommandText = $"SELECT {list} FROM {Quote(table)}";
        await using var insert = reference.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = $"INSERT INTO {Quote(table)} ({list}) VALUES ({string.Join(", ", columns.Select((_, index) => "$p" + index))})";
        var parameters = new List<SqliteParameter>();
        for (var index = 0; index < columns.Count; index++)
        {
            // Ohne festen Typ: Der Speichertyp (Ganzzahl, Gleitkomma, Text, Blob, NULL) folgt dem gelesenen Wert.
            var parameter = insert.CreateParameter();
            parameter.ParameterName = "$p" + index;
            insert.Parameters.Add(parameter);
            parameters.Add(parameter);
        }

        await using var reader = await ExecuteSourceReaderAsync(select, cancellationToken).ConfigureAwait(false);
        var rows = 0;
        while (await ReadSourceRowAsync(reader, cancellationToken).ConfigureAwait(false))
        {
            for (var index = 0; index < parameters.Count; index++)
            {
                parameters[index].Value = reader.GetValue(index);
            }

            await insert.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            if (++rows % CancellationCheckInterval == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }

    private static async Task<BackupManifestCounts?> ReadAllEntitiesAsync(string databasePath, CancellationToken cancellationToken)
    {
        // Jede Entität wird einmal über Entity Framework gelesen: ungültige Datums-, Zahlen- und Enum-Werte würden sonst erst in der laufenden App auffallen.
        // Geprüft werden nur Werte, die die App selbst nur gültig schreibt.
        var options = new DbContextOptionsBuilder<TankradarDbContext>().UseSqlite(TankradarDbContext.BuildConnectionString(databasePath, pooling: false)).Options;
        await using var context = new TankradarDbContext(options);
        await foreach (var item in context.UserSettings.AsNoTracking().AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            _ = item;
        }

        await foreach (var item in context.FuelTypeSettings.AsNoTracking().AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            _ = item;
        }

        var counts = new BackupManifestCounts();
        await foreach (var item in context.Stations.AsNoTracking().AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            _ = item;
            counts.Stations++;
        }

        await foreach (var item in context.PriceEntries.AsNoTracking().AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            _ = item;
            counts.Prices++;
        }

        await foreach (var item in context.FavoriteGroups.AsNoTracking().AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            _ = item;
            counts.Groups++;
        }

        await foreach (var entry in context.FavoriteEntries.AsNoTracking().AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (!IsDefined<FavoritePriority>(entry.Priority))
            {
                return null;
            }

            counts.Favorites++;
        }

        await foreach (var vehicle in context.Vehicles.AsNoTracking().AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (!IsDefined<VehicleFuelKind>(vehicle.FuelKind))
            {
                return null;
            }

            counts.Vehicles++;
        }

        await foreach (var entry in context.TankbookEntries.AsNoTracking().AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (!IsDefined<TankbookEntryKind>(entry.Kind))
            {
                return null;
            }

            counts.Entries++;
        }

        return counts;
    }

    private static bool IsDefined<TEnum>(string value)
        where TEnum : struct, Enum
    {
        return Enum.TryParse<TEnum>(value, out var parsed) && Enum.IsDefined(parsed);
    }

    private static bool IsEnvironmentError(SqliteException ex)
    {
        // SQLITE_IOERR (10), SQLITE_FULL (13), SQLITE_CANTOPEN (14): Speicher voll oder Dateisystemfehler, nicht der Inhalt der Datei.
        return ex.SqliteErrorCode is 10 or 13 or 14;
    }

    private static string Quote(string identifier)
    {
        return "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static async Task<List<string>> ReadSourceStringsAsync(SqliteConnection source, string sql, string name, CancellationToken cancellationToken)
    {
        try
        {
            return await ReadStringsAsync(source, sql, name, cancellationToken).ConfigureAwait(false);
        }
        catch (SqliteException ex)
        {
            throw new SourceFileException(ex);
        }
    }

    private static async Task<SqliteDataReader> ExecuteSourceReaderAsync(SqliteCommand select, CancellationToken cancellationToken)
    {
        try
        {
            return await select.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SqliteException ex)
        {
            throw new SourceFileException(ex);
        }
    }

    private static async Task<bool> ReadSourceRowAsync(SqliteDataReader reader, CancellationToken cancellationToken)
    {
        try
        {
            return await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SqliteException ex)
        {
            throw new SourceFileException(ex);
        }
    }

    private static async Task<List<string>> ReadStringsAsync(SqliteConnection connection, string sql, string? name, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (name is not null)
        {
            command.Parameters.AddWithValue("$name", name);
        }

        var values = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }

    private static async Task<string?> ReadLatestMigrationAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 1";
        return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
    }

    private static async Task<bool> HasOversizedTextAsync(SqliteConnection reference, CancellationToken cancellationToken)
    {
        // Tabellen- und Spaltennamen stammen aus der festen Liste der App; geprüft wird die bereits in die Referenz kopierte Fassung (Länge in Zeichen und, gegen eingebettete NUL-Zeichen, in UTF-8-Bytes).
        foreach (var (table, column, maxLength) in BackupTextLimits.Columns)
        {
            await using var command = reference.CreateCommand();
            command.CommandText = $"SELECT 1 FROM {Quote(table)} WHERE length({Quote(column)}) > $max OR length(CAST({Quote(column)} AS BLOB)) > $maxBytes LIMIT 1";
            command.Parameters.AddWithValue("$max", maxLength);
            command.Parameters.AddWithValue("$maxBytes", maxLength * 4);
            if (await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is not null)
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<bool> HasForeignKeyViolationAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_key_check";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> IsIntegrityOkAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check";
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
        return string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Kennzeichnet einen Fehler, der beim Lesen der Sicherungsdatenbank auftrat; er liegt an der Datei und nicht an der Umgebung.
    /// </summary>
    private sealed class SourceFileException : Exception
    {
        public SourceFileException(SqliteException inner)
            : base("Die Sicherungsdatenbank konnte nicht gelesen werden.", inner)
        {
        }
    }
}

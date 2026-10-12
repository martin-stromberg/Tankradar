using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Tankradar.MAUI.Services.Backup;

/// <summary>
/// Beschreibt das Schema einer SQLite-Datenbank als sortierte Liste von Zeilen (Objekte, Spalten, Indizes, Fremdschlüssel; Trigger und Views samt Text),
/// damit zwei Datenbanken auf Gleichheit des Schemas verglichen werden können. Die Beschreibung stützt sich auf <c>sqlite_master</c> und die <c>pragma_*</c>-Funktionen
/// statt auf den Text der <c>CREATE</c>-Anweisungen von Tabellen und Indizes, weil dieser Text je nach App-Version, mit der die Datenbank angelegt wurde, abweichen kann.
/// </summary>
public static class BackupSchemaDescriber
{
    /// <summary>
    /// Liest die Schemabeschreibung einer Datenbank.
    /// </summary>
    /// <param name="databasePath">Der Pfad der Datenbankdatei.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Die Zeilen der Beschreibung in fester Reihenfolge.</returns>
    public static async Task<IReadOnlyList<string>> DescribeAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadWrite, Pooling = false }.ToString();
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var lines = new List<string>();
        var tables = new List<string>();
        await using (var command = connection.CreateCommand())
        {
            // sqlite_-Objekte (sqlite_sequence, Statistiktabellen) legt SQLite selbst an; ihre Indizes erscheinen über die Indexlisten der Tabellen.
            command.CommandText = "SELECT type, name, tbl_name, CASE WHEN type IN ('trigger', 'view') THEN sql ELSE '' END FROM sqlite_master WHERE name NOT LIKE 'sqlite\\_%' ESCAPE '\\' ORDER BY type, name, tbl_name";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var type = reader.GetString(0);
                var name = reader.GetString(1);
                lines.Add(Join("object", type, name, reader.IsDBNull(2) ? string.Empty : reader.GetString(2), reader.IsDBNull(3) ? string.Empty : reader.GetString(3)));
                if (type == "table")
                {
                    tables.Add(name);
                }
            }
        }

        foreach (var table in tables)
        {
            await AppendRowsAsync(connection, lines, "table " + table, "SELECT type, ncol, wr, strict FROM pragma_table_list WHERE schema = 'main' AND name = $name", table, cancellationToken).ConfigureAwait(false);
            await AppendRowsAsync(connection, lines, "column " + table, "SELECT cid, name, type, \"notnull\", dflt_value, pk, hidden FROM pragma_table_xinfo($name) ORDER BY cid", table, cancellationToken).ConfigureAwait(false);
            await AppendRowsAsync(connection, lines, "foreignkey " + table, "SELECT id, seq, \"table\", \"from\", \"to\", on_update, on_delete, \"match\" FROM pragma_foreign_key_list($name) ORDER BY id, seq", table, cancellationToken).ConfigureAwait(false);

            var indexes = new List<string>();
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT name, \"unique\", origin, partial FROM pragma_index_list($name) ORDER BY name";
                command.Parameters.AddWithValue("$name", table);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    indexes.Add(reader.GetString(0));
                    lines.Add(Join("index " + table, Format(reader, 4)));
                }
            }

            foreach (var index in indexes)
            {
                await AppendRowsAsync(connection, lines, "indexcolumn " + index, "SELECT seqno, cid, name, desc, coll, key FROM pragma_index_xinfo($name) ORDER BY seqno", index, cancellationToken).ConfigureAwait(false);
            }
        }

        return lines;
    }

    private static async Task AppendRowsAsync(SqliteConnection connection, List<string> lines, string prefix, string sql, string name, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$name", name);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            lines.Add(Join(prefix, Format(reader, reader.FieldCount)));
        }
    }

    private static string[] Format(SqliteDataReader reader, int count)
    {
        var values = new string[count];
        for (var index = 0; index < count; index++)
        {
            values[index] = reader.IsDBNull(index) ? "<null>" : Convert.ToString(reader.GetValue(index), CultureInfo.InvariantCulture) ?? string.Empty;
        }

        return values;
    }

    private static string Join(string prefix, params string[] values)
    {
        return prefix + "|" + string.Join("|", values);
    }
}

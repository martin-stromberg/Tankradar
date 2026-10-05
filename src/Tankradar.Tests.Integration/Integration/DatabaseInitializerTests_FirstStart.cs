using Microsoft.Data.Sqlite;
using Tankradar.MAUI.Data;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft das Anlegen der Datenbank beim ersten Start im Datenverzeichnis aus <c>IAppDataPathProvider</c>.
/// </summary>
public class DatabaseInitializerTests_FirstStart : IDisposable
{
    private readonly TestDatabase _database = new();

    /// <summary>
    /// Prüft, dass Datei, Tabellen und Migrationshistorie angelegt werden.
    /// </summary>
    [Fact]
    public async Task Initialize_FirstStart_CreatesDatabaseFileAndSchema()
    {
        Assert.False(File.Exists(_database.DatabasePath));

        await _database.CreateInitializer(_database.CreateFactory()).InitializeAsync();

        Assert.Equal(Path.Combine(_database.DataDirectory, TankradarDbContext.DatabaseFileName), _database.DatabasePath);
        Assert.True(File.Exists(_database.DatabasePath));
        using var connection = new SqliteConnection($"Data Source={_database.DatabasePath};Pooling=False");
        connection.Open();
        var tables = Query(connection, "SELECT name FROM sqlite_master WHERE type = 'table'");
        Assert.Contains("UserSettings", tables);
        Assert.Contains("FuelTypeSettings", tables);
        var history = Query(connection, "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId");
        Assert.Equal(3, history.Count);
        Assert.EndsWith("_InitialCreate", history[0]);
        Assert.EndsWith("_AddFuelTypeSettings", history[1]);
        Assert.EndsWith("_AddPriceCache", history[2]);
        Assert.Contains("Stations", tables);
        Assert.Contains("PriceEntries", tables);
    }

    /// <summary>
    /// Räumt die Testumgebung auf.
    /// </summary>
    public void Dispose()
    {
        _database.Dispose();
        GC.SuppressFinalize(this);
    }

    private static List<string> Query(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using var reader = command.ExecuteReader();
        var result = new List<string>();
        while (reader.Read())
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }
}

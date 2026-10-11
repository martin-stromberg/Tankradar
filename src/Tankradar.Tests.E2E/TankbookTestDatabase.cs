using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Tankradar.Tests.E2E;

/// <summary>
/// Direkter Zugriff auf die isolierte Testdatenbank der laufenden App, um für das Tankbuch Tank- bzw. Ladevorgänge einzufügen und zu zählen
/// (in Schritt 12 gibt es dafür noch keine Oberfläche). Verbindungen ohne Pool, damit die Datei nach jedem Zugriff freigegeben ist.
/// </summary>
public static class TankbookTestDatabase
{
    /// <summary>
    /// Fügt dem Fahrzeug mit dem Namen die angegebene Zahl an Vorgängen hinzu.
    /// </summary>
    /// <param name="databasePath">Der Pfad der Datenbankdatei der App.</param>
    /// <param name="vehicleName">Der Name des Fahrzeugs.</param>
    /// <param name="count">Die Zahl der Vorgänge.</param>
    public static void AddEntries(string databasePath, string vehicleName, int count)
    {
        using var connection = Open(databasePath);
        using var lookup = connection.CreateCommand();
        lookup.CommandText = "SELECT Id FROM Vehicles WHERE Name = $name";
        lookup.Parameters.AddWithValue("$name", vehicleName);
        var vehicleId = Convert.ToInt64(lookup.ExecuteScalar() ?? throw new InvalidOperationException("Das Fahrzeug fehlt in der Testdatenbank."), CultureInfo.InvariantCulture);

        using var transaction = connection.BeginTransaction();
        for (var i = 0; i < count; i++)
        {
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = "INSERT INTO TankbookEntries (VehicleId, CreatedUtc) VALUES ($vehicleId, $created)";
            insert.Parameters.AddWithValue("$vehicleId", vehicleId);
            insert.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            insert.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    /// <summary>
    /// Zählt alle Vorgänge in der Testdatenbank.
    /// </summary>
    /// <param name="databasePath">Der Pfad der Datenbankdatei der App.</param>
    /// <returns>Die Zahl der Vorgänge.</returns>
    public static int CountEntries(string databasePath)
    {
        using var connection = Open(databasePath);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM TankbookEntries";
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static SqliteConnection Open(string databasePath)
    {
        var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False;Default Timeout=5");
        connection.Open();
        return connection;
    }
}

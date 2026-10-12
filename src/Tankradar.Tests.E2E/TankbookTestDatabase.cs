using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Tankradar.Tests.E2E;

/// <summary>
/// Direkter Zugriff auf die isolierte Testdatenbank und das Datenverzeichnis der laufenden App: Vorgänge einfügen (z. B. „inzwischen hinzugekommen“),
/// gespeicherte Werte und Belegfotos prüfen. Verbindungen ohne Pool, damit die Datei nach jedem Zugriff freigegeben ist.
/// </summary>
public static class TankbookTestDatabase
{
    /// <summary>
    /// Fügt dem Fahrzeug mit dem Namen vollständige Vorgänge hinzu (Alpha Tankstelle, 40 L zu 1,859 €/L, je 100 km mehr, je eine Minute später).
    /// </summary>
    /// <param name="databasePath">Der Pfad der Datenbankdatei der App.</param>
    /// <param name="vehicleName">Der Name des Fahrzeugs.</param>
    /// <param name="count">Die Zahl der Vorgänge.</param>
    /// <param name="firstOdometerKm">Der Kilometerstand des ersten Vorgangs; <see langword="null"/> für den Start-Kilometerstand des Fahrzeugs.</param>
    /// <param name="firstOccurredAt">Der Zeitpunkt (lokale Wanduhrzeit) des ersten Vorgangs; <see langword="null"/> für gestern um diese Zeit.</param>
    public static void AddEntries(string databasePath, string vehicleName, int count, int? firstOdometerKm = null, DateTime? firstOccurredAt = null)
    {
        using var connection = Open(databasePath);
        using var lookup = connection.CreateCommand();
        lookup.CommandText = "SELECT Id, StartOdometerKm FROM Vehicles WHERE Name = $name";
        lookup.Parameters.AddWithValue("$name", vehicleName);
        long vehicleId;
        int startKm;
        using (var reader = lookup.ExecuteReader())
        {
            if (!reader.Read())
            {
                throw new InvalidOperationException("Das Fahrzeug fehlt in der Testdatenbank.");
            }

            vehicleId = reader.GetInt64(0);
            startKm = reader.GetInt32(1);
        }

        var occurred = firstOccurredAt ?? DateTime.Now.AddDays(-1);
        occurred = new DateTime(occurred.Year, occurred.Month, occurred.Day, occurred.Hour, occurred.Minute, 0, DateTimeKind.Unspecified);
        var odometer = firstOdometerKm ?? startKm;
        using var transaction = connection.BeginTransaction();
        for (var i = 0; i < count; i++)
        {
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText =
                "INSERT INTO TankbookEntries (VehicleId, CreatedUtc, OccurredAt, Kind, Quantity, UnitPrice, TotalAmount, OdometerKm, IsPartial, StationName) "
                + "VALUES ($vehicleId, $created, $occurred, 'Fuel', '40.0', '1.859', '74.36', $odometer, 0, 'Alpha Tankstelle')";
            insert.Parameters.AddWithValue("$vehicleId", vehicleId);
            insert.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            insert.Parameters.AddWithValue("$occurred", occurred.AddMinutes(i).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            insert.Parameters.AddWithValue("$odometer", odometer + (i * 100));
            insert.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    /// <summary>
    /// Fügt dem Fahrzeug mit dem Namen einen Vorgang mit frei wählbarer Art, Menge, Gesamtbetrag und Zeitpunkt hinzu (Alpha Tankstelle bzw. Zuhause; der Preis pro Einheit wird berechnet).
    /// </summary>
    /// <param name="databasePath">Der Pfad der Datenbankdatei der App.</param>
    /// <param name="vehicleName">Der Name des Fahrzeugs.</param>
    /// <param name="occurredAt">Der Zeitpunkt (lokale Wanduhrzeit, minutengenau).</param>
    /// <param name="odometerKm">Der Kilometerstand.</param>
    /// <param name="quantity">Die Menge in Litern bzw. kWh.</param>
    /// <param name="totalAmount">Der Gesamtbetrag in Euro.</param>
    /// <param name="isPartial">Gibt an, ob es eine Teilbetankung ist.</param>
    /// <param name="kind">Die Art (<c>Fuel</c> oder <c>Electricity</c>).</param>
    public static void AddEntry(string databasePath, string vehicleName, DateTime occurredAt, int odometerKm, decimal quantity, decimal totalAmount, bool isPartial = false, string kind = "Fuel")
    {
        using var connection = Open(databasePath);
        using var lookup = connection.CreateCommand();
        lookup.CommandText = "SELECT Id FROM Vehicles WHERE Name = $name";
        lookup.Parameters.AddWithValue("$name", vehicleName);
        var vehicleId = lookup.ExecuteScalar() ?? throw new InvalidOperationException("Das Fahrzeug fehlt in der Testdatenbank.");
        using var insert = connection.CreateCommand();
        insert.CommandText =
            "INSERT INTO TankbookEntries (VehicleId, CreatedUtc, OccurredAt, Kind, Quantity, UnitPrice, TotalAmount, OdometerKm, IsPartial, StationName) "
            + "VALUES ($vehicleId, $created, $occurred, $kind, $quantity, $unitPrice, $total, $odometer, $partial, $station)";
        insert.Parameters.AddWithValue("$vehicleId", vehicleId);
        insert.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        insert.Parameters.AddWithValue("$occurred", occurredAt.ToString("yyyy-MM-dd HH:mm:00", CultureInfo.InvariantCulture));
        insert.Parameters.AddWithValue("$kind", kind);
        insert.Parameters.AddWithValue("$quantity", quantity.ToString(CultureInfo.InvariantCulture));
        insert.Parameters.AddWithValue("$unitPrice", Math.Round(totalAmount / quantity, 3, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture));
        insert.Parameters.AddWithValue("$total", totalAmount.ToString(CultureInfo.InvariantCulture));
        insert.Parameters.AddWithValue("$odometer", odometerKm);
        insert.Parameters.AddWithValue("$partial", isPartial ? 1 : 0);
        insert.Parameters.AddWithValue("$station", kind == "Electricity" ? "Zuhause" : "Alpha Tankstelle");
        insert.ExecuteNonQuery();
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

    /// <summary>
    /// Liest die gespeicherten Werte aller Vorgänge, nach Kilometerstand aufsteigend.
    /// </summary>
    /// <param name="databasePath">Der Pfad der Datenbankdatei der App.</param>
    /// <returns>Die Vorgänge.</returns>
    public static IReadOnlyList<StoredEntry> ReadEntries(string databasePath)
    {
        using var connection = Open(databasePath);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Kind, Quantity, UnitPrice, TotalAmount, OdometerKm, IsPartial, StationName, StationId, ReceiptPhotoFileName FROM TankbookEntries ORDER BY OdometerKm, Id";
        using var reader = command.ExecuteReader();
        var entries = new List<StoredEntry>();
        while (reader.Read())
        {
            entries.Add(new StoredEntry(
                reader.GetString(0),
                decimal.Parse(reader.GetString(1), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(2), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(3), CultureInfo.InvariantCulture),
                reader.GetInt32(4),
                reader.GetInt64(5) != 0,
                reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetString(8)));
        }

        return entries;
    }

    /// <summary>
    /// Liefert die Dateinamen der Belegfotos im Unterverzeichnis <c>receipts</c> des Datenverzeichnisses.
    /// </summary>
    /// <param name="dataDirectory">Das Testdatenverzeichnis der App.</param>
    /// <returns>Die Dateinamen.</returns>
    public static IReadOnlyList<string> ListReceiptFiles(string dataDirectory)
    {
        var directory = Path.Combine(dataDirectory, "receipts");
        return Directory.Exists(directory) ? Directory.EnumerateFiles(directory).Select(path => Path.GetFileName(path)).ToList() : [];
    }

    /// <summary>
    /// Schreibt eine kleine, gültige PNG-Datei (1 × 1 Pixel) als Belegfoto der Test-Fotoauswahl.
    /// </summary>
    /// <param name="path">Der Zielpfad.</param>
    public static void WriteTestPng(string path)
    {
        File.WriteAllBytes(path, Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg=="));
    }

    private static SqliteConnection Open(string databasePath)
    {
        var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False;Default Timeout=5");
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Die gespeicherten Werte eines Vorgangs.
    /// </summary>
    /// <param name="Kind">Die Art.</param>
    /// <param name="Quantity">Die Menge.</param>
    /// <param name="UnitPrice">Der Preis pro Einheit.</param>
    /// <param name="TotalAmount">Der Gesamtbetrag.</param>
    /// <param name="OdometerKm">Der Kilometerstand.</param>
    /// <param name="IsPartial">Gibt an, ob es eine Teilbetankung ist.</param>
    /// <param name="StationName">Der Name der Tankstelle.</param>
    /// <param name="StationId">Die Kennung der Favoriten-Tankstelle oder <see langword="null"/>.</param>
    /// <param name="ReceiptPhotoFileName">Der Dateiname des Belegfotos oder <see langword="null"/>.</param>
    /// <returns>Der Wert.</returns>
    public sealed record StoredEntry(
        string Kind,
        decimal Quantity,
        decimal UnitPrice,
        decimal TotalAmount,
        int OdometerKm,
        bool IsPartial,
        string StationName,
        string? StationId,
        string? ReceiptPhotoFileName);
}

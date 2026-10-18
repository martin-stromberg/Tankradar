using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Services;
using Tankradar.MAUI.Services.Backup;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.MAUI.Services.Tankbook;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration.Support;

/// <summary>
/// Integrationsumgebung für den <see cref="BackupService"/>: echte SQLite-Datei und Fotoablage im isolierten Testdatenverzeichnis, aufzeichnender Dateischutz und Zwischenspeicher-Zähler.
/// </summary>
public sealed class BackupTestEnvironment : IDisposable
{
    /// <summary>
    /// Der Dateiname des Beispielfotos.
    /// </summary>
    public const string PhotoName = "0123456789abcdef0123456789abcdef.jpg";

    /// <summary>
    /// Die Kennung der Beispieltankstelle.
    /// </summary>
    public const string StationId = "00000001-0000-4000-8000-000000000000";

    private readonly TestDatabase _database = new();

    /// <summary>
    /// Das Datenverzeichnis.
    /// </summary>
    public string DataDirectory
    {
        get { return _database.DataDirectory; }
    }

    /// <summary>
    /// Die Pfade, auf die <see cref="IDatabaseFileProtector.ProtectFile"/> angewendet wurde.
    /// </summary>
    public List<string> ProtectedFiles { get; } = [];

    /// <summary>
    /// Die Pfade, auf die <see cref="IDatabaseFileProtector.Protect"/> angewendet wurde.
    /// </summary>
    public List<string> ProtectedDatabases { get; } = [];

    /// <summary>
    /// Wie oft die Zwischenspeicher des Preisdienstes verworfen wurden.
    /// </summary>
    public int CacheInvalidations { get; private set; }

    /// <summary>
    /// Das Verzeichnis der Sicherungen.
    /// </summary>
    public string BackupDirectory
    {
        get { return Path.Combine(DataDirectory, BackupFormat.BackupDirectoryName); }
    }

    /// <summary>
    /// Das Verzeichnis der Belegfotos.
    /// </summary>
    public string ReceiptsDirectory
    {
        get { return Path.Combine(DataDirectory, ReceiptPhotoStore.DirectoryName); }
    }

    /// <summary>
    /// Erstellt eine Kontext-Factory auf der Datenbank dieser Umgebung.
    /// </summary>
    /// <returns>Die Factory.</returns>
    public IDbContextFactory<TankradarDbContext> CreateFactory()
    {
        return _database.CreateFactory();
    }

    /// <summary>
    /// Erstellt den Dienst; <paramref name="swapHook"/> dient der Fehlerinjektion beim Austausch.
    /// </summary>
    /// <param name="swapHook">Wird nach jedem Schritt des Austauschs aufgerufen; optional.</param>
    /// <param name="prepareTimeout">Die Höchstdauer der Datenbankprüfung; optional.</param>
    /// <returns>Der Dienst.</returns>
    public BackupService CreateService(Action<string>? swapHook = null, TimeSpan? prepareTimeout = null)
    {
        var factory = _database.CreateFactory();
        var paths = new Paths(DataDirectory);
        var protector = new RecordingProtector(this);
        var initializer = new DatabaseInitializer(factory, paths, protector, new RestoreRecovery());
        return new BackupService(factory, paths, protector, initializer, TimeProvider.System, new RecordingLogger<BackupService>(), new CacheCounter(this), swapHook, prepareTimeout);
    }

    /// <summary>
    /// Legt Beispieldaten in allen Tabellen an und ein Belegfoto im Datenverzeichnis.
    /// </summary>
    /// <param name="photoBytes">Der Inhalt des Fotos (muss mit einer JPEG-Signatur beginnen).</param>
    /// <param name="vehicleName">Der Name des Fahrzeugs.</param>
    /// <returns>Eine Aufgabe.</returns>
    public async Task SeedAsync(byte[] photoBytes, string vehicleName = "Golf")
    {
        await _database.CreateInitializer(_database.CreateFactory()).InitializeAsync();
        Directory.CreateDirectory(ReceiptsDirectory);
        await File.WriteAllBytesAsync(Path.Combine(ReceiptsDirectory, PhotoName), photoBytes);
        await using var context = _database.CreateFactory().CreateDbContext();
        var settings = await context.UserSettings.SingleOrDefaultAsync() ?? context.UserSettings.Add(new UserSettingsEntity()).Entity;
        settings.GpsUsage = "Never";
        settings.ResultView = "Map";
        settings.ResultSortOrder = "Name";
        settings.ShowNearby = false;
        context.FuelTypeSettings.Add(new FuelTypeSettingEntity { FuelTypeKey = "Diesel", SortOrder = 0, IsSelected = true });
        var station = new StationEntity
        {
            Id = StationId,
            Name = "Alpha Tankstelle",
            Brand = "Alpha",
            Place = "Berlin",
            Latitude = 52.5,
            Longitude = 13.4,
            WholeDay = true,
            OpeningTimesJson = "[]",
            DetailsUpdatedUtc = new DateTime(2026, 10, 5, 10, 0, 0, DateTimeKind.Utc),
        };
        context.Stations.Add(station);
        context.PriceEntries.Add(new PriceEntryEntity { Station = station, FuelTypeKey = "Diesel", Price = 1.699m, RetrievedUtc = new DateTime(2026, 10, 5, 10, 0, 7, DateTimeKind.Utc) });
        var group = new FavoriteGroupEntity { Name = "Arbeitsweg", Description = "Täglich", CreatedUtc = new DateTime(2026, 10, 5, 9, 0, 0, DateTimeKind.Utc) };
        context.FavoriteGroups.Add(group);
        context.FavoriteEntries.Add(new FavoriteEntryEntity { Group = group, Station = station, Note = "Günstig", Priority = "High", AddedUtc = new DateTime(2026, 10, 5, 9, 5, 0, DateTimeKind.Utc) });
        var vehicle = new VehicleEntity { Name = vehicleName, FuelKind = "Diesel", StartOdometerKm = 84350, StartConsumption = 5.4m, CreatedUtc = new DateTime(2026, 10, 11, 9, 0, 0, DateTimeKind.Utc) };
        context.Vehicles.Add(vehicle);
        context.TankbookEntries.Add(new TankbookEntryEntity
        {
            Vehicle = vehicle,
            CreatedUtc = new DateTime(2026, 10, 11, 10, 0, 0, DateTimeKind.Utc),
            OccurredAt = new DateTime(2026, 10, 11, 12, 30, 0),
            Kind = "Fuel",
            Quantity = 40.123m,
            UnitPrice = 1.699m,
            TotalAmount = 68.17m,
            OdometerKm = 84900,
            StationName = "Alpha Tankstelle",
            StationId = StationId,
            Note = "Belegfoto",
            ReceiptPhotoFileName = PhotoName,
        });
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Liest die Namen aller Fahrzeuge.
    /// </summary>
    /// <returns>Die Namen, aufsteigend.</returns>
    public async Task<List<string>> GetVehicleNamesAsync()
    {
        await using var context = _database.CreateFactory().CreateDbContext();
        return await context.Vehicles.OrderBy(v => v.Name).Select(v => v.Name).ToListAsync();
    }

    /// <summary>
    /// Berechnet eine Momentaufnahme der Livedaten (Inhalt aller Tabellen und Fotos) zum Vergleich; die Datenbankdatei selbst ändert sich schon durch das Öffnen (Migrationssperre), ihr Inhalt nicht.
    /// </summary>
    /// <returns>Die Prüfsummen je Datei.</returns>
    public Dictionary<string, string> Snapshot()
    {
        var result = new Dictionary<string, string>();
        var database = Path.Combine(DataDirectory, TankradarDbContext.DatabaseFileName);
        if (File.Exists(database))
        {
            result["db"] = BackupTestArchive.Hash(System.Text.Encoding.UTF8.GetBytes(DumpTables(database)));
        }

        if (Directory.Exists(ReceiptsDirectory))
        {
            foreach (var photo in Directory.EnumerateFiles(ReceiptsDirectory))
            {
                result["receipts/" + Path.GetFileName(photo)] = BackupTestArchive.Hash(File.ReadAllBytes(photo));
            }
        }

        return result;
    }

    private static string DumpTables(string database)
    {
        var dump = new System.Text.StringBuilder();
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection(TankradarDbContext.BuildConnectionString(database, pooling: false));
        connection.Open();
        var tables = new List<string>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '%Lock' ORDER BY name";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                tables.Add(reader.GetString(0));
            }
        }

        foreach (var table in tables)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM \"" + table + "\" ORDER BY 1";
            using var reader = command.ExecuteReader();
            dump.Append(table).Append("\n");
            while (reader.Read())
            {
                for (var column = 0; column < reader.FieldCount; column++)
                {
                    dump.Append(Convert.ToString(reader.GetValue(column), System.Globalization.CultureInfo.InvariantCulture)).Append('|');
                }

                dump.Append("\n");
            }
        }

        return dump.ToString();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _database.Dispose();
    }

    private sealed class Paths : IAppDataPathProvider
    {
        private readonly string _directory;

        public Paths(string directory)
        {
            _directory = directory;
        }

        public string GetDataDirectory()
        {
            return _directory;
        }

        public string GetCacheDirectory()
        {
            return Path.Combine(_directory, "cache");
        }
    }

    private sealed class RecordingProtector : IDatabaseFileProtector
    {
        private readonly BackupTestEnvironment _owner;

        public RecordingProtector(BackupTestEnvironment owner)
        {
            _owner = owner;
        }

        public void Protect(string databasePath)
        {
            _owner.ProtectedDatabases.Add(databasePath);
        }

        public void ProtectFile(string filePath)
        {
            _owner.ProtectedFiles.Add(filePath);
        }
    }

    private sealed class CacheCounter : IPriceCacheInvalidator
    {
        private readonly BackupTestEnvironment _owner;

        public CacheCounter(BackupTestEnvironment owner)
        {
            _owner = owner;
        }

        public void InvalidateCaches()
        {
            _owner.CacheInvalidations++;
        }
    }
}

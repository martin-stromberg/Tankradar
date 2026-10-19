using System.Text;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services;
using Tankradar.MAUI.Services.Export;
using Tankradar.MAUI.Services.Tankbook;
using Tankradar.Tests.Integration.Integration.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft den <see cref="TankbookExportService"/> auf einer echten, migrierten SQLite-Datei im Zusammenspiel mit <see cref="VehicleService"/> und <see cref="TankbookEntryService"/>:
/// CSV und PDF, Zeitraum, Hybridfahrzeug und Fahrzeug ohne Vorgänge; die gespeicherten Daten bleiben unverändert.
/// </summary>
public class TankbookExportServiceTests_RealDatabase : IDisposable
{
    private static readonly TimeProvider Clock = new FixedClock(new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero));

    private readonly TestDatabase _database = new();

    private VehicleService CreateVehicleService()
    {
        var factory = _database.CreateFactory();
        return new VehicleService(factory, _database.CreateInitializer(factory), _database.CreatePhotoStore(), TimeProvider.System, new RecordingLogger<VehicleService>());
    }

    private TankbookEntryService CreateEntryService()
    {
        var factory = _database.CreateFactory();
        return new TankbookEntryService(factory, _database.CreateInitializer(factory), _database.CreatePhotoStore(), TimeProvider.System, new RecordingLogger<TankbookEntryService>());
    }

    private TankbookExportService CreateExportService()
    {
        return new TankbookExportService(CreateVehicleService(), CreateEntryService(), new CachePaths(_database.DataDirectory), Clock, new RecordingLogger<TankbookExportService>());
    }

    private async Task<Vehicle> SeedAsync(string name = "Käfer", VehicleFuelKind fuelKind = VehicleFuelKind.Diesel)
    {
        var vehicle = (await CreateVehicleService().CreateVehicleAsync(new VehicleInput(name, fuelKind, 10_000, null))).Vehicle!;
        var service = CreateEntryService();
        var entries = new[]
        {
            new TankbookEntryInput(new DateTime(2026, 4, 30, 23, 59, 0), TankbookEntryKind.Fuel, 40m, 1.859m, 74.36m, 10_500, false, "Müller Süd", null, "Urlaub; Italien", null),
            new TankbookEntryInput(new DateTime(2026, 5, 10, 8, 0, 0), TankbookEntryKind.Fuel, 10m, 1.9m, 19m, 10_700, true, "Alpha Tankstelle", null, null, null),
            new TankbookEntryInput(new DateTime(2026, 5, 20, 8, 0, 0), TankbookEntryKind.Fuel, 20m, 1.859m, 37.18m, 11_000, false, "Alpha Tankstelle", null, null, null),
        };
        foreach (var entry in entries)
        {
            Assert.Equal(TankbookEntryResult.Ok, (await service.CreateEntryAsync(vehicle.Id, entry)).Result);
        }

        return vehicle;
    }

    /// <summary>
    /// Prüft den Export aller Vorgänge als CSV aus der echten Datenbank: Kopfzeile, drei Zeilen, Umlaute, maskiertes Semikolon, Verbrauch.
    /// </summary>
    [Fact]
    public async Task Export_Csv_FromRealDatabase_ContainsAllEntries()
    {
        var vehicle = await SeedAsync();

        var result = await CreateExportService().CreateAsync(vehicle.Id, new TankbookExportRequest(TankbookExportFormat.Csv, null, null));

        Assert.Equal(TankbookExportStatus.Exported, result.Status);
        Assert.Equal("Tankbuch_Käfer_2026-06-10.csv", result.FileName);
        var lines = Encoding.UTF8.GetString(await File.ReadAllBytesAsync(result.FilePath!)).TrimStart('﻿').Split("\r\n");
        Assert.Equal(5, lines.Length);
        Assert.Equal("30.04.2026;23:59;Müller Süd;Kraftstoff;40,00;Liter;1,859;74,36;10500;Nein;500;8,00;L/100 km;ab Startstand;\"Urlaub; Italien\"", lines[1]);
        Assert.StartsWith("10.05.2026;08:00;Alpha Tankstelle;Kraftstoff;10,00;Liter;1,90;19,00;10700;Ja;;;;;", lines[2], StringComparison.Ordinal);
        Assert.StartsWith("20.05.2026;08:00;Alpha Tankstelle;Kraftstoff;20,00;Liter;1,859;37,18;11000;Nein;500;6,00;L/100 km;;", lines[3], StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft den Zeitraum Mai: Der Vorgang vom 30.04. fehlt, der erste Vorgang im Zeitraum behält seinen Verbrauch.
    /// </summary>
    [Fact]
    public async Task Export_Csv_WithRange_FiltersAndKeepsConsumptionFromPredecessor()
    {
        var vehicle = await SeedAsync();

        var result = await CreateExportService().CreateAsync(vehicle.Id, new TankbookExportRequest(TankbookExportFormat.Csv, new DateTime(2026, 5, 1), new DateTime(2026, 5, 31)));

        Assert.Equal(2, result.EntryCount);
        var lines = Encoding.UTF8.GetString(await File.ReadAllBytesAsync(result.FilePath!)).TrimStart('﻿').Split("\r\n");
        Assert.DoesNotContain(lines, line => line.Contains("30.04.2026", StringComparison.Ordinal));
        Assert.Contains(";500;6,00;L/100 km;;", lines[2], StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft den PDF-Export aus der echten Datenbank: gültiger Aufbau und Inhalt.
    /// </summary>
    [Fact]
    public async Task Export_Pdf_FromRealDatabase_IsValidPdfWithContent()
    {
        var vehicle = await SeedAsync();

        var result = await CreateExportService().CreateAsync(vehicle.Id, new TankbookExportRequest(TankbookExportFormat.Pdf, null, null));

        var text = Encoding.Latin1.GetString(await File.ReadAllBytesAsync(result.FilePath!));
        Assert.StartsWith("%PDF-1.4", text, StringComparison.Ordinal);
        Assert.EndsWith("%%EOF\n", text, StringComparison.Ordinal);
        Assert.Contains("(M\\374ller S\\374d)", text, StringComparison.Ordinal);
        Assert.Contains("6,00 L/100 km", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft ein Hybridfahrzeug mit Kraftstoff- und Stromvorgängen: Beide Einheiten stehen in der Datei.
    /// </summary>
    [Fact]
    public async Task Export_Csv_Hybrid_ContainsLiterAndKwh()
    {
        var vehicle = (await CreateVehicleService().CreateVehicleAsync(new VehicleInput("Plug-in", VehicleFuelKind.Hybrid, 1_000, null))).Vehicle!;
        var service = CreateEntryService();
        Assert.Equal(TankbookEntryResult.Ok, (await service.CreateEntryAsync(vehicle.Id, new TankbookEntryInput(new DateTime(2026, 5, 1, 8, 0, 0), TankbookEntryKind.Fuel, 30m, 1.8m, 54m, 1_500, false, "Alpha Tankstelle", null, null, null))).Result);
        Assert.Equal(TankbookEntryResult.Ok, (await service.CreateEntryAsync(vehicle.Id, new TankbookEntryInput(new DateTime(2026, 5, 2, 8, 0, 0), TankbookEntryKind.Electricity, 15m, 0.4m, 6m, 1_600, false, "Zuhause", null, null, null))).Result);

        var result = await CreateExportService().CreateAsync(vehicle.Id, new TankbookExportRequest(TankbookExportFormat.Csv, null, null));

        var text = Encoding.UTF8.GetString(await File.ReadAllBytesAsync(result.FilePath!));
        Assert.Contains(";Kraftstoff;30,00;Liter;", text, StringComparison.Ordinal);
        Assert.Contains(";Strom;15,00;kWh;", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass ein Fahrzeug ohne Vorgänge keine Datei erzeugt und der Export die gespeicherten Daten nicht verändert.
    /// </summary>
    [Fact]
    public async Task Export_VehicleWithoutEntries_CreatesNoFile_AndDataStaysUnchanged()
    {
        var empty = (await CreateVehicleService().CreateVehicleAsync(new VehicleInput("Leer", VehicleFuelKind.Petrol, 0, null))).Vehicle!;
        var vehicle = await SeedAsync();
        var before = await CreateEntryService().GetEntriesAsync(vehicle.Id);
        var service = CreateExportService();

        var none = await service.CreateAsync(empty.Id, new TankbookExportRequest(TankbookExportFormat.Csv, null, null));
        var created = await service.CreateAsync(vehicle.Id, new TankbookExportRequest(TankbookExportFormat.Pdf, null, null));

        Assert.Equal(TankbookExportStatus.NoEntries, none.Status);
        Assert.Null(none.FilePath);
        Assert.Equal(before.Select(overview => overview.Entry), (await CreateEntryService().GetEntriesAsync(vehicle.Id)).Select(overview => overview.Entry));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _database.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class CachePaths : IAppDataPathProvider
    {
        private readonly string _directory;

        public CachePaths(string directory)
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

    private sealed class FixedClock : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedClock(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }
    }
}

using System.Text;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Export;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Ergebnisse des <see cref="TankbookExportService"/> für alle Werte von <see cref="TankbookExportStatus"/> außer dem Fehlerfall sowie beide Formate.
/// </summary>
public class TankbookExportServiceTests_Result : BaseTest
{
    private readonly TankbookEntryFixture _fixture = new();

    private async Task<Vehicle> CreateVehicleWithEntriesAsync(int count = 3)
    {
        var vehicle = await _fixture.CreateVehicleAsync("Golf");
        for (var index = 0; index < count; index++)
        {
            await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(index - count - 1), 10_500 + (index * 500)));
        }

        return vehicle;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fixture.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Prüft den Erfolg im Format CSV: Datei unter dem Zwischenspeicher mit BOM und Zeilen.
    /// </summary>
    [Fact]
    public async Task Create_Csv_WritesFileInCacheDirectory()
    {
        var vehicle = await CreateVehicleWithEntriesAsync();
        var service = _fixture.CreateExportService();

        var result = await service.CreateAsync(vehicle.Id, new TankbookExportRequest(TankbookExportFormat.Csv, null, null));

        Assert.Equal(TankbookExportStatus.Exported, result.Status);
        Assert.Equal(3, result.EntryCount);
        Assert.Equal($"Tankbuch_Golf_{_fixture.Now:yyyy-MM-dd}.csv", result.FileName);
        Assert.Equal(_fixture.Temp.Combine("cache", "exports", result.FileName!), result.FilePath);
        var bytes = await File.ReadAllBytesAsync(result.FilePath!);
        Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3]);
        Assert.Equal(5, Encoding.UTF8.GetString(bytes).Split("\r\n").Length);
    }

    /// <summary>
    /// Prüft den Erfolg im Format PDF: gültiger Kopf und Abschluss.
    /// </summary>
    [Fact]
    public async Task Create_Pdf_WritesPdfFile()
    {
        var vehicle = await CreateVehicleWithEntriesAsync();
        var service = _fixture.CreateExportService();

        var result = await service.CreateAsync(vehicle.Id, new TankbookExportRequest(TankbookExportFormat.Pdf, null, null));

        Assert.Equal(TankbookExportStatus.Exported, result.Status);
        Assert.EndsWith(".pdf", result.FileName, StringComparison.Ordinal);
        var text = Encoding.Latin1.GetString(await File.ReadAllBytesAsync(result.FilePath!));
        Assert.StartsWith("%PDF-1.4", text, StringComparison.Ordinal);
        Assert.EndsWith("%%EOF\n", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass ein unbekanntes Fahrzeug <see cref="TankbookExportStatus.VehicleNotFound"/> liefert und keine Datei entsteht.
    /// </summary>
    [Fact]
    public async Task Create_UnknownVehicle_ReturnsVehicleNotFound()
    {
        var service = _fixture.CreateExportService();

        var result = await service.CreateAsync(4711, new TankbookExportRequest(TankbookExportFormat.Csv, null, null));

        Assert.Equal(TankbookExportStatus.VehicleNotFound, result.Status);
        Assert.Null(result.FilePath);
        Assert.False(Directory.Exists(_fixture.Temp.Combine("cache", "exports")));
    }

    /// <summary>
    /// Prüft, dass ein Fahrzeug ohne Vorgänge <see cref="TankbookExportStatus.NoEntries"/> liefert und keine Datei entsteht.
    /// </summary>
    [Fact]
    public async Task Create_VehicleWithoutEntries_ReturnsNoEntries()
    {
        var vehicle = await _fixture.CreateVehicleAsync("Leer");
        var service = _fixture.CreateExportService();

        var result = await service.CreateAsync(vehicle.Id, new TankbookExportRequest(TankbookExportFormat.Pdf, null, null));

        Assert.Equal(TankbookExportStatus.NoEntries, result.Status);
        Assert.Equal(0, result.EntryCount);
        Assert.Null(result.FileName);
        Assert.False(Directory.Exists(_fixture.Temp.Combine("cache", "exports")));
    }

    /// <summary>
    /// Prüft, dass ein Zeitraum ohne Vorgänge <see cref="TankbookExportStatus.NoEntries"/> liefert.
    /// </summary>
    [Fact]
    public async Task Create_RangeWithoutEntries_ReturnsNoEntries()
    {
        var vehicle = await CreateVehicleWithEntriesAsync();
        var service = _fixture.CreateExportService();
        var longAgo = new DateTime(2000, 1, 1);

        var result = await service.CreateAsync(vehicle.Id, new TankbookExportRequest(TankbookExportFormat.Csv, longAgo, longAgo.AddDays(30)));

        Assert.Equal(TankbookExportStatus.NoEntries, result.Status);
    }

    /// <summary>
    /// Prüft, dass ein Zeitraum mit Start nach Ende <see cref="TankbookExportStatus.InvalidRange"/> liefert.
    /// </summary>
    [Fact]
    public async Task Create_StartAfterEnd_ReturnsInvalidRange()
    {
        var vehicle = await CreateVehicleWithEntriesAsync();
        var service = _fixture.CreateExportService();

        var result = await service.CreateAsync(vehicle.Id, new TankbookExportRequest(TankbookExportFormat.Csv, _fixture.Now.Date, _fixture.Now.Date.AddDays(-1)));

        Assert.Equal(TankbookExportStatus.InvalidRange, result.Status);
    }

    /// <summary>
    /// Prüft, dass ein Enddatum in der Zukunft <see cref="TankbookExportStatus.InvalidRange"/> liefert, heute aber gültig ist.
    /// </summary>
    [Fact]
    public async Task Create_EndInFuture_ReturnsInvalidRange_TodayIsValid()
    {
        var vehicle = await CreateVehicleWithEntriesAsync();
        var service = _fixture.CreateExportService();

        var future = await service.CreateAsync(vehicle.Id, new TankbookExportRequest(TankbookExportFormat.Csv, null, _fixture.Now.Date.AddDays(1)));
        var today = await service.CreateAsync(vehicle.Id, new TankbookExportRequest(TankbookExportFormat.Csv, null, _fixture.Now.Date));

        Assert.Equal(TankbookExportStatus.InvalidRange, future.Status);
        Assert.Equal(TankbookExportStatus.Exported, today.Status);
    }

    /// <summary>
    /// Prüft, dass der Zeitraum die Zeilen der Datei einschränkt.
    /// </summary>
    [Fact]
    public async Task Create_Range_LimitsRows()
    {
        var vehicle = await CreateVehicleWithEntriesAsync(3);
        var service = _fixture.CreateExportService();
        var newest = _fixture.Now.Date.AddDays(-2);

        var result = await service.CreateAsync(vehicle.Id, new TankbookExportRequest(TankbookExportFormat.Csv, newest, newest));

        Assert.Equal(TankbookExportStatus.Exported, result.Status);
        Assert.Equal(1, result.EntryCount);
    }
}

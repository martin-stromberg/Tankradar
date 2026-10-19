using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Export;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Ablage, Bereinigung alter Dateien und <see cref="ITankbookExportService.Discard"/> des <see cref="TankbookExportService"/>.
/// </summary>
public class TankbookExportServiceTests_Files : BaseTest
{
    private readonly TankbookEntryFixture _fixture = new();

    private async Task<Vehicle> CreateVehicleWithEntryAsync()
    {
        var vehicle = await _fixture.CreateVehicleAsync("Golf");
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-1), 10_500));
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
    /// Prüft, dass vor einem Export alte Dateien im Exportverzeichnis gelöscht werden.
    /// </summary>
    [Fact]
    public async Task Create_RemovesOldExportFiles()
    {
        var vehicle = await CreateVehicleWithEntryAsync();
        var directory = _fixture.Temp.Combine("cache", "exports");
        Directory.CreateDirectory(directory);
        var old = Path.Combine(directory, "Tankbuch_Alt_2020-01-01.csv");
        await File.WriteAllTextAsync(old, "alt");

        var result = await _fixture.CreateExportService().CreateAsync(vehicle.Id, new TankbookExportRequest(TankbookExportFormat.Csv, null, null));

        Assert.False(File.Exists(old));
        Assert.Equal(result.FilePath, Assert.Single(Directory.GetFiles(directory)));
    }

    /// <summary>
    /// Prüft, dass Discard die Datei löscht und bei fehlender Datei ohne Fehler bleibt.
    /// </summary>
    [Fact]
    public async Task Discard_DeletesFile_AndIgnoresMissingFile()
    {
        var vehicle = await CreateVehicleWithEntryAsync();
        var service = _fixture.CreateExportService();
        var result = await service.CreateAsync(vehicle.Id, new TankbookExportRequest(TankbookExportFormat.Csv, null, null));

        service.Discard(result.FilePath!);
        service.Discard(result.FilePath!);

        Assert.False(File.Exists(result.FilePath));
    }

    /// <summary>
    /// Prüft, dass ein zweiter Export am selben Tag die erste Datei ersetzt.
    /// </summary>
    [Fact]
    public async Task Create_Twice_LeavesOneFile()
    {
        var vehicle = await CreateVehicleWithEntryAsync();
        var service = _fixture.CreateExportService();

        await service.CreateAsync(vehicle.Id, new TankbookExportRequest(TankbookExportFormat.Csv, null, null));
        await service.CreateAsync(vehicle.Id, new TankbookExportRequest(TankbookExportFormat.Pdf, null, null));

        var file = Assert.Single(Directory.GetFiles(_fixture.Temp.Combine("cache", "exports")));
        Assert.EndsWith(".pdf", file, StringComparison.Ordinal);
    }
}

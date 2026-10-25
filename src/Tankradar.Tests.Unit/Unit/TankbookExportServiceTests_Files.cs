using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Export;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Ablage, Bereinigung alter Dateien und Ausweichnamen des <see cref="TankbookExportService"/>.
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
    /// Prüft, dass die fertige Exportdatei den Dateischutz des Betriebssystems erhält (personenbezogene Tankbuchdaten).
    /// </summary>
    [Fact]
    public async Task Create_ProtectsFinishedFile()
    {
        var vehicle = await CreateVehicleWithEntryAsync();

        var result = await _fixture.CreateExportService().CreateAsync(vehicle.Id, new TankbookExportRequest(TankbookExportFormat.Csv, null, null));

        Assert.Equal(TankbookExportStatus.Exported, result.Status);
        Assert.Equal(result.FilePath, Assert.Single(_fixture.ExportProtector.ProtectedFiles));
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
    /// Prüft, dass eine noch gesperrte Vorversion (geöffnet, ohne Mitbenutzung) nicht zum Fehler führt: Der zweite Export erhält einen Zähler im Namen.
    /// </summary>
    [Fact]
    public async Task Create_PreviousFileLocked_UsesNumberedName()
    {
        var vehicle = await CreateVehicleWithEntryAsync();
        var service = _fixture.CreateExportService();
        var first = await service.CreateAsync(vehicle.Id, new TankbookExportRequest(TankbookExportFormat.Csv, null, null));
        await using var lockHandle = new FileStream(first.FilePath!, FileMode.Open, FileAccess.Read, FileShare.None);

        var second = await service.CreateAsync(vehicle.Id, new TankbookExportRequest(TankbookExportFormat.Csv, null, null));
        var third = await service.CreateAsync(vehicle.Id, new TankbookExportRequest(TankbookExportFormat.Csv, null, null));

        Assert.Equal(TankbookExportStatus.Exported, second.Status);
        Assert.EndsWith("_2.csv", second.FileName, StringComparison.Ordinal);
        Assert.Equal(TankbookExportStatus.Exported, third.Status);
        Assert.EndsWith("_2.csv", third.FileName, StringComparison.Ordinal);
        Assert.True(File.Exists(second.FilePath));
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

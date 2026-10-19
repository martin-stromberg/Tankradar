using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Export;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Fehlerfall <see cref="TankbookExportStatus.Failed"/> des <see cref="TankbookExportService"/>: Schreibfehler, Protokoll nur mit Ausnahmetyp.
/// </summary>
public class TankbookExportServiceTests_Failure : BaseTest
{
    private readonly TankbookEntryFixture _fixture = new();

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
    /// Prüft, dass ein Schreibfehler (das Exportverzeichnis ist durch eine Datei belegt) als <see cref="TankbookExportStatus.Failed"/> gemeldet wird
    /// und das Protokoll weder Namen noch Werte enthält.
    /// </summary>
    [Fact]
    public async Task Create_WriteError_ReturnsFailedAndLogsOnlyExceptionType()
    {
        var vehicle = await _fixture.CreateVehicleAsync("Geheimname");
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-1), 10_500, stationName: "Geheime Tankstelle"));
        Directory.CreateDirectory(_fixture.Temp.Combine("cache"));
        await File.WriteAllTextAsync(_fixture.Temp.Combine("cache", "exports"), "blockiert");

        var result = await _fixture.CreateExportService().CreateAsync(vehicle.Id, new TankbookExportRequest(TankbookExportFormat.Csv, null, null));

        Assert.Equal(TankbookExportStatus.Failed, result.Status);
        Assert.Null(result.FilePath);
        var log = _fixture.ExportLogger.AllText;
        Assert.Contains("IOException", log, StringComparison.Ordinal);
        Assert.DoesNotContain("Geheim", log, StringComparison.Ordinal);
        Assert.DoesNotContain("blockiert", log, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass ein Abbruch nicht als Fehler verschluckt wird.
    /// </summary>
    [Fact]
    public async Task Create_Cancelled_ThrowsOperationCanceled()
    {
        var vehicle = await _fixture.CreateVehicleAsync("Golf");
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-1), 10_500));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _fixture.CreateExportService().CreateAsync(vehicle.Id, new TankbookExportRequest(TankbookExportFormat.Csv, null, null), cts.Token));
    }
}

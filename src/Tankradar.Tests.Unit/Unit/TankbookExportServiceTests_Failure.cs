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
    /// Prüft, dass ein Abbruch nach dem Anlegen der Datei (während des Schreibens) die angefangene Datei löscht und die Ausnahme weitergibt.
    /// </summary>
    [Fact]
    public async Task Create_CancelledAfterFileCreation_DeletesPartialFile()
    {
        var vehicle = await _fixture.CreateVehicleAsync("Golf");
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-1), 10_500));
        using var cts = new CancellationTokenSource();
        var service = new TankbookExportService(_fixture.Vehicles, _fixture.Service, new CancellingPaths(_fixture.Temp.Path, cts), _fixture.ExportProtector, _fixture.Clock, _fixture.ExportLogger);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.CreateAsync(vehicle.Id, new TankbookExportRequest(TankbookExportFormat.Csv, null, null), cts.Token));

        Assert.Empty(Directory.GetFiles(_fixture.Temp.Combine("cache", "exports")));
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

    private sealed class CancellingPaths : MAUI.Services.IAppDataPathProvider
    {
        private readonly string _directory;
        private readonly CancellationTokenSource _source;

        public CancellingPaths(string directory, CancellationTokenSource source)
        {
            _directory = directory;
            _source = source;
        }

        public string GetDataDirectory()
        {
            return _directory;
        }

        public string GetCacheDirectory()
        {
            // Wird nach dem Laden der Vorgänge und vor dem Anlegen der Datei aufgerufen: Der Abbruch trifft das Schreiben.
            _source.Cancel();
            return Path.Combine(_directory, "cache");
        }
    }
}

using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Gemeinsamer Aufbau der Tests des <see cref="TankbookExportViewModel"/>: echter Exportdienst auf In-Memory-SQLite und Zwischenspeicher im temporären Verzeichnis,
/// ersetzte Weitergabe und Ansage.
/// </summary>
public abstract class TankbookExportViewModelTestBase : BaseTest
{
    /// <summary>
    /// Erstellt den Aufbau.
    /// </summary>
    protected TankbookExportViewModelTestBase()
    {
        ViewModel = Fixture.CreateExport(Sharer, Announcer, Logger);
    }

    /// <summary>
    /// Die Testumgebung.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected TankbookEntryFixture Fixture { get; } = new();

    /// <summary>
    /// Die ersetzte Weitergabe.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeBackupSharer Sharer { get; } = new();

    /// <summary>
    /// Die Ansage.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeScreenReaderAnnouncer Announcer { get; } = new();

    /// <summary>
    /// Der Logger.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected RecordingLogger<TankbookExportViewModel> Logger { get; } = new();

    /// <summary>
    /// Das zu testende ViewModel.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected TankbookExportViewModel ViewModel { get; }

    /// <summary>
    /// Legt ein Fahrzeug mit zwei Vorgängen an und übergibt es dem ViewModel.
    /// </summary>
    /// <returns>Das Fahrzeug.</returns>
    protected async Task<Vehicle> PrepareVehicleAsync()
    {
        var vehicle = await Fixture.CreateVehicleAsync("Golf");
        await Fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(Fixture.Now.AddDays(-20), 10_500));
        await Fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(Fixture.Now.AddDays(-2), 11_000, 30m));
        ViewModel.SetVehicle(vehicle);
        return vehicle;
    }

    /// <summary>
    /// Löst <b>Exportieren</b> aus und wartet den Vorgang ab.
    /// </summary>
    /// <returns>Ein Task, der nach dem Vorgang abgeschlossen ist.</returns>
    protected async Task ExportAsync()
    {
        ViewModel.ExportCommand.Execute(null);
        await ViewModel.LastTask;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Fixture.Dispose();
        }

        base.Dispose(disposing);
    }
}

using System.Windows.Input;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Gemeinsamer Aufbau der Tests des Tabs „Tankbuch“ (Tank- und Ladevorgänge): echte Dienste auf In-Memory-SQLite, Fotoablage- und Auswahl-Doubles, Ansage,
/// Navigation und gemerkte Fahrzeugauswahl (über <see cref="FakePreferences"/>).
/// </summary>
public abstract class TankbookEntriesViewModelTestBase : BaseTest
{
    private IVehicleSelectionStore? _selectionStore;

    /// <summary>
    /// Die Testumgebung.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected TankbookEntryFixture Fixture { get; } = new();

    /// <summary>
    /// Die Ansage.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeScreenReaderAnnouncer Announcer { get; } = new();

    /// <summary>
    /// Die Fotoauswahl.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeReceiptPhotoPicker Picker { get; } = new();

    /// <summary>
    /// Die Navigation.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeTankbookNavigator Navigator { get; } = new();

    /// <summary>
    /// Die gerätelokalen Einstellungen hinter der gemerkten Fahrzeugauswahl.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakePreferences Preferences { get; } = new();

    /// <summary>
    /// Die gemerkte Fahrzeugauswahl.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected IVehicleSelectionStore SelectionStore
    {
        get { return _selectionStore ??= new PreferencesVehicleSelectionStore(Preferences); }
    }

    /// <summary>
    /// Der Logger der Seite.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected RecordingLogger<TankbookEntriesViewModel> Logger { get; } = new();

    /// <summary>
    /// Der Logger des Formulars.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected RecordingLogger<TankbookEntryFormViewModel> FormLogger { get; } = new();

    /// <summary>
    /// Der Logger der Auswertung.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected RecordingLogger<TankbookStatisticsViewModel> StatisticsLogger { get; } = new();

    /// <summary>
    /// Der Logger des Exports.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected RecordingLogger<TankbookExportViewModel> ExportLogger { get; } = new();

    /// <summary>
    /// Die Weitergabe des Exports.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeBackupSharer Sharer { get; } = new();

    /// <summary>
    /// Merkt sich das Fahrzeug als gewählt, öffnet den Tab und wartet das Laden ab. Die Ansage des Fahrzeugs beim Öffnen wird verworfen, damit Tests nur
    /// die Ansagen ihrer eigenen Schritte sehen (die Ansage beim Öffnen prüft <see cref="TankbookEntriesViewModelTests_Selection"/>).
    /// </summary>
    /// <param name="vehicleId">Die Kennung des Fahrzeugs.</param>
    /// <returns>Das ViewModel.</returns>
    protected async Task<TankbookEntriesViewModel> OpenAsync(long vehicleId)
    {
        return await OpenAsync(vehicleId, Fixture.Service);
    }

    /// <summary>
    /// Merkt sich das Fahrzeug als gewählt, öffnet den Tab mit dem angegebenen Dienst der Vorgänge und wartet das Laden ab; die Ansage beim Öffnen wird verworfen.
    /// </summary>
    /// <param name="vehicleId">Die Kennung des Fahrzeugs.</param>
    /// <param name="entryService">Der Dienst der Vorgänge.</param>
    /// <returns>Das ViewModel.</returns>
    protected async Task<TankbookEntriesViewModel> OpenAsync(long vehicleId, ITankbookEntryService entryService)
    {
        SelectionStore.Set(vehicleId);
        var viewModel = CreateViewModel(entryService);
        viewModel.OnAppearing();
        await viewModel.LastTask;
        if (viewModel.HasVehicle)
        {
            Announcer.Announcements.Clear();
        }

        return viewModel;
    }

    /// <summary>
    /// Erstellt das ViewModel des Tabs, ohne es zu laden.
    /// </summary>
    /// <param name="entryService">Der Dienst der Vorgänge; ohne Angabe der echte Dienst.</param>
    /// <returns>Das ViewModel.</returns>
    protected TankbookEntriesViewModel CreateViewModel(ITankbookEntryService? entryService = null)
    {
        return new TankbookEntriesViewModel(
            Fixture.Vehicles,
            entryService ?? Fixture.Service,
            Fixture.Photos,
            Fixture.CreateForm(Picker, Announcer, FormLogger),
            Fixture.CreateStatistics(Announcer, StatisticsLogger),
            Fixture.CreateExport(Sharer, Announcer, ExportLogger),
            Fixture.Restore,
            Navigator,
            Announcer,
            SelectionStore,
            Logger);
    }

    /// <summary>
    /// Legt ein Fahrzeug an, öffnet die Seite und das Formular für einen neuen Vorgang.
    /// </summary>
    /// <returns>Die Seite mit geöffnetem Formular.</returns>
    protected async Task<TankbookEntriesViewModel> OpenFormAsync()
    {
        var vehicle = await CreateVehicleAsync();
        var viewModel = await OpenAsync(vehicle.Id);
        viewModel.AddEntryCommand.Execute(null);
        return viewModel;
    }

    /// <summary>
    /// Führt einen Befehl des Formulars aus und wartet Speichern und Neuladen ab.
    /// </summary>
    /// <param name="viewModel">Die Seite.</param>
    /// <param name="command">Der Befehl.</param>
    /// <returns>Ein Task, der nach dem Abschluss abgeschlossen ist.</returns>
    protected static async Task PressFormAsync(TankbookEntriesViewModel viewModel, ICommand command)
    {
        command.Execute(null);
        await viewModel.Form.LastTask;
        await viewModel.LastTask;
    }

    /// <summary>
    /// Führt einen Befehl der Seite oder eines Eintrags aus und wartet den gestarteten Vorgang ab.
    /// </summary>
    /// <param name="viewModel">Die Seite.</param>
    /// <param name="command">Der Befehl.</param>
    /// <returns>Ein Task, der nach dem Abschluss abgeschlossen ist.</returns>
    protected static async Task PressAsync(TankbookEntriesViewModel viewModel, ICommand command)
    {
        command.Execute(null);
        await viewModel.LastTask;
    }

    /// <summary>
    /// Füllt das Formular mit Menge, Preis und Kilometerstand und speichert.
    /// </summary>
    /// <param name="viewModel">Die Seite.</param>
    /// <param name="quantity">Die Menge.</param>
    /// <param name="unitPrice">Der Preis pro Einheit.</param>
    /// <param name="total">Der Gesamtbetrag.</param>
    /// <param name="odometer">Der Kilometerstand.</param>
    /// <param name="station">Der Name der Tankstelle.</param>
    /// <returns>Ein Task, der nach Speichern und Neuladen abgeschlossen ist.</returns>
    protected static async Task CreateViaFormAsync(TankbookEntriesViewModel viewModel, string quantity, string unitPrice, string total, string odometer, string station = "Alpha Tankstelle")
    {
        viewModel.AddEntryCommand.Execute(null);
        var form = viewModel.Form;
        form.StationName = station;
        form.Quantity = quantity;
        form.UnitPrice = unitPrice;
        form.TotalAmount = total;
        form.Odometer = odometer;
        await PressFormAsync(viewModel, form.SaveCommand);
    }

    /// <summary>
    /// Legt ein Fahrzeug an.
    /// </summary>
    /// <param name="fuelKind">Die Kraftstoffart.</param>
    /// <returns>Das Fahrzeug.</returns>
    protected Task<Vehicle> CreateVehicleAsync(VehicleFuelKind fuelKind = VehicleFuelKind.Diesel)
    {
        return Fixture.CreateVehicleAsync("Golf", fuelKind, 10_000);
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

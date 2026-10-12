using System.Windows.Input;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Navigation;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Gemeinsamer Aufbau der Tests der Seite „Tank- und Ladevorgänge“: echte Dienste auf In-Memory-SQLite, Fotoablage- und Auswahl-Doubles, Ansage und Navigation.
/// </summary>
public abstract class TankbookEntriesViewModelTestBase : BaseTest
{
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
    /// Öffnet die Seite eines Fahrzeugs wie über die Shell-Navigation und wartet das Laden ab.
    /// </summary>
    /// <param name="vehicleId">Die Kennung des Fahrzeugs.</param>
    /// <returns>Das ViewModel.</returns>
    protected async Task<TankbookEntriesViewModel> OpenAsync(long vehicleId)
    {
        var viewModel = new TankbookEntriesViewModel(
            Fixture.Vehicles,
            Fixture.Service,
            Fixture.Photos,
            Fixture.CreateForm(Picker, Announcer, FormLogger),
            Fixture.CreateStatistics(Announcer, StatisticsLogger),
            Navigator,
            Announcer,
            Logger);
        viewModel.ApplyQueryAttributes(new Dictionary<string, object> { [ShellTankbookNavigator.VehicleIdParameter] = vehicleId });
        viewModel.OnAppearing();
        await viewModel.LastTask;
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

using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Navigation;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass die geöffnete Seite <see cref="MAUI.ViewModels.TankbookEntriesViewModel"/> nach einer Wiederherstellung (H5) ohne erneutes Öffnen neu lädt.
/// </summary>
public class TankbookEntriesViewModelTests_Restore : TankbookEntriesViewModelTestBase
{
    /// <summary>
    /// Prüft, dass nach der Benachrichtigung Liste und Auswertung den geänderten Bestand zeigen.
    /// </summary>
    [Fact]
    public async Task Restored_ReloadsEntriesAndStatistics()
    {
        var vehicle = await CreateVehicleAsync();
        await Fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(Fixture.Now.AddDays(-2), 10_500, 40m));
        var viewModel = await OpenAsync(vehicle.Id);
        Assert.Single(viewModel.Entries);
        Assert.Equal("8,00", viewModel.Statistics.Kinds.Single().AverageValueText);

        await Fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(Fixture.Now.AddDays(-1), 11_000, 30m));
        Fixture.Restore.NotifyRestored();
        await viewModel.LastTask;

        Assert.Equal(2, viewModel.Entries.Count);
        Assert.Equal("6,00", viewModel.Statistics.Kinds.Single().AverageValueText);
    }

    /// <summary>
    /// Prüft, dass eine alte Rückmeldung des Exports nach der Wiederherstellung entfernt wird.
    /// </summary>
    [Fact]
    public async Task Restored_ClearsOldExportStatus()
    {
        var vehicle = await CreateVehicleAsync();
        await Fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(Fixture.Now.AddDays(-2), 10_500, 40m));
        var viewModel = await OpenAsync(vehicle.Id);
        await PressAsync(viewModel, viewModel.Export.ExportCommand);
        await viewModel.Export.LastTask;
        Assert.NotNull(viewModel.Export.StatusMessage);

        Fixture.Restore.NotifyRestored();
        await viewModel.LastTask;

        Assert.Null(viewModel.Export.StatusMessage);
    }

    /// <summary>
    /// Prüft, dass ein nach der Wiederherstellung nicht mehr vorhandenes Fahrzeug gemeldet und die Seite geschlossen wird.
    /// </summary>
    [Fact]
    public async Task Restored_VehicleGone_ClosesPage()
    {
        var vehicle = await CreateVehicleAsync();
        var viewModel = await OpenAsync(vehicle.Id);
        await Fixture.Vehicles.DeleteVehicleAsync(vehicle.Id, 0);

        Fixture.Restore.NotifyRestored();
        await viewModel.LastTask;

        Assert.Equal(1, Navigator.Closed);
        Assert.Equal([vehicle.Id], Navigator.ClosedVehicles);
        Assert.Empty(viewModel.Entries);
        Assert.Null(viewModel.Vehicle);
    }

    /// <summary>
    /// Prüft, dass ohne Fahrzeug „Exportieren“ ausgeblendet ist und „Vorgang erfassen“ kein Formular öffnet.
    /// </summary>
    [Fact]
    public async Task Restored_VehicleGone_HidesExportAndBlocksAdd()
    {
        var vehicle = await CreateVehicleAsync();
        var viewModel = await OpenAsync(vehicle.Id);
        Assert.True(viewModel.Export.HasVehicle);
        await Fixture.Vehicles.DeleteVehicleAsync(vehicle.Id, 0);

        Fixture.Restore.NotifyRestored();
        await viewModel.LastTask;
        await PressAsync(viewModel, viewModel.AddEntryCommand);
        await PressAsync(viewModel, viewModel.Export.ExportCommand);

        Assert.False(viewModel.HasVehicle);
        Assert.False(viewModel.Export.HasVehicle);
        Assert.True(viewModel.Form.IsClosed);
        Assert.Null(viewModel.Export.StatusMessage);
    }

    /// <summary>
    /// Prüft, dass ein fehlgeschlagenes Schließen die Seite nicht abstürzen lässt und beim nächsten Erscheinen erneut versucht wird.
    /// </summary>
    [Fact]
    public async Task Restored_VehicleGone_CloseFails_RetriesOnAppearing()
    {
        var vehicle = await CreateVehicleAsync();
        var viewModel = await OpenAsync(vehicle.Id);
        await Fixture.Vehicles.DeleteVehicleAsync(vehicle.Id, 0);
        Navigator.FailOnClose = true;

        Fixture.Restore.NotifyRestored();
        await viewModel.LastTask;
        Assert.Equal(1, Navigator.Closed);

        Navigator.FailOnClose = false;
        viewModel.OnAppearing();
        await viewModel.LastTask;

        Assert.Equal(2, Navigator.Closed);
        Assert.Null(viewModel.Vehicle);
    }

    /// <summary>
    /// Prüft, dass eine Seite ohne Fahrzeug bei der Benachrichtigung nichts lädt.
    /// </summary>
    [Fact]
    public void Restored_WithoutVehicle_DoesNothing()
    {
        var viewModel = new MAUI.ViewModels.TankbookEntriesViewModel(
            Fixture.Vehicles,
            Fixture.Service,
            Fixture.Photos,
            Fixture.CreateForm(Picker, Announcer, FormLogger),
            Fixture.CreateStatistics(Announcer, StatisticsLogger),
            Fixture.CreateExport(Sharer, Announcer, ExportLogger),
            Fixture.Restore,
            Navigator,
            Announcer,
            Logger);

        Fixture.Restore.NotifyRestored();

        Assert.False(viewModel.IsLoaded);
        Assert.Equal(0, Navigator.Closed);
    }

    /// <summary>
    /// Prüft, dass nur die Seiten des Fahrzeugs ausgewählt werden, das geschlossen werden soll, nicht die eines anderen Fahrzeugs.
    /// </summary>
    [Fact]
    public async Task SelectPages_OnlyPagesOfTheVehicle()
    {
        var vehicle = await CreateVehicleAsync();
        var viewModel = await OpenAsync(vehicle.Id);
        var pages = new[] { ("A", (object?)viewModel), ("B", null), ("C", "kein ViewModel") };

        var own = ShellTankbookNavigator.SelectPages(pages, page => page.Item2, vehicle.Id);
        var other = ShellTankbookNavigator.SelectPages(pages, page => page.Item2, vehicle.Id + 1);

        Assert.Equal(["A"], own.Select(page => page.Item1));
        Assert.Empty(other);
    }
}

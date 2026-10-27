using Tankradar.MAUI.Models.Tankbook;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass die geöffnete Seite <see cref="MAUI.ViewModels.TankbookEntriesViewModel"/> nach einer Wiederherstellung (H5) ohne erneutes Öffnen neu lädt
/// und die gemerkte Fahrzeugauswahl verwirft.
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
    /// Prüft, dass ohne Fahrzeug nach der Wiederherstellung „Exportieren“ ausgeblendet ist, „Vorgang erfassen“ kein Formular öffnet und der Leerzustand erscheint.
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
        Assert.True(viewModel.ShowNoVehiclesHint);
        Assert.Empty(viewModel.Entries);
    }

    /// <summary>
    /// Prüft, dass eine noch nie geladene Seite ohne Fahrzeuge bei der Benachrichtigung nur den Leerzustand zeigt und nichts merkt.
    /// </summary>
    [Fact]
    public async Task Restored_WithoutVehicle_ShowsNoVehiclesHint()
    {
        var viewModel = CreateViewModel();

        Fixture.Restore.NotifyRestored();
        await viewModel.LastTask;

        Assert.False(viewModel.HasVehicle);
        Assert.True(viewModel.ShowNoVehiclesHint);
        Assert.Null(SelectionStore.Get());
        Assert.Empty(Navigator.VehicleManagementOpened);
    }

    /// <summary>
    /// Prüft, dass die Wiederherstellung die gemerkte Auswahl verwirft und das erste Fahrzeug nach Name wählt und merkt.
    /// </summary>
    [Fact]
    public async Task Restored_ClearsStoredSelection_AndSelectsFirstByName()
    {
        var golf = await Fixture.CreateVehicleAsync("Golf");
        var polo = await Fixture.CreateVehicleAsync("Polo");
        var viewModel = await OpenAsync(polo.Id);
        Assert.Equal("Polo", viewModel.VehicleName);

        Fixture.Restore.NotifyRestored();
        await viewModel.LastTask;

        Assert.Equal("Golf", viewModel.VehicleName);
        Assert.Equal(golf.Id, viewModel.VehicleId);
        Assert.Equal(golf.Id, SelectionStore.Get());
    }
}

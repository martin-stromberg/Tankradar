using Tankradar.MAUI.Models.Tankbook;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Fahrzeugauswahl des Tabs „Tankbuch“ (<see cref="MAUI.ViewModels.TankbookEntriesViewModel"/>): gemerktes Fahrzeug, Rückfall, Leerzustand,
/// Wechsel über die Chips im Kopf, Ansage und Weg zur Fahrzeugverwaltung.
/// </summary>
public class TankbookEntriesViewModelTests_Selection : TankbookEntriesViewModelTestBase
{
    /// <summary>
    /// Prüft, dass der Tab das gemerkte Fahrzeug öffnet (nicht das erste nach Name) und es ansagt.
    /// </summary>
    [Fact]
    public async Task Load_ReusesStoredVehicle_AndAnnouncesIt()
    {
        await Fixture.CreateVehicleAsync("Golf");
        var polo = await Fixture.CreateVehicleAsync("Polo");
        SelectionStore.Set(polo.Id);
        var viewModel = CreateViewModel();

        viewModel.OnAppearing();
        await viewModel.LastTask;

        Assert.Equal("Polo", viewModel.VehicleName);
        Assert.Equal(polo.Id, viewModel.VehicleId);
        Assert.Equal(["Fahrzeug „Polo“"], Announcer.Announcements);
        Assert.Equal(["Golf", "Polo"], viewModel.VehicleOptions.Select(option => option.Label));
        Assert.Equal([false, true], viewModel.VehicleOptions.Select(option => option.IsSelected));
        Assert.Equal("Fahrzeug „Golf“ auswählen", viewModel.VehicleOptions[0].Hint);
    }

    /// <summary>
    /// Prüft, dass bei gelöschtem gemerktem Fahrzeug das erste nach Name gewählt und gemerkt wird, ohne Fehlermeldung.
    /// </summary>
    [Fact]
    public async Task Load_StoredVehicleDeleted_SelectsNextByNameAndStoresIt()
    {
        var audi = await Fixture.CreateVehicleAsync("Audi");
        var polo = await Fixture.CreateVehicleAsync("Polo");
        await Fixture.CreateVehicleAsync("Zoe", VehicleFuelKind.Electric);
        var viewModel = await OpenAsync(polo.Id);
        await Fixture.Vehicles.DeleteVehicleAsync(polo.Id, 0);

        viewModel.OnAppearing();
        await viewModel.LastTask;

        Assert.Equal("Audi", viewModel.VehicleName);
        Assert.Equal(audi.Id, SelectionStore.Get());
        Assert.Null(viewModel.StatusMessage);
        Assert.Equal(["Fahrzeug „Audi“"], Announcer.Announcements);
        Assert.Equal(["Audi", "Zoe"], viewModel.VehicleOptions.Select(option => option.Label));
    }

    /// <summary>
    /// Prüft den Leerzustand ohne Fahrzeug: Hinweis sichtbar, kein Fahrzeug, Formular, Auswertung und Export ausgeblendet bzw. gesperrt, keine Ansage.
    /// </summary>
    [Fact]
    public async Task Load_NoVehicles_ShowsNoVehiclesHintAndHidesSections()
    {
        var viewModel = CreateViewModel();

        viewModel.OnAppearing();
        await viewModel.LastTask;
        viewModel.AddEntryCommand.Execute(null);

        Assert.True(viewModel.ShowNoVehiclesHint);
        Assert.False(viewModel.HasVehicles);
        Assert.False(viewModel.HasVehicle);
        Assert.False(viewModel.ShowEmptyHint);
        Assert.True(viewModel.Form.IsClosed);
        Assert.False(viewModel.Statistics.HasKinds);
        Assert.False(viewModel.Export.HasVehicle);
        Assert.False(viewModel.ToggleVehiclePickerCommand.CanExecute(null));
        Assert.Null(SelectionStore.Get());
        Assert.Empty(Announcer.Announcements);
    }

    /// <summary>
    /// Prüft, dass „Fahrzeug hinzufügen“ des Leerzustands die Verwaltung mit dem Formular „Neues Fahrzeug“ öffnet.
    /// </summary>
    [Fact]
    public async Task AddVehicleCommand_CallsNavigatorWithStartCreate()
    {
        var viewModel = CreateViewModel();
        viewModel.OnAppearing();
        await viewModel.LastTask;

        await PressAsync(viewModel, viewModel.AddVehicleCommand);

        Assert.Equal([true], Navigator.VehicleManagementOpened);
    }

    /// <summary>
    /// Prüft, dass „Fahrzeuge verwalten“ die Verwaltung ohne Formular öffnet und die Auswahl im Kopf schließt.
    /// </summary>
    [Fact]
    public async Task ManageVehiclesCommand_CallsNavigator()
    {
        var vehicle = await CreateVehicleAsync();
        var viewModel = await OpenAsync(vehicle.Id);
        viewModel.ToggleVehiclePickerCommand.Execute(null);
        Assert.True(viewModel.IsVehiclePickerOpen);

        await PressAsync(viewModel, viewModel.ManageVehiclesCommand);

        Assert.Equal([false], Navigator.VehicleManagementOpened);
        Assert.False(viewModel.IsVehiclePickerOpen);
    }

    /// <summary>
    /// Prüft, dass der Wechsel über einen Chip die Kennung merkt, die Vorgänge des anderen Fahrzeugs lädt und das Fahrzeug ansagt.
    /// </summary>
    [Fact]
    public async Task SelectVehicle_StoresIdReloadsAndAnnounces()
    {
        var golf = await Fixture.CreateVehicleAsync("Golf");
        var polo = await Fixture.CreateVehicleAsync("Polo");
        await Fixture.CreateEntryAsync(polo.Id, TankbookEntryFixture.Input(Fixture.Now.AddDays(-1), 10_500, 40m));
        var viewModel = await OpenAsync(golf.Id);
        Assert.Empty(viewModel.Entries);

        viewModel.ToggleVehiclePickerCommand.Execute(null);
        await PressAsync(viewModel, viewModel.VehicleOptions.Single(option => option.Label == "Polo").SelectCommand);

        Assert.Equal("Polo", viewModel.VehicleName);
        Assert.Equal(polo.Id, SelectionStore.Get());
        Assert.Single(viewModel.Entries);
        Assert.True(viewModel.Statistics.HasKinds);
        Assert.Contains("Polo", viewModel.Export.StartHint, StringComparison.Ordinal);
        Assert.Equal(["Fahrzeug „Polo“"], Announcer.Announcements);
        Assert.Equal([false, true], viewModel.VehicleOptions.Select(option => option.IsSelected));
    }

    /// <summary>
    /// Prüft, dass sich ein offenes Vorgangsformular und die Chips der Auswahl beim Wechsel schließen.
    /// </summary>
    [Fact]
    public async Task SelectVehicle_ClosesOpenFormAndPicker()
    {
        var golf = await Fixture.CreateVehicleAsync("Golf");
        var polo = await Fixture.CreateVehicleAsync("Polo");
        var viewModel = await OpenAsync(golf.Id);
        viewModel.AddEntryCommand.Execute(null);
        viewModel.ToggleVehiclePickerCommand.Execute(null);
        Assert.True(viewModel.Form.IsOpen);
        Assert.True(viewModel.IsVehiclePickerOpen);

        await viewModel.SelectVehicleAsync(polo.Id);

        Assert.True(viewModel.Form.IsClosed);
        Assert.False(viewModel.IsVehiclePickerOpen);
        Assert.Equal("Polo", viewModel.VehicleName);
    }

    /// <summary>
    /// Prüft, dass die Liste nach dem Wechsel nur die Vorgänge des gewählten Fahrzeugs enthält.
    /// </summary>
    [Fact]
    public async Task SelectVehicle_ShowsOnlyEntriesOfSelectedVehicle()
    {
        var golf = await Fixture.CreateVehicleAsync("Golf");
        var polo = await Fixture.CreateVehicleAsync("Polo");
        await Fixture.CreateEntryAsync(golf.Id, TankbookEntryFixture.Input(Fixture.Now.AddDays(-2), 10_500, 40m, stationName: "Golf Tankstelle"));
        await Fixture.CreateEntryAsync(polo.Id, TankbookEntryFixture.Input(Fixture.Now.AddDays(-1), 10_400, 30m, stationName: "Polo Tankstelle"));
        var viewModel = await OpenAsync(golf.Id);
        Assert.Equal(["Golf Tankstelle"], viewModel.Entries.Select(item => item.StationName));

        await viewModel.SelectVehicleAsync(polo.Id);

        Assert.Equal(["Polo Tankstelle"], viewModel.Entries.Select(item => item.StationName));
    }

    /// <summary>
    /// Prüft, dass der Wechsel zu einer Kennung, die nicht in der Auswahl steht (z. B. ein gelöschtes Fahrzeug), die Fahrzeuge neu lädt, das erste nach Name
    /// wählt, es merkt und ansagt.
    /// </summary>
    [Fact]
    public async Task SelectVehicle_UnknownId_ReloadsAndSelectsFirstByName()
    {
        var audi = await Fixture.CreateVehicleAsync("Audi");
        var polo = await Fixture.CreateVehicleAsync("Polo");
        var zoe = await Fixture.CreateVehicleAsync("Zoe", VehicleFuelKind.Electric);
        await Fixture.Vehicles.DeleteVehicleAsync(polo.Id, 0);
        var viewModel = await OpenAsync(zoe.Id);
        Assert.DoesNotContain(viewModel.VehicleOptions, option => option.Vehicle.Id == polo.Id);

        await viewModel.SelectVehicleAsync(polo.Id);

        Assert.Equal("Audi", viewModel.VehicleName);
        Assert.Equal(audi.Id, SelectionStore.Get());
        Assert.Equal(["Audi", "Zoe"], viewModel.VehicleOptions.Select(option => option.Label));
        Assert.Equal([true, false], viewModel.VehicleOptions.Select(option => option.IsSelected));
        Assert.Equal(["Fahrzeug „Audi“"], Announcer.Announcements);
        Assert.Null(viewModel.StatusMessage);
    }

    /// <summary>
    /// Prüft, dass ein Fehler beim Öffnen der Fahrzeugverwaltung protokolliert wird, ohne abzustürzen, und die Seite benutzbar bleibt.
    /// </summary>
    [Fact]
    public async Task ManageVehiclesCommand_NavigatorFails_LogsWarningWithoutCrash()
    {
        var vehicle = await CreateVehicleAsync();
        var viewModel = await OpenAsync(vehicle.Id);
        Navigator.FailOnOpen = true;

        await PressAsync(viewModel, viewModel.ManageVehiclesCommand);

        Assert.Equal([false], Navigator.VehicleManagementOpened);
        Assert.Contains("Die Fahrzeugverwaltung konnte nicht geöffnet werden (InvalidOperationException).", Logger.Messages);
        Assert.True(viewModel.ManageVehiclesCommand.CanExecute(null));
        Assert.True(viewModel.HasVehicle);
        Assert.Null(viewModel.StatusMessage);
    }
}

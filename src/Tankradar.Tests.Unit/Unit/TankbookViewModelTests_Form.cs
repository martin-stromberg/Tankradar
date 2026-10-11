using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Formular des <see cref="TankbookViewModel"/>: Anlegen, Bearbeiten, Kraftstoffart mit Einheit, feldbezogene Fehler, Abbrechen und Speicherfehler.
/// </summary>
public class TankbookViewModelTests_Form : BaseTest
{
    private readonly VehicleFixture _fixture = new();
    private readonly RecordingLogger<TankbookViewModel> _logger = new();

    private async Task<TankbookViewModel> OpenAsync()
    {
        var viewModel = new TankbookViewModel(_fixture.Service, _logger);
        viewModel.OnAppearing();
        await viewModel.LastTask;
        return viewModel;
    }

    private static async Task SaveAsync(TankbookViewModel viewModel)
    {
        viewModel.SaveCommand.Execute(null);
        await viewModel.LastTask;
    }

    private static ChoiceOptionViewModel<VehicleFuelKind> Chip(TankbookViewModel viewModel, VehicleFuelKind fuelKind)
    {
        return viewModel.FuelKindOptions.Single(option => option.Value == fuelKind);
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
    /// Prüft, dass „Fahrzeug hinzufügen“ das leere Formular mit vorgewähltem Benzin öffnet.
    /// </summary>
    [Fact]
    public async Task Add_OpensFormWithPetrolPreselected()
    {
        var viewModel = await OpenAsync();
        Assert.True(viewModel.IsFormClosed);

        viewModel.AddVehicleCommand.Execute(null);

        Assert.True(viewModel.IsFormOpen);
        Assert.False(viewModel.IsFormClosed);
        Assert.Equal("Neues Fahrzeug", viewModel.FormTitle);
        Assert.Null(viewModel.EditingVehicleId);
        Assert.Equal(string.Empty, viewModel.FormName);
        Assert.Equal(string.Empty, viewModel.FormOdometer);
        Assert.Equal(string.Empty, viewModel.FormConsumption);
        Assert.Equal(VehicleFuelKind.Petrol, viewModel.SelectedFuelKind);
        Assert.Equal(["Benzin", "Diesel", "Elektro", "Hybrid"], viewModel.FuelKindOptions.Select(option => option.Label));
        Assert.Equal([true, false, false, false], viewModel.FuelKindOptions.Select(option => option.IsSelected));
        Assert.Equal("L/100 km", viewModel.ConsumptionUnit);
        Assert.Equal("z. B. 5,4", viewModel.ConsumptionPlaceholder);
    }

    /// <summary>
    /// Prüft, dass die Wahl von Elektro die Einheit auf kWh/100 km umstellt und Hybrid wieder auf L/100 km.
    /// </summary>
    [Fact]
    public async Task SelectElectric_SwitchesConsumptionUnit()
    {
        var viewModel = await OpenAsync();
        viewModel.AddVehicleCommand.Execute(null);

        Chip(viewModel, VehicleFuelKind.Electric).SelectCommand.Execute(null);
        Assert.Equal(VehicleFuelKind.Electric, viewModel.SelectedFuelKind);
        Assert.Equal("kWh/100 km", viewModel.ConsumptionUnit);
        Assert.Equal("Startverbrauch, optional, in kWh/100 km", viewModel.ConsumptionDescription);
        Assert.False(Chip(viewModel, VehicleFuelKind.Petrol).IsSelected);

        Chip(viewModel, VehicleFuelKind.Hybrid).SelectCommand.Execute(null);
        Assert.Equal("L/100 km", viewModel.ConsumptionUnit);
        Assert.Equal([false, false, false, true], viewModel.FuelKindOptions.Select(option => option.IsSelected));
    }

    /// <summary>
    /// Prüft, dass ungültige Felder feldbezogen gemeldet und nicht gespeichert werden.
    /// </summary>
    [Fact]
    public async Task Save_InvalidFields_ShowsFieldErrorsAndSavesNothing()
    {
        var viewModel = await OpenAsync();
        viewModel.AddVehicleCommand.Execute(null);
        viewModel.FormName = "  ";
        viewModel.FormOdometer = "-5";
        viewModel.FormConsumption = "abc";

        await SaveAsync(viewModel);

        Assert.Equal("Bitte gib einen Namen ein.", viewModel.NameError);
        Assert.Equal("Der Kilometerstand darf nicht negativ sein.", viewModel.OdometerError);
        Assert.Equal("Bitte gib den Startverbrauch als Zahl ein, z. B. 5,4.", viewModel.ConsumptionError);
        Assert.True(viewModel.HasNameError);
        Assert.True(viewModel.HasOdometerError);
        Assert.True(viewModel.HasConsumptionError);
        Assert.True(viewModel.IsFormOpen);
        Assert.Empty(await _fixture.Service.GetVehiclesAsync());
    }

    /// <summary>
    /// Prüft das Anlegen: Das Formular schließt, die Liste wird neu geladen.
    /// </summary>
    [Fact]
    public async Task Save_Valid_CreatesVehicleClosesFormAndReloads()
    {
        var viewModel = await OpenAsync();
        viewModel.AddVehicleCommand.Execute(null);
        viewModel.FormName = " Golf ";
        Chip(viewModel, VehicleFuelKind.Diesel).SelectCommand.Execute(null);
        viewModel.FormOdometer = "84.350";
        viewModel.FormConsumption = "5,4";

        await SaveAsync(viewModel);

        Assert.False(viewModel.IsFormOpen);
        Assert.False(viewModel.HasNameError);
        var item = Assert.Single(viewModel.Vehicles);
        Assert.Equal("Golf", item.Name);
        Assert.Equal("Diesel • Start 84.350 km • Startverbrauch 5,4 L/100 km", item.Details);
        Assert.False(viewModel.ShowEmptyHint);
    }

    /// <summary>
    /// Prüft, dass ein schon vergebener Name als Feldfehler am Namen erscheint.
    /// </summary>
    [Fact]
    public async Task Save_DuplicateName_ShowsNameError()
    {
        await _fixture.Service.CreateVehicleAsync(new VehicleInput("Golf", VehicleFuelKind.Diesel, 0, null));
        var viewModel = await OpenAsync();
        viewModel.AddVehicleCommand.Execute(null);
        viewModel.FormName = "golf";
        viewModel.FormOdometer = "10";

        await SaveAsync(viewModel);

        Assert.Equal("Ein Fahrzeug mit diesem Namen gibt es bereits.", viewModel.NameError);
        Assert.True(viewModel.IsFormOpen);
        Assert.Single(await _fixture.Service.GetVehiclesAsync());
    }

    /// <summary>
    /// Prüft, dass „Bearbeiten“ das Formular mit den gespeicherten Werten vorbelegt und Änderungen speichert.
    /// </summary>
    [Fact]
    public async Task Edit_PrefillsFormAndSavesChanges()
    {
        await _fixture.Service.CreateVehicleAsync(new VehicleInput("Golf", VehicleFuelKind.Diesel, 84350, 5.4m));
        var viewModel = await OpenAsync();
        var item = viewModel.Vehicles[0];

        item.EditCommand.Execute(null);

        Assert.True(viewModel.IsFormOpen);
        Assert.Equal("Fahrzeug bearbeiten", viewModel.FormTitle);
        Assert.Equal(item.Id, viewModel.EditingVehicleId);
        Assert.Equal("Golf", viewModel.FormName);
        Assert.Equal("84350", viewModel.FormOdometer);
        Assert.Equal("5,4", viewModel.FormConsumption);
        Assert.True(Chip(viewModel, VehicleFuelKind.Diesel).IsSelected);
        Assert.False(Chip(viewModel, VehicleFuelKind.Petrol).IsSelected);

        viewModel.FormName = "Golf Variant";
        Chip(viewModel, VehicleFuelKind.Electric).SelectCommand.Execute(null);
        viewModel.FormConsumption = "16,5";
        await SaveAsync(viewModel);

        Assert.False(viewModel.IsFormOpen);
        Assert.Equal("Golf Variant", viewModel.Vehicles[0].Name);
        Assert.Equal("Elektro • Start 84.350 km • Startverbrauch 16,5 kWh/100 km", viewModel.Vehicles[0].Details);
    }

    /// <summary>
    /// Prüft, dass „Abbrechen“ das Formular ohne Speichern schließt und die Fehler leert.
    /// </summary>
    [Fact]
    public async Task Cancel_ClosesFormWithoutSaving()
    {
        var viewModel = await OpenAsync();
        viewModel.AddVehicleCommand.Execute(null);
        await SaveAsync(viewModel);
        Assert.True(viewModel.HasNameError);
        viewModel.FormName = "Golf";
        viewModel.FormOdometer = "1";

        viewModel.CancelCommand.Execute(null);

        Assert.False(viewModel.IsFormOpen);
        Assert.False(viewModel.HasNameError);
        Assert.False(viewModel.HasOdometerError);
        Assert.Empty(await _fixture.Service.GetVehiclesAsync());
    }

    /// <summary>
    /// Prüft, dass ein Speicherfehler gemeldet wird und das Formular offen bleibt.
    /// </summary>
    [Fact]
    public async Task Save_StorageFailure_ShowsMessage()
    {
        var viewModel = await OpenAsync();
        viewModel.AddVehicleCommand.Execute(null);
        viewModel.FormName = "Geheimauto";
        viewModel.FormOdometer = "1";
        _fixture.Factory.FailingCreations = 1;

        await SaveAsync(viewModel);

        Assert.Equal("Die Änderung konnte nicht gespeichert werden.", viewModel.StatusMessage);
        Assert.True(viewModel.IsFormOpen);
        Assert.False(viewModel.IsBusy);
        Assert.DoesNotContain("Geheimauto", _logger.AllText, StringComparison.Ordinal);
    }
}

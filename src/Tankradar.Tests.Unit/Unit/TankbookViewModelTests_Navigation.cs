using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Backup;
using Tankradar.MAUI.Services.Navigation;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Einstieg in die Fahrzeugverwaltung (<see cref="TankbookViewModel"/>) aus dem Tab „Tankbuch“ über die Shell-Navigation.
/// </summary>
public class TankbookViewModelTests_Navigation : BaseTest
{
    private readonly VehicleFixture _fixture = new();

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
    /// Prüft, dass die Verwaltung mit <c>startCreate</c> nach dem Laden das Formular „Neues Fahrzeug“ öffnet.
    /// </summary>
    [Fact]
    public async Task ApplyQueryAttributes_StartCreate_OpensCreateForm()
    {
        await _fixture.Service.CreateVehicleAsync(new VehicleInput("Polo", VehicleFuelKind.Petrol, 2000, null));
        var viewModel = CreateViewModel();

        viewModel.ApplyQueryAttributes(new Dictionary<string, object> { [ShellTankbookNavigator.StartCreateParameter] = true });
        viewModel.OnAppearing();
        await viewModel.LastTask;

        Assert.True(viewModel.IsFormOpen);
        Assert.Equal("Neues Fahrzeug", viewModel.FormTitle);
        Assert.Null(viewModel.EditingVehicleId);
    }

    /// <summary>
    /// Prüft, dass die Verwaltung ohne <c>startCreate</c> nur die Liste zeigt und das Formular geschlossen bleibt.
    /// </summary>
    [Fact]
    public async Task ApplyQueryAttributes_WithoutStartCreate_KeepsFormClosed()
    {
        var viewModel = CreateViewModel();

        viewModel.ApplyQueryAttributes(new Dictionary<string, object> { [ShellTankbookNavigator.StartCreateParameter] = false });
        viewModel.OnAppearing();
        await viewModel.LastTask;

        Assert.False(viewModel.IsFormOpen);
        Assert.True(viewModel.IsLoaded);
    }

    /// <summary>
    /// Prüft, dass <c>startCreate</c> nur einmal wirkt: Nach dem Abbrechen öffnet das nächste Erscheinen ohne den Parameter das Formular nicht erneut.
    /// </summary>
    [Fact]
    public async Task ApplyQueryAttributes_StartCreate_AppliesOnlyOnce()
    {
        var viewModel = CreateViewModel();
        viewModel.ApplyQueryAttributes(new Dictionary<string, object> { [ShellTankbookNavigator.StartCreateParameter] = true });
        viewModel.OnAppearing();
        await viewModel.LastTask;
        viewModel.CancelCommand.Execute(null);

        viewModel.OnAppearing();
        await viewModel.LastTask;

        Assert.False(viewModel.IsFormOpen);
        Assert.True(viewModel.IsLoaded);
    }

    private TankbookViewModel CreateViewModel()
    {
        return new TankbookViewModel(_fixture.Service, new RecordingLogger<TankbookViewModel>(), new FakeScreenReaderAnnouncer(), new RestoreNotifier());
    }
}

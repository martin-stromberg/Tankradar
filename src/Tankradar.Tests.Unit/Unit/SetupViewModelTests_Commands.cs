using Tankradar.MAUI.Models;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass die Befehle der Einrichtung an <c>SaveAsync</c> und <c>SkipAsync</c> delegieren.
/// </summary>
public class SetupViewModelTests_Commands : BaseTest
{
    private readonly FakeSettingsService _service = new();

    /// <summary>
    /// Prüft, dass <c>SaveCommand</c> die aktuelle Auswahl speichert und <c>Completed</c> auslöst.
    /// </summary>
    [Fact]
    public void SetupViewModel_SaveCommand_SavesSelection()
    {
        var viewModel = SetupViewModelFactory.CreateSetupViewModel(_service);
        var completed = 0;
        viewModel.Completed += (_, _) => completed++;
        viewModel.FuelTypes.Single(item => item.FuelType == FuelType.Diesel).IsSelected = false;

        viewModel.SaveCommand.Execute(null);

        Assert.Equal(
            [new FuelTypeSelection(FuelType.SuperE5, true), new FuelTypeSelection(FuelType.SuperE10, true), new FuelTypeSelection(FuelType.Diesel, false)],
            Assert.Single(_service.Saved).FuelTypes);
        Assert.Equal(1, completed);
    }

    /// <summary>
    /// Prüft, dass <c>SkipCommand</c> unabhängig von der Auswahl die Standardeinstellungen speichert und <c>Completed</c> auslöst.
    /// </summary>
    [Fact]
    public void SetupViewModel_SkipCommand_SavesDefaults()
    {
        var viewModel = SetupViewModelFactory.CreateSetupViewModel(_service);
        var completed = 0;
        viewModel.Completed += (_, _) => completed++;
        viewModel.FuelTypes.Single(item => item.FuelType == FuelType.Diesel).IsSelected = false;

        viewModel.SkipCommand.Execute(null);

        Assert.Equal(AppSettings.CreateDefault().FuelTypes, Assert.Single(_service.Saved).FuelTypes);
        Assert.Equal(1, completed);
    }
}

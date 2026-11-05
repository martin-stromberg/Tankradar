using Tankradar.MAUI.Models;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Vorbelegung und das Umschalten der Spritsorten im <see cref="MAUI.ViewModels.SetupViewModel"/>.
/// </summary>
public class SetupViewModelTests_Selection : BaseTest
{
    private readonly FakeSettingsService _service = new();

    /// <summary>
    /// Prüft, dass alle Spritsorten in der Standardreihenfolge E5, E10, Diesel ausgewählt sind.
    /// </summary>
    [Fact]
    public void SetupViewModel_DefaultsAllFuelTypesSelected()
    {
        var viewModel = SetupViewModelFactory.CreateSetupViewModel(_service);

        Assert.Equal([FuelType.SuperE5, FuelType.SuperE10, FuelType.Diesel], viewModel.FuelTypes.Select(item => item.FuelType));
        Assert.All(viewModel.FuelTypes, item => Assert.True(item.IsSelected));
        Assert.False(viewModel.HasStatusMessage);
        Assert.Empty(_service.Saved);
    }

    /// <summary>
    /// Prüft, dass das Umschalten einer Sorte nur deren Auswahl ändert und nichts sofort speichert.
    /// </summary>
    [Fact]
    public void SetupViewModel_ToggleFuelType_UpdatesSelection()
    {
        var viewModel = SetupViewModelFactory.CreateSetupViewModel(_service);

        viewModel.FuelTypes.Single(item => item.FuelType == FuelType.SuperE10).IsSelected = false;

        Assert.False(viewModel.FuelTypes.Single(item => item.FuelType == FuelType.SuperE10).IsSelected);
        Assert.True(viewModel.FuelTypes.Single(item => item.FuelType == FuelType.SuperE5).IsSelected);
        Assert.True(viewModel.FuelTypes.Single(item => item.FuelType == FuelType.Diesel).IsSelected);
        Assert.Equal(0, _service.SaveCount);
    }
}

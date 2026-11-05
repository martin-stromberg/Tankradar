using Tankradar.MAUI.Models;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Überspringen der Einrichtung.
/// </summary>
public class SetupViewModelTests_Skip : BaseTest
{
    private readonly FakeSettingsService _service = new();

    /// <summary>
    /// Prüft, dass Überspringen unabhängig von der aktuellen Auswahl die Standardeinstellungen speichert und <c>Completed</c> auslöst.
    /// </summary>
    [Fact]
    public async Task SetupViewModel_Skip_SavesDefaults()
    {
        var viewModel = SetupViewModelFactory.CreateSetupViewModel(_service);
        var completed = 0;
        viewModel.Completed += (_, _) => completed++;
        viewModel.FuelTypes.Single(item => item.FuelType == FuelType.Diesel).IsSelected = false;

        await viewModel.SkipAsync();

        var defaults = AppSettings.CreateDefault();
        var saved = Assert.Single(_service.Saved);
        Assert.Equal(defaults.FuelTypes, saved.FuelTypes);
        Assert.Equal(defaults.GpsUsage, saved.GpsUsage);
        Assert.Equal(defaults.ResultView, saved.ResultView);
        Assert.Equal(defaults.ResultSortOrder, saved.ResultSortOrder);
        Assert.Equal(defaults.ShowNearby, saved.ShowNearby);
        Assert.Equal(1, completed);
    }
}

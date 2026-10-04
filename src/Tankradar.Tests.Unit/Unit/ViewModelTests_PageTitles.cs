using Tankradar.MAUI.ViewModels;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass jedes seitenspezifische ViewModel den erwarteten Seitentitel setzt und <see cref="BaseViewModel.OnAppearing"/> ohne Seiteneffekte aufrufbar ist.
/// </summary>
public class ViewModelTests_PageTitles : BaseTest
{
    /// <summary>
    /// Prüft die Seitentitel aller ViewModels der Hauptnavigation.
    /// </summary>
    /// <param name="viewModelType">Der Typ des zu prüfenden ViewModels.</param>
    /// <param name="expectedTitle">Der erwartete Seitentitel.</param>
    [Theory]
    [InlineData(typeof(FavoritesViewModel), "Favoriten")]
    [InlineData(typeof(MapViewModel), "Karte")]
    [InlineData(typeof(TankbookViewModel), "Tankbuch")]
    [InlineData(typeof(SettingsViewModel), "Optionen")]
    public void Constructor_SetsExpectedTitle(Type viewModelType, string expectedTitle)
    {
        var viewModel = (BaseViewModel)Activator.CreateInstance(viewModelType)!;

        Assert.Equal(expectedTitle, viewModel.Title);
    }

    /// <summary>
    /// Prüft, dass <see cref="BaseViewModel.OnAppearing"/> den Zustand des ViewModels nicht verändert.
    /// </summary>
    [Fact]
    public void OnAppearing_DoesNotChangeState()
    {
        var viewModel = new MapViewModel();
        var raisedCount = 0;
        viewModel.PropertyChanged += (_, _) => raisedCount++;

        viewModel.OnAppearing();

        Assert.Equal(0, raisedCount);
        Assert.Equal("Karte", viewModel.Title);
        Assert.False(viewModel.IsBusy);
    }
}

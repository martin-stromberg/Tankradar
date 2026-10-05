using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;

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
    [InlineData(typeof(TankbookViewModel), "Tankbuch")]
    public void Constructor_SetsExpectedTitle(Type viewModelType, string expectedTitle)
    {
        var viewModel = (BaseViewModel)Activator.CreateInstance(viewModelType)!;

        Assert.Equal(expectedTitle, viewModel.Title);
    }

    /// <summary>
    /// Prüft den Seitentitel des <see cref="MapViewModel"/>, das Dienste für Einstellungen, Standort, Preise und Verbindung benötigt.
    /// </summary>
    [Fact]
    public void MapViewModel_Constructor_SetsTitle()
    {
        var viewModel = new MapViewModel(
            new FakeSettingsService(),
            new FakeLocationService(),
            new FakeGeocodingService(),
            new FakeFuelPriceService(),
            new FakeConnectionMonitor(),
            TimeProvider.System,
            NullLogger<MapViewModel>.Instance);

        Assert.Equal("Karte", viewModel.Title);
    }

    /// <summary>
    /// Prüft den Seitentitel des <see cref="SettingsViewModel"/>, das einen Einstellungsdienst benötigt.
    /// </summary>
    [Fact]
    public void SettingsViewModel_Constructor_SetsTitle()
    {
        var viewModel = new SettingsViewModel(new FakeSettingsService(), NullLogger<SettingsViewModel>.Instance);

        Assert.Equal("Optionen", viewModel.Title);
    }

    /// <summary>
    /// Prüft, dass <see cref="BaseViewModel.OnAppearing"/> den Zustand des ViewModels nicht verändert.
    /// </summary>
    [Fact]
    public void OnAppearing_DoesNotChangeState()
    {
        var viewModel = new FavoritesViewModel();
        var raisedCount = 0;
        viewModel.PropertyChanged += (_, _) => raisedCount++;

        viewModel.OnAppearing();

        Assert.Equal(0, raisedCount);
        Assert.Equal("Favoriten", viewModel.Title);
        Assert.False(viewModel.IsBusy);
    }
}

using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.Services.Backup;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass jedes seitenspezifische ViewModel den erwarteten Seitentitel setzt und <see cref="BaseViewModel.OnAppearing"/> ohne Seiteneffekte aufrufbar ist.
/// </summary>
public class ViewModelTests_PageTitles : BaseTest
{
    /// <summary>
    /// Prüft den Seitentitel des <see cref="TankbookViewModel"/>, das den Fahrzeugdienst benötigt.
    /// </summary>
    [Fact]
    public void TankbookViewModel_Constructor_SetsTitle()
    {
        using var vehicles = new VehicleFixture();

        var viewModel = new TankbookViewModel(vehicles.Service, new FakeTankbookNavigator(), NullLogger<TankbookViewModel>.Instance, new FakeScreenReaderAnnouncer(), new RestoreNotifier());

        Assert.Equal("Tankbuch", viewModel.Title);
    }

    /// <summary>
    /// Prüft den Seitentitel des <see cref="TankbookEntriesViewModel"/> (Vorgänge eines Fahrzeugs).
    /// </summary>
    [Fact]
    public void TankbookEntriesViewModel_Constructor_SetsTitle()
    {
        using var fixture = new TankbookEntryFixture();
        var form = fixture.CreateForm(new FakeReceiptPhotoPicker(), new FakeScreenReaderAnnouncer(), new Tankradar.TestSupport.RecordingLogger<TankbookEntryFormViewModel>());

        var viewModel = new TankbookEntriesViewModel(
            fixture.Vehicles,
            fixture.Service,
            fixture.Photos,
            form,
            fixture.CreateStatistics(new FakeScreenReaderAnnouncer(), new Tankradar.TestSupport.RecordingLogger<TankbookStatisticsViewModel>()),
            fixture.CreateExport(new FakeBackupSharer(), new FakeScreenReaderAnnouncer(), new Tankradar.TestSupport.RecordingLogger<TankbookExportViewModel>()),
            fixture.Restore,
            new FakeTankbookNavigator(),
            new FakeScreenReaderAnnouncer(),
            NullLogger<TankbookEntriesViewModel>.Instance);

        Assert.Equal("Tank- und Ladevorgänge", viewModel.Title);
        viewModel.OnAppearing();
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
            new FakeStationNavigator(),
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
    /// Prüft die Seitentitel der Favoriten-ViewModels, die den Favoritendienst benötigen.
    /// </summary>
    [Fact]
    public void FavoritesViewModels_Constructor_SetTitles()
    {
        using var favorites = new FavoritesFixture();
        var navigator = new FakeFavoriteGroupNavigator();

        Assert.Equal("Favoriten", HomeViewModelFactory.Create(favorites.Service).Title);
        Assert.Equal("Favoritengruppe", new FavoriteGroupViewModel(favorites.Service, navigator, new FakeStationNavigator(), NullLogger<FavoriteGroupViewModel>.Instance).Title);
    }
}

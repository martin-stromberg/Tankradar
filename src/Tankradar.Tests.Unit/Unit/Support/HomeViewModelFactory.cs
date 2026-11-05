using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.Services.Favorites;
using Tankradar.MAUI.ViewModels;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Erzeugt das ViewModel der Startseite mit Standard-Doubles für alle Abhängigkeiten außer dem Favoritendienst.
/// </summary>
public static class HomeViewModelFactory
{
    /// <summary>
    /// Erstellt das ViewModel der Startseite.
    /// </summary>
    /// <param name="service">Der Favoritendienst.</param>
    /// <returns>Das ViewModel.</returns>
    public static FavoritesViewModel Create(IFavoritesService service)
    {
        var prices = new FakeFuelPriceService();
        var stationNavigator = new FakeStationNavigator();
        var nearby = new NearbySectionViewModel(prices, stationNavigator, TimeProvider.System, NullLogger<NearbySectionViewModel>.Instance);
        return new FavoritesViewModel(
            service,
            new FakeFavoriteGroupNavigator(),
            new FakeSettingsService(),
            prices,
            new FakeLocationService(),
            new FakeConnectionMonitor(),
            stationNavigator,
            TimeProvider.System,
            nearby,
            NullLogger<FavoritesViewModel>.Instance);
    }
}

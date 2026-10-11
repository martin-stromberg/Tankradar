using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Gemeinsamer Aufbau der Tests der Startseite: echter Favoritendienst auf In-Memory-SQLite und ersetzte Preis-, Standort-, Einstellungs-, Verbindungs- und Navigationsdienste.
/// </summary>
public abstract class FavoritesViewModelTestBase : BaseTest
{
    /// <summary>
    /// Der Favoritendienst samt Teststationen.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FavoritesFixture Fixture { get; } = new();

    /// <summary>
    /// Die Navigation zur Gruppenansicht.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeFavoriteGroupNavigator GroupNavigator { get; } = new();

    /// <summary>
    /// Die Navigation zur Detailansicht.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeStationNavigator StationNavigator { get; } = new();

    /// <summary>
    /// Der Einstellungsdienst.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeSettingsService Settings { get; } = new();

    /// <summary>
    /// Der Preisdienst.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeFuelPriceService Prices { get; } = new();

    /// <summary>
    /// Der Standortdienst.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeLocationService Location { get; } = new();

    /// <summary>
    /// Die steuerbare Verbindung.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeConnectionMonitor Connection { get; } = new();

    /// <summary>
    /// Die Testuhr.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected ManualTimeProvider Clock { get; } = new();

    /// <summary>
    /// Der Logger der Startseite.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected RecordingLogger<FavoritesViewModel> Logger { get; } = new();

    /// <summary>
    /// Der Abrufzeitpunkt, der den Preisen der Teststationen mitgegeben wird: fünf Minuten vor „jetzt“.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected DateTime FreshTime => Clock.UtcNow.AddMinutes(-5);

    /// <summary>
    /// Erstellt das ViewModel der Startseite.
    /// </summary>
    /// <returns>Das ViewModel.</returns>
    protected FavoritesViewModel CreateViewModel()
    {
        var nearby = new NearbySectionViewModel(Prices, StationNavigator, Clock, NullLogger<NearbySectionViewModel>.Instance);
        return new FavoritesViewModel(Fixture.Service, GroupNavigator, Settings, Prices, Location, Connection, StationNavigator, Clock, nearby, Logger);
    }

    /// <summary>
    /// Legt eine Gruppe mit den angegebenen Teststationen an und gibt deren Kennung zurück.
    /// </summary>
    /// <param name="name">Der Name der Gruppe.</param>
    /// <param name="stationIds">Die Kennungen der Tankstellen.</param>
    /// <returns>Die Kennung der Gruppe.</returns>
    protected async Task<long> CreateGroupAsync(string name, params string[] stationIds)
    {
        var group = (await Fixture.Service.CreateGroupAsync(name, null)).Group!.Id;
        foreach (var id in stationIds)
        {
            await Fixture.Service.AddStationAsync(group, id);
        }

        return group;
    }

    /// <summary>
    /// Erzeugt zu den Teststationen des Favoritendienstes die Tankstellen mit Preisen für die Fakes des Preisdienstes (lokal bekannt und als Ergebnis eines Abrufs).
    /// </summary>
    /// <param name="retrievedUtc">Der Abrufzeitpunkt der Preise.</param>
    /// <returns>Die Tankstellen Alpha, Beta und Citra mit Position nahe der Testposition.</returns>
    protected IReadOnlyList<StationInfo> SeedPrices(DateTime retrievedUtc)
    {
        StationInfo Make(string id, string name, double lat, decimal e5, decimal diesel)
        {
            return new StationInfo
            {
                Id = id,
                Name = name,
                Latitude = lat,
                Longitude = StationFactory.CenterLongitude,
                Prices = [new FuelPrice(FuelType.SuperE5, e5, retrievedUtc), new FuelPrice(FuelType.Diesel, diesel, retrievedUtc)],
            };
        }

        var stations = new List<StationInfo>
        {
            Make(FavoritesFixture.StationA, "Alpha Tankstelle", StationFactory.CenterLatitude + 0.01, 1.859m, 1.699m),
            Make(FavoritesFixture.StationB, "Beta Tankstelle", StationFactory.CenterLatitude + 0.02, 1.879m, 1.719m),
            Make(FavoritesFixture.StationC, "Citra Tankstelle", StationFactory.CenterLatitude + 0.03, 1.899m, 1.739m),
        };
        Prices.KnownStations.AddRange(stations);
        Prices.RefreshResult = new StationRefreshResult(stations, PriceDataSource.Live, PriceFailure.None);
        return stations;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Fixture.Dispose();
        }

        base.Dispose(disposing);
    }
}

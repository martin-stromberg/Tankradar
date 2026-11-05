using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Map;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Gemeinsamer Aufbau der Tests des <see cref="MapViewModel"/>: Fakes für Einstellungen, Standort, Preisdienst, Verbindung und Uhr.
/// </summary>
public abstract class MapViewModelTestBase : BaseTest
{
    /// <summary>
    /// Erstellt den Aufbau mit einem ViewModel, das auf die Fakes zugreift.
    /// </summary>
    protected MapViewModelTestBase()
    {
        ViewModel = new MapViewModel(Settings, Location, Geocoding, Prices, RouteSearch, RouteAccess, Connection, Clock, Navigator, Logger);
    }

    /// <summary>
    /// Die Einstellungen.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeSettingsService Settings { get; } = new();

    /// <summary>
    /// Der Standortdienst.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeLocationService Location { get; } = new();

    /// <summary>
    /// Der Geokodierungsdienst.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeGeocodingService Geocoding { get; } = new();

    /// <summary>
    /// Der Preisdienst.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeFuelPriceService Prices { get; } = new();

    /// <summary>
    /// Der Routensuchdienst.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeRouteSearchService RouteSearch { get; } = new();

    /// <summary>
    /// Der Zugang zur Routensuche (standardmäßig mit Schlüssel und eingeschaltet).
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeRouteSearchAccess RouteAccess { get; } = new();

    /// <summary>
    /// Die Verbindungserkennung.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeConnectionMonitor Connection { get; } = new();

    /// <summary>
    /// Die Uhr.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected ManualTimeProvider Clock { get; } = new();

    /// <summary>
    /// Die Navigation zur Detailansicht.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeStationNavigator Navigator { get; } = new();

    /// <summary>
    /// Der Log-Sammler.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected RecordingLogger<MapViewModel> Logger { get; } = new();

    /// <summary>
    /// Das zu testende ViewModel.
    /// </summary>
    protected MapViewModel ViewModel { get; }

    /// <summary>
    /// Die aktuelle Uhrzeit der Testuhr in UTC.
    /// </summary>
    protected DateTime Now => Clock.UtcNow;

    /// <summary>
    /// Lässt die Seite erscheinen und wartet, bis die Einstellungen geladen sind.
    /// </summary>
    /// <returns>Ein Task, der nach dem Laden abgeschlossen ist.</returns>
    protected async Task AppearAsync()
    {
        ViewModel.OnAppearing();
        await ViewModel.LastSettingsTask;
        await ViewModel.LastRouteAccessTask;
    }

    /// <summary>
    /// Lässt die Seite erscheinen, wählt die Suchart (bei „Adresse“ mit der Eingabe „Berlin“), führt auf Wunsch eine Suche aus und meldet danach einen Kartenausschnitt
    /// um den Teststandort (400 × 700 Pixel) als Bewegung des Anwenders.
    /// </summary>
    /// <param name="mode">Die Suchart.</param>
    /// <param name="zoom">Die Zoomstufe des gemeldeten Ausschnitts (13 ergibt rund 5 km Radius, 8 einen zu großen Ausschnitt).</param>
    /// <param name="search">Gibt an, ob vor der Bewegung gesucht wird.</param>
    /// <returns>Der gemeldete Ausschnitt.</returns>
    protected async Task<MapViewport> AppearAndViewportAsync(SearchMode mode = SearchMode.CurrentLocation, int zoom = 13, bool search = true)
    {
        await AppearAsync();
        ViewModel.ModeOptions.Single(option => option.Value == mode).SelectCommand.Execute(null);
        if (mode == SearchMode.Address)
        {
            ViewModel.AddressText = "Berlin";
        }

        if (search)
        {
            await SearchAsync();
        }

        var viewport = MapViewport.Create(StationFactory.CenterLatitude, StationFactory.CenterLongitude, zoom, 400, 700);
        ViewModel.OnViewportChanged(viewport);
        return viewport;
    }

    /// <summary>
    /// Führt die Suche aus.
    /// </summary>
    /// <returns>Ein Task, der nach der Suche abgeschlossen ist.</returns>
    protected async Task SearchAsync()
    {
        await ViewModel.SearchAsync();
    }

    /// <summary>
    /// Erzeugt ein Suchergebnis.
    /// </summary>
    /// <param name="source">Die Datenherkunft.</param>
    /// <param name="failure">Der Fehlergrund.</param>
    /// <param name="stations">Die Tankstellen.</param>
    /// <returns>Das Ergebnis.</returns>
    protected static StationSearchResult Result(PriceDataSource source, PriceFailure failure, params StationInfo[] stations)
    {
        return new StationSearchResult(stations, source, failure);
    }

    /// <summary>
    /// Erzeugt ein erfolgreiches Ergebnis der Routensuche (Berlin nach Oranienburg, rund 40 km).
    /// </summary>
    /// <param name="startName">Der aufgelöste Startname.</param>
    /// <param name="destinationName">Der aufgelöste Zielname.</param>
    /// <param name="stations">Die Tankstellen im Abstandsband.</param>
    /// <returns>Das Ergebnis.</returns>
    protected static RouteSearchResult RouteSuccess(string? startName, string? destinationName, params RouteStation[] stations)
    {
        return RouteSearchResult.Success(
            RouteTestData.NorthFromBerlin(0.36),
            new GeoPosition(52.52, 13.405),
            new GeoPosition(52.88, 13.405),
            startName,
            destinationName,
            stations);
    }

    /// <summary>
    /// Erzeugt zwei Teststationen mit allen Sorten.
    /// </summary>
    /// <returns>Die Stationen.</returns>
    protected StationInfo[] TwoStations()
    {
        return
        [
            StationFactory.CreateCustom(1, "Nahe", 1.0, Now, true, true, (FuelType.SuperE5, 1.90m), (FuelType.SuperE10, 1.85m), (FuelType.Diesel, 1.60m)),
            StationFactory.CreateCustom(2, "Ferne", 5.0, Now, false, true, (FuelType.SuperE5, 1.80m), (FuelType.SuperE10, 1.75m)),
        ];
    }
}

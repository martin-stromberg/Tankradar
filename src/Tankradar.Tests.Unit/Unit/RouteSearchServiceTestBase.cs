using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Geocoding;
using Tankradar.MAUI.Services.Routing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Gemeinsamer Aufbau der Tests des Routensuchdienstes: Fakes für Ortsauflösung, Routing und Preise.
/// </summary>
public abstract class RouteSearchServiceTestBase : BaseTest
{
    /// <summary>
    /// Der Eingabetext des Starts.
    /// </summary>
    protected const string StartText = "10115";

    /// <summary>
    /// Der Eingabetext des Ziels.
    /// </summary>
    protected const string DestinationText = "Oranienburg";

    /// <summary>
    /// Erstellt den Aufbau mit gültigen Orten und einer Route von Berlin nach Oranienburg.
    /// </summary>
    protected RouteSearchServiceTestBase()
    {
        Geocoding.Results[StartText] = GeocodingResult.Success(new GeoPosition(52.52, 13.405), "Berlin-Mitte");
        Geocoding.Results[DestinationText] = GeocodingResult.Success(new GeoPosition(52.88, 13.405), "Oranienburg, Brandenburg");
        Routing.Result = RoutingResult.Success(RouteTestData.NorthFromBerlin(0.36));
        Service = new RouteSearchService(Geocoding, Routing, new RouteStationSearchService(Prices), new RouteSearchOptions());
    }

    /// <summary>
    /// Die Ortsauflösung.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected ScriptedGeocodingService Geocoding { get; } = new();

    /// <summary>
    /// Der Routingdienst.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeRoutingService Routing { get; } = new();

    /// <summary>
    /// Der Preisdienst.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected ScriptedFuelPriceService Prices { get; } = new();

    /// <summary>
    /// Der zu testende Dienst.
    /// </summary>
    protected RouteSearchService Service { get; set; }

    /// <summary>
    /// Erzeugt einen Auftrag mit Adressstart.
    /// </summary>
    /// <param name="distanceKm">Der Abstand.</param>
    /// <returns>Der Auftrag.</returns>
    protected static RouteSearchRequest Request(int distanceKm = 2)
    {
        return new RouteSearchRequest(null, StartText, DestinationText, distanceKm, [FuelType.SuperE5]);
    }

    /// <summary>
    /// Führt die Suche aus.
    /// </summary>
    /// <param name="request">Der Auftrag oder <see langword="null"/> für <see cref="Request"/>.</param>
    /// <param name="progress">Der Fortschrittsempfänger oder <see langword="null"/>.</param>
    /// <returns>Das Ergebnis.</returns>
    protected Task<RouteSearchResult> SearchAsync(RouteSearchRequest? request = null, IProgress<RouteSearchProgress>? progress = null)
    {
        return Service.SearchAsync(request ?? Request(), progress, CancellationToken.None);
    }
}

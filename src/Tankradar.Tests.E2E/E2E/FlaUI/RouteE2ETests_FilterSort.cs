using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Die Ergebnisse der Routensuche bieten in der Trefferliste die gewohnten Filter und Sortierungen (ohne erneute Dienstaufrufe), die Karte zeigt die eingezeichnete Route.
/// </summary>
public class RouteE2ETests_FilterSort : RouteE2ETestBase
{
    /// <summary>
    /// Prüft Spritsortenfilter und Sortierungen nach Preis, Name und Entfernung ohne weitere Anfragen.
    /// </summary>
    [Fact]
    public void FilterAndSort_ApplyToRouteResultWithoutNewRequests()
    {
        RunWithDiagnostics(() =>
        {
            OpenRouteSearch();
            FillRoute("10115", "Oranienburg");
            Submit();
            WaitForStationNames(TwoKilometerStations);

            SelectSort("Name");
            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle", "Epsilon Tankstelle", "Gamma Tankstelle", "Route Nah");

            SelectFilter("Diesel");
            WaitForStationNames("Alpha Tankstelle", "Epsilon Tankstelle", "Gamma Tankstelle", "Route Nah");

            SelectSort("Price");
            WaitForStationNames("Route Nah", "Gamma Tankstelle", "Epsilon Tankstelle", "Alpha Tankstelle");

            SelectSort("Distance");
            WaitForStationNames("Alpha Tankstelle", "Gamma Tankstelle", "Route Nah", "Epsilon Tankstelle");

            Assert.Equal(2, Geocoding.Requests);
            Assert.Equal(1, Routing.Requests);
            Assert.Equal(1, Server.ListRequests);
        });
    }

    /// <summary>
    /// Prüft die Karte mit eingezeichneter Route: Beschreibung der Route, Marker der Tankstellen, Quellenangabe des Routingdienstes und erneutes Ausklappen der Liste ohne neue Anfrage.
    /// </summary>
    [Fact]
    public void MapView_DescribesDrawnRouteAndShowsMarkers()
    {
        RunWithDiagnostics(() =>
        {
            OpenRouteSearch();
            FillRoute("10115", "Oranienburg");
            Submit();
            WaitForStationNames(TwoKilometerStations);

            CollapsePanel();

            var hint = $"Route von {StartName} nach {DestinationName} eingezeichnet, 5 Tankstellen";
            WaitUntil(() => FindByAutomationId("Search.Map.Hint")?.Properties.Name.ValueOrDefault == hint, $"Der Hinweis der Karte lautet nicht '{hint}'.");
            WaitForAutomationId("Map.Destination");
            WaitForAutomationId("Map.Marker." + MockTankerkoenigServer.StationAlpha);
            Assert.Equal("Routen: OpenRouteService, Geodaten © OpenStreetMap-Mitwirkende", WaitForAutomationId("Map.Attribution.Routing").Name);

            ExpandPanel();

            WaitForStationNames(TwoKilometerStations);
            Assert.Equal(1, Routing.Requests);
        });
    }
}

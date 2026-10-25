using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Grundablauf der Routensuche gegen die Mock-Dienste (Nominatim, OpenRouteService, Tankerkönig): Modus „Route“, Start „10115“, Ziel „Oranienburg“, Abstand 2 km,
/// Stationen in Reihenfolge der Streckenposition, Entfernung „ab Start“, Zusammenfassung und Quellenangabe. Produktive Endpunkte werden nie berührt.
/// </summary>
public class RouteE2ETests_Basic : RouteE2ETestBase
{
    /// <summary>
    /// Prüft Eingabe, Route, Auswahl der Stationen innerhalb von 2 km, Sortierung nach Streckenposition, Entfernungstexte, Zusammenfassung und Quellenangabe.
    /// </summary>
    [Fact]
    public void RouteSearch_ShowsStationsAlongRouteInOrder()
    {
        RunWithDiagnostics(() =>
        {
            OpenRouteSearch();
            Assert.Equal("Routen: OpenRouteService, Geodaten © OpenStreetMap-Mitwirkende", WaitForAutomationId("Search.Attribution.Routing").Name);
            Assert.False(Exists("Search.Radius.5"));
            FillRoute("10115", "Oranienburg");

            Submit();

            WaitForStationNames(TwoKilometerStations);
            var distances = ReadTexts("Search.Station.Distance");
            Assert.Equal(5, distances.Count);
            Assert.Equal("0,0 km ab Start", distances[0]);
            Assert.All(distances, text => Assert.EndsWith(" km ab Start", text, StringComparison.Ordinal));
            Assert.Equal("40,0 km ab Start", distances[^1]);
            var offsets = ReadTexts("Search.Station.RouteOffset");
            Assert.Equal(5, offsets.Count);
            Assert.All(offsets, text => Assert.EndsWith(" km abseits der Route", text, StringComparison.Ordinal));
            WaitForRouteSummary(Summary(5, 2));
            Assert.False(Exists("Search.StatusMessage"));
            Assert.False(Exists("Search.EmptyState"));
        });
    }

    /// <summary>
    /// Prüft die Aufrufe der Dienste: Start und Ziel über Nominatim, eine Route, eine Umkreisabfrage innerhalb der Grenze von 25 km, Schlüssel nur im Kopf.
    /// </summary>
    [Fact]
    public void RouteSearch_CallsServicesAsPlanned()
    {
        RunWithDiagnostics(() =>
        {
            OpenRouteSearch();
            FillRoute("10115", "Oranienburg");

            Submit();

            WaitForStationNames(TwoKilometerStations);
            Assert.Equal(2, Geocoding.Requests);
            Assert.Equal(1, Routing.Requests);
            Assert.Equal(1, Server.ListRequests);
            Assert.InRange(Server.LastListRadius!.Value, 1, 25);
            Assert.Equal(MockOpenRouteServiceServer.AcceptedKey, Routing.LastAuthorization);
            Assert.DoesNotContain(MockOpenRouteServiceServer.AcceptedKey, Routing.LastRawRequest, StringComparison.Ordinal);
            Assert.True((Geocoding.RequestTimes[1] - Geocoding.RequestTimes[0]).TotalMilliseconds >= 1000);
            Assert.Contains("Tankatlas", Routing.LastUserAgent, StringComparison.Ordinal);
        });
    }
}

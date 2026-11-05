namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Start am aktuellen Standort (fester Teststandort Berlin-Mitte): Nur das Ziel wird über Nominatim aufgelöst, der Standort geht als Startposition an den Routingdienst.
/// </summary>
public class RouteE2ETests_CurrentLocation : RouteE2ETestBase
{
    /// <summary>
    /// Prüft den Ablauf ohne Startadresse.
    /// </summary>
    [Fact]
    public void CurrentLocationStart_SearchesWithoutStartAddress()
    {
        RunWithDiagnostics(() =>
        {
            OpenRouteSearch();
            Assert.True(IsChipSelected("Search.Route.StartKind.CurrentLocation"));
            Assert.False(Exists("Search.Route.Start.Input"));
            SetRouteDestination("Oranienburg");

            Submit();

            WaitForStationNames(TwoKilometerStations);
            Assert.Equal(1, Geocoding.Requests);
            Assert.Equal("Oranienburg", Geocoding.LastQuery);
            Assert.Equal(1, Routing.Requests);
            Assert.Contains("[[13.405,52.52],[13.405,52.88]]", Routing.LastRequestBody, StringComparison.Ordinal);
            WaitForRouteSummary($"Von Aktueller Standort nach {DestinationName}, 40 km, 5 Tankstellen im Abstand von 2 km zur Route");
        });
    }
}

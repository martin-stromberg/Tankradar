namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Der einstellbare Abstand zur Route (1, 2, 5, 10 km) ändert die Treffermenge; die Zahl der Tankerkönig-Abfragen je Suche bleibt innerhalb der Obergrenze von 15.
/// </summary>
public class RouteE2ETests_Distance : RouteE2ETestBase
{
    /// <summary>
    /// Prüft die Treffermenge für alle Abstandsstufen und die Obergrenze der Abfragen.
    /// </summary>
    [Fact]
    public void DistanceSteps_ChangeNumberOfStations()
    {
        RunWithDiagnostics(() =>
        {
            OpenRouteSearch();
            Assert.True(IsChipSelected("Search.RouteDistance.2"));
            FillRoute("10115", "Oranienburg");
            Submit();
            WaitForStationNames(TwoKilometerStations);
            var queriesAfterFirst = Server.ListRequests;

            SelectRouteDistance("1");
            Submit();
            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle", "Gamma Tankstelle", "Epsilon Tankstelle");
            WaitForRouteSummary(Summary(4, 1));

            SelectRouteDistance("5");
            Submit();
            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle", "Gamma Tankstelle", "Route Nah", "Route Mittel", "Epsilon Tankstelle");

            SelectRouteDistance("10");
            Submit();
            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle", "Gamma Tankstelle", "Route Nah", "Route Mittel", "Epsilon Tankstelle", "Route Dahinter");
            WaitForRouteSummary(Summary(7, 10));

            Assert.Equal(4, Routing.Requests);
            Assert.InRange(queriesAfterFirst, 1, 15);
            Assert.InRange(Server.ListRequests, 4, 4 * 15);
        });
    }
}

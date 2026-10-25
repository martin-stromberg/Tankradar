namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Bei der Standortnutzung „Nie“ meldet die Routensuche mit Start am aktuellen Standort einen Hinweis und löst keinen externen Aufruf aus.
/// </summary>
public class RouteE2ETests_GpsNever : RouteE2ETestBase
{
    /// <summary>
    /// Prüft Hinweis und fehlende Aufrufe; mit Adressstart funktioniert die Routensuche trotzdem.
    /// </summary>
    [Fact]
    public void GpsNever_WithCurrentLocationStart_ShowsHintAndMakesNoCalls()
    {
        RunWithDiagnostics(() =>
        {
            OpenSettings();
            SelectRadio("Settings.Gps.Never");
            OpenRouteSearch();
            SetRouteDestination("Oranienburg");

            Submit();

            WaitForStatusMessage("Die Standortnutzung ist ausgeschaltet. Du kannst sie unter „Optionen“ ändern.");
            Assert.False(Exists("Search.Station.Name"));
            Assert.Equal(0, Server.TotalRequests);
            Assert.Equal(0, Geocoding.Requests);
            Assert.Equal(0, Routing.Requests);

            SetRouteStart("10115");
            Submit();

            WaitForStationNames(TwoKilometerStations);
            Assert.Equal(1, Routing.Requests);
        });
    }
}

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Fehlt der Schlüssel des Routingdienstes, meldet die Routensuche „nicht verfügbar“, und die übrige Suche bleibt nutzbar.
/// </summary>
public class RouteE2ETests_KeyMissing : RouteE2ETestBase
{
    /// <summary>
    /// Startet die App ohne Schlüssel für den Routingdienst.
    /// </summary>
    public RouteE2ETests_KeyMissing()
        : base(routingKey: null)
    {
    }

    /// <summary>
    /// Prüft Meldung, fehlende Aufrufe und die weiterhin funktionierende Umkreis- und Adresssuche.
    /// </summary>
    [Fact]
    public void MissingKey_ReportsUnavailableAndOtherModesStillWork()
    {
        RunWithDiagnostics(() =>
        {
            OpenRouteSearch();
            FillRoute("10115", "Oranienburg");

            Submit();

            WaitForStatusMessage("Routensuche nicht verfügbar. Die übrige Suche funktioniert weiterhin.");
            Assert.Equal(0, Geocoding.Requests);
            Assert.Equal(0, Routing.Requests);
            Assert.Equal(0, Server.TotalRequests);

            SelectLocationMode();
            Submit();
            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");

            SelectAddressMode();
            SetAddress("Oranienburg");
            SetRadius("1");
            Submit();
            WaitForStationNames("Epsilon Tankstelle");
        });
    }
}

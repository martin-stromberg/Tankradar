namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Fehler der Routensuche werden verständlich gemeldet (Eingabe, Ort, Route, Routingdienst, Preisdienst), und Eingaben werden vor externen Aufrufen geprüft.
/// </summary>
public class RouteE2ETests_Errors : RouteE2ETestBase
{
    private void SearchExpecting(string start, string destination, string message)
    {
        OpenRouteSearch();
        FillRoute(start, destination);
        Submit();
        WaitForStatusMessage(message);
        Assert.False(Exists("Search.Station.Name"));
        Assert.False(Exists("Search.Route.Summary"));
    }

    /// <summary>
    /// Prüft, dass ein unbekanntes Ziel gemeldet wird, ohne die Route zu berechnen oder Preise abzufragen.
    /// </summary>
    [Fact]
    public void UnknownDestination_ShowsNotFoundMessageWithoutRouting()
    {
        RunWithDiagnostics(() =>
        {
            SearchExpecting("10115", "Nirgendwoburg", "Zum Ziel wurde in Deutschland kein Ort gefunden. Bitte die Schreibweise prüfen oder genauer angeben.");

            Assert.Equal(0, Routing.Requests);
            Assert.Equal(0, Server.ListRequests);
        });
    }

    /// <summary>
    /// Prüft, dass eine zu kurze Eingabe vor jedem externen Aufruf abgelehnt wird.
    /// </summary>
    [Fact]
    public void TooShortInput_IsRejectedBeforeAnyExternalCall()
    {
        RunWithDiagnostics(() =>
        {
            SearchExpecting("10115", "ab", "Ziel: Die Eingabe ist zu kurz. Bitte mindestens 3 Zeichen eingeben.");

            Assert.Equal(0, Geocoding.Requests);
            Assert.Equal(0, Routing.Requests);
            Assert.Equal(0, Server.TotalRequests);
        });
    }

    /// <summary>
    /// Prüft, dass eine nicht berechenbare Route (ORS 404) gemeldet wird.
    /// </summary>
    [Fact]
    public void RouteNotFound_ShowsMessage()
    {
        RunWithDiagnostics(() =>
        {
            Routing.EnqueueError(404, 2010);

            SearchExpecting("10115", "Oranienburg", "Zwischen Start und Ziel konnte keine Route berechnet werden.");

            Assert.Equal(1, Routing.Requests);
            Assert.Equal(0, Server.ListRequests);
        });
    }

    /// <summary>
    /// Prüft die Wiederholung bei ausgelastetem Routingdienst (429 dreimal) und die Meldung zum Kontingent.
    /// </summary>
    [Fact]
    public void TooManyRequests_IsRetriedAndShowsQuotaMessage()
    {
        RunWithDiagnostics(() =>
        {
            Routing.EnqueueStatuses(429, 429, 429);

            SearchExpecting("10115", "Oranienburg", "Der Routendienst ist gerade ausgelastet oder das Tageskontingent ist aufgebraucht. Bitte später erneut versuchen.");

            Assert.Equal(3, Routing.Requests);
            Assert.Equal(0, Server.ListRequests);
        });
    }

    /// <summary>
    /// Prüft die Meldung bei nicht erreichbarem Routingdienst.
    /// </summary>
    [Fact]
    public void RoutingUnreachable_ShowsMessage()
    {
        RunWithDiagnostics(() =>
        {
            Routing.EnqueueStatuses(503, 503, 503);

            SearchExpecting("10115", "Oranienburg", "Der Routendienst ist nicht erreichbar. Bitte versuche es später erneut.");

            Assert.Equal(3, Routing.Requests);
        });
    }

    /// <summary>
    /// Prüft die Meldung bei zu langer Route (Mock liefert rund 600 km) und dass keine Tankstellen abgefragt werden.
    /// </summary>
    [Fact]
    public void RouteTooLong_ShowsMessageWithoutPriceQueries()
    {
        RunWithDiagnostics(() =>
        {
            Routing.SetRoute([(52.52, 13.405), (50.0, 13.405), (47.2, 13.405)]);

            SearchExpecting("10115", "Oranienburg", "Die Route ist zu lang. Es werden Routen bis 400 km unterstützt.");

            Assert.Equal(1, Routing.Requests);
            Assert.Equal(0, Server.ListRequests);
        });
    }

    /// <summary>
    /// Prüft die Meldung bei einem Fehler des Preisdienstes (ohne Teilergebnisse).
    /// </summary>
    [Fact]
    public void PriceServiceError_ShowsMessageWithoutPartialResults()
    {
        RunWithDiagnostics(() =>
        {
            Server.EnqueueStatuses(503, 503, 503);

            SearchExpecting("10115", "Oranienburg", "Der Preisdienst ist nicht erreichbar.");
        });
    }

    /// <summary>
    /// Prüft, dass Start und Ziel am selben Ort ohne Routenberechnung gemeldet werden.
    /// </summary>
    [Fact]
    public void SamePlace_ShowsMessageWithoutRouting()
    {
        RunWithDiagnostics(() =>
        {
            SearchExpecting("10115", "Berlin", "Start und Ziel liegen am selben Ort. Bitte ein anderes Ziel wählen.");

            Assert.Equal(0, Routing.Requests);
        });
    }
}

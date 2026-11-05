using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Die Elemente der Routensuche haben die erwarteten AutomationIds und Namen für Bedienhilfen; Fortschritt und Fehlermeldung sind lesbar.
/// </summary>
public class RouteE2ETests_Accessibility : RouteE2ETestBase
{
    private const string ProgressRoute = "Route wird berechnet …";

    /// <summary>
    /// Prüft AutomationIds, Namen und Auswahlzustände der Elemente der Routensuche.
    /// </summary>
    [Fact]
    public void RouteElements_HaveAutomationIdsAndNames()
    {
        RunWithDiagnostics(() =>
        {
            OpenRouteSearch();
            SetRouteStart("10115");

            Assert.Equal("Route", WaitForAutomationId("Search.Mode.Route").Name);
            Assert.Equal("Aktueller Standort", WaitForAutomationId("Search.Route.StartKind.CurrentLocation").Name);
            Assert.Equal("Adresse, Ort oder PLZ", WaitForAutomationId("Search.Route.StartKind.Address").Name);
            Assert.Equal("Start: Adresse, PLZ oder Ort, z. B. Berlin", WaitForAutomationId("Search.Route.Start.Input").Name);
            Assert.Equal("Ziel: Adresse, PLZ oder Ort, z. B. Hamburg", WaitForAutomationId("Search.Route.Destination.Input").Name);
            Assert.Equal("Start löschen", WaitForAutomationId("Search.Route.Start.Clear").Name);
            Assert.Equal(["1 km", "2 km", "5 km", "10 km"], new[] { "1", "2", "5", "10" }.Select(key => WaitForAutomationId("Search.RouteDistance." + key).Name));
            Assert.True(IsChipSelected("Search.Mode.Route"));
            Assert.True(IsChipSelected("Search.RouteDistance.2"));
            Assert.True(IsChipSelected("Search.Route.StartKind.Address"));
            Assert.Equal("Routen: OpenRouteService, Geodaten © OpenStreetMap-Mitwirkende", WaitForAutomationId("Search.Attribution.Routing").Name);
        });
    }

    /// <summary>
    /// Prüft den lesbaren Fortschritt während der Routenberechnung und die Fehlermeldung als Statusmeldung.
    /// </summary>
    [Fact]
    public void Progress_IsReadableWhileRunning_AndErrorIsStatusMessage()
    {
        RunWithDiagnostics(() =>
        {
            Routing.ResponseDelay = TimeSpan.FromSeconds(1.5);
            OpenRouteSearch();
            FillRoute("10115", "Oranienburg");

            Submit();

            string progress = string.Empty;
            WaitUntil(
                () =>
                {
                    progress = FindByAutomationId("Search.Route.Progress")?.Properties.Name.ValueOrDefault ?? string.Empty;
                    return progress == ProgressRoute;
                },
                $"Der Fortschritt der Routenberechnung ist nicht lesbar, zuletzt: '{progress}'.");
            WaitForStationNames(TwoKilometerStations);
            WaitUntil(() => !Exists("Search.Route.Progress"), "Der Fortschritt wurde nach der Suche nicht entfernt.");

            SetRouteDestination("Nirgendwoburg");
            Submit();

            WaitForStatusMessage("Zum Ziel wurde in Deutschland kein Ort gefunden. Bitte die Schreibweise prüfen oder genauer angeben.");
            Assert.Equal("Search.StatusMessage", FindByAutomationId("Search.StatusMessage")?.AutomationId);
        });
    }
}

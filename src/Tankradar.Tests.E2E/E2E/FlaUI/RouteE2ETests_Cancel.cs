namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: "Suche abbrechen" beendet die laufende Routensuche; es erscheinen weder Ergebnisse noch eine Fehlermeldung, und es folgen keine Preisabfragen mehr.
/// </summary>
public class RouteE2ETests_Cancel : RouteE2ETestBase
{
    private const string ProgressRoute = "Route wird berechnet …";
    private const string CancelledMessage = "Die Routensuche wurde abgebrochen.";

    /// <summary>
    /// Prüft, dass der Abbruch während der Routenberechnung die Eingabe wieder freigibt, nichts anzeigt und keine Preisabfrage nachläuft.
    /// </summary>
    [Fact]
    public void CancelRouteSearch_WhileRunning_EndsWithoutResultAndWithoutPriceRequests()
    {
        RunWithDiagnostics(() =>
        {
            Routing.ResponseDelay = TimeSpan.FromSeconds(3);
            OpenRouteSearch();
            FillRoute("10115", "Oranienburg");

            Submit();

            WaitUntil(
                () => FindByAutomationId("Search.Route.Progress")?.Properties.Name.ValueOrDefault == ProgressRoute,
                "Der Fortschritt der Routenberechnung erscheint nicht.");
            // Nur UIA-Muster (Invoke), kein Mausklick und kein Fokus.
            WaitForAutomationId("Search.Route.Cancel").Patterns.Invoke.Pattern.Invoke();

            WaitForStatusMessage(CancelledMessage);
            WaitUntil(() => !Exists("Search.Route.Progress"), "Der Fortschritt wurde nach dem Abbruch nicht entfernt.");

            // Die verzögerte Antwort des Routingdienstes trifft nach dem Abbruch ein; sie darf keine Preisabfrage und kein Ergebnis mehr auslösen.
            Thread.Sleep(TimeSpan.FromSeconds(4));
            Assert.Equal(0, Server.ListRequests);
            Assert.Empty(ReadTexts("Search.Station.Name"));
            Assert.Equal(CancelledMessage, FindByAutomationId("Search.StatusMessage")?.Properties.Name.ValueOrDefault);
        });
    }
}

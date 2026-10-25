using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Routen, Start- und Zieleingaben werden nicht gespeichert; nach einem Neustart der App sind Felder und Ergebnis leer.
/// </summary>
public class RouteE2ETests_NotPersisted : RouteE2ETestBase
{
    /// <summary>
    /// Prüft, dass nach dem Neustart weder Eingaben noch Route noch Zusammenfassung vorhanden sind.
    /// </summary>
    [Fact]
    public void AfterRestart_FieldsAndRouteAreEmpty()
    {
        RunWithDiagnostics(() =>
        {
            OpenRouteSearch();
            FillRoute("10115", "Oranienburg");
            Submit();
            WaitForStationNames(TwoKilometerStations);
            WaitForRouteSummary(Summary(5, 2));

            RestartApplication(CreateEnvironment(
                Server.BaseUrl,
                TestLocation,
                MockTankerkoenigServer.AcceptedKey,
                Geocoding.BaseUrl,
                Tiles.BaseUrl,
                Routing.BaseUrl,
                MockOpenRouteServiceServer.AcceptedKey));
            OpenSearch();
            SelectRouteMode();

            Assert.True(IsChipSelected("Search.Route.StartKind.CurrentLocation"));
            Assert.False(Exists("Search.Route.Start.Input"));
            Assert.Equal(string.Empty, FindByAutomationId("Search.Route.Destination.Input")?.Patterns.Value.PatternOrDefault?.Value.ValueOrDefault ?? string.Empty);
            Assert.True(IsChipSelected("Search.RouteDistance.2"));
            Assert.False(Exists("Search.Route.Summary"));
            Assert.False(Exists("Search.Station.Name"));
            Assert.False(Exists("Search.StatusMessage"));
        });
    }
}

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Aus der Ergebnisliste der Routensuche lässt sich die Detailansicht einer Tankstelle öffnen.
/// </summary>
public class RouteE2ETests_OpenDetail : RouteE2ETestBase
{
    /// <summary>
    /// Prüft, dass das Öffnen der Detailansicht aus der Routenliste die gewählte Tankstelle zeigt.
    /// </summary>
    [Fact]
    public void OpenDetail_FromRouteList_ShowsSelectedStation()
    {
        RunWithDiagnostics(() =>
        {
            OpenRouteSearch();
            FillRoute("10115", "Oranienburg");
            Submit();
            WaitForStationNames(TwoKilometerStations);

            FindAllOrdered("Search.Station.Open")[2].Patterns.Invoke.Pattern.Invoke();

            WaitUntil(() => FindByAutomationId("Detail.Name")?.Properties.Name.ValueOrDefault == "Gamma Tankstelle", "Die Detailansicht zeigt nicht 'Gamma Tankstelle'.");
            Assert.Equal(1, Server.DetailRequests);
        });
    }
}

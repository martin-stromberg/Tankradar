namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Aus einer Tankstellenkachel der Startseite öffnet sich die Detailansicht.
/// </summary>
public class HomeE2ETests_OpenDetail : HomeE2ETestBase
{
    /// <summary>
    /// Prüft, dass „Details öffnen“ die Detailansicht der Tankstelle mit Preisen zeigt.
    /// </summary>
    [Fact]
    public void OpenStationTile_ShowsDetail()
    {
        RunWithDiagnostics(() =>
        {
            CreateGroupWithStation(0, "Alpha Tankstelle", "Arbeitsweg");
            OpenHome("Arbeitsweg");
            WaitForText("Favorites.Station.Name", "Alpha Tankstelle");

            Press("Favorites.Station.Open");

            WaitUntil(() => FindByAutomationId("Detail.Name")?.Name == "Alpha Tankstelle", "Die Detailansicht zeigt nicht 'Alpha Tankstelle'.");
            Assert.Equal("1,859 Euro pro Liter", WaitForAutomationId("Detail.Price.SuperE5").Name);
        });
    }
}

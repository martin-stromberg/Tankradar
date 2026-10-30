namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Die Trefferliste am unteren Rand zeigt ausgeklappt Filter, Sortierung und die Liste; ein Eintrag öffnet die Detailansicht.
/// </summary>
public class MapE2ETests_Panel : MapE2ETestBase
{
    /// <summary>
    /// Prüft, dass das Ausklappen der eingeklappten Trefferliste die Liste mit Filter und Sortierung zeigt und das Antippen eines Eintrags die Detailansicht öffnet;
    /// zurück auf der Karte bleibt die Trefferliste mit derselben Anzahl erhalten.
    /// </summary>
    [Fact]
    public void ExpandPanel_ShowsList_AndEntryOpensDetails()
    {
        RunWithDiagnostics(() =>
        {
            SearchAndShowMap();
            Assert.False(Exists("Search.Station.Name"));
            Assert.Equal("2 Tankstellen", ReadPanelCount());

            ExpandPanel();

            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
            Assert.True(Exists("Search.Filter.All"));
            Assert.True(Exists("Search.Sort.Price"));

            FindAllOrdered("Search.Station.Open")[1].Patterns.Invoke.Pattern.Invoke();

            WaitUntil(() => FindByAutomationId("Detail.Name")?.Name == "Beta Tankstelle", "Die Detailansicht von Beta wurde nicht geöffnet.");
            WaitForAutomationId("NavigationViewBackButton").Patterns.Invoke.Pattern.Invoke();

            WaitForPageTitle("Karte");
            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
            Assert.Equal("2 Tankstellen", ReadPanelCount());
            Assert.Equal(1, Server.ListRequests);
        });
    }
}

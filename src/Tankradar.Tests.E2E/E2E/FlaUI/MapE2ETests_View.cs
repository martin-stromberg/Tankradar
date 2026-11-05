using System.Text.RegularExpressions;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Trefferliste und Karte zeigen dieselben Ergebnisse, die Standardansicht aus den Einstellungen bestimmt nur den Startzustand der Trefferliste,
/// Quellenangabe und Legende liegen über der Karte und Kacheln kommen vom Mock-Kachelserver.
/// </summary>
public class MapE2ETests_View : MapE2ETestBase
{
    /// <summary>
    /// Prüft, dass Liste und Markierungen gleichzeitig dieselben Ergebnisse zeigen und Ein- und Ausklappen der Trefferliste keine neue Anfrage auslöst.
    /// </summary>
    [Fact]
    public void SwitchView_BetweenListAndMap_ShowsTheSameResults()
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();
            Assert.True(IsPanelExpanded());
            Submit();
            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
            WaitForMapCount("2 von 2 Stationen sichtbar");
            Assert.True(Exists(AlphaMarker));
            Assert.True(Exists(BetaMarker));

            CollapsePanel();

            WaitUntil(() => !Exists("Search.Station.Name"), "Die Liste wurde beim Einklappen nicht ausgeblendet.");
            WaitForMapCount("2 von 2 Stationen sichtbar");
            Assert.Equal("2 Tankstellen", ReadPanelCount());

            ExpandPanel();

            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
            Assert.True(Exists("Map.StationCount"));
            Assert.Equal(1, Server.ListRequests);
        });
    }

    /// <summary>
    /// Prüft, dass die Standardansicht „Karte“ aus den Einstellungen die Trefferliste eingeklappt öffnet (zunächst „Keine Ergebnisse“, nach der Suche mit Anzahl) und der Nutzer sie ausklappen kann.
    /// </summary>
    [Fact]
    public void DefaultViewFromSettings_Map_OpensSearchAsMap()
    {
        RunWithDiagnostics(() =>
        {
            OpenSettings();
            SelectRadio("Settings.View.Map");
            OpenSearch();

            Assert.False(IsPanelExpanded());
            WaitUntil(() => ReadPanelCount() == "Keine Ergebnisse", "Die Trefferliste zeigt nicht „Keine Ergebnisse“.");

            Submit();

            WaitForMapCount("2 von 2 Stationen sichtbar");
            WaitUntil(() => ReadPanelCount() == "2 Tankstellen", "Die Trefferliste zeigt nicht die Anzahl.");
            Assert.False(IsPanelExpanded());
            Assert.False(Exists("Search.Station.Name"));

            ExpandPanel();
            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
        });
    }

    /// <summary>
    /// Prüft die über der Karte liegende Quellenangabe (auch bei ausgeklappter Trefferliste), Zoomstufe und Legende sowie dass alle Kacheln vom Mock-Server stammen, mit identifizierender Kennung abgerufen werden und nur gültige Kachelpfade betreffen.
    /// </summary>
    [Fact]
    public void Map_ShowsAttributionLegendAndLoadsTilesFromMockServerOnly()
    {
        RunWithDiagnostics(() =>
        {
            SearchAndShowMap();

            Assert.Equal("© OpenStreetMap-Mitwirkende", WaitForAutomationId("Map.Attribution").Name);
            ExpandPanel();
            Assert.Equal("© OpenStreetMap-Mitwirkende", WaitForAutomationId("Map.Attribution").Name);
            Assert.False(WaitForAutomationId("Map.Attribution").Properties.IsOffscreen.ValueOrDefault);
            Assert.Matches(new Regex(@"^Zoom \d+$"), WaitForAutomationId("Map.Zoom").Name);
            Assert.True(Exists("Map.Legend"));
            WaitUntil(() => Tiles.Requests > 0, "Es wurden keine Kacheln vom Mock-Kachelserver abgerufen.");
            Assert.All(Tiles.Paths, path => Assert.Matches(new Regex(@"^/\d{1,2}/\d+/\d+\.png$"), path));
            Assert.All(Tiles.UserAgents, agent => Assert.Matches(new Regex(@"^Tankatlas/(?!0\.0\.0 )\d+(\.\d+){1,3} \(\+https://github\.com/martin-stromberg/Tankradar; [\w.]+\)$"), agent));
            Assert.Equal(1, Server.ListRequests);
        });
    }
}

using System.Text.RegularExpressions;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Wechsel zwischen Listen- und Kartenansicht der Suchergebnisse, Standardansicht aus den Einstellungen, Quellenangabe und Kacheln vom Mock-Kachelserver.
/// </summary>
public class MapE2ETests_View : MapE2ETestBase
{
    /// <summary>
    /// Prüft, dass die Suche zunächst als Liste erscheint, der Wechsel auf die Karte Liste und Markierungen austauscht (ohne neue Anfrage) und der Wechsel zurück die Liste wieder zeigt.
    /// </summary>
    [Fact]
    public void SwitchView_BetweenListAndMap_ShowsTheSameResults()
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();
            Assert.True(IsChipSelected("Search.View.List"));
            Submit();
            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
            Assert.False(Exists("Map.StationCount"));

            ShowMapView();

            WaitForMapCount("2 von 2 Stationen sichtbar");
            WaitUntil(() => !Exists("Search.Station.Name"), "Die Liste wurde in der Kartenansicht nicht ausgeblendet.");
            Assert.True(Exists(AlphaMarker));
            Assert.True(Exists(BetaMarker));
            Assert.False(IsChipSelected("Search.View.List"));

            ShowListView();

            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
            WaitUntil(() => !Exists("Map.StationCount"), "Die Karte wurde in der Listenansicht nicht ausgeblendet.");
            Assert.Equal(1, Server.ListRequests);
        });
    }

    /// <summary>
    /// Prüft, dass die Standardansicht „Karte“ aus den Einstellungen die Suchseite in der Kartenansicht öffnet (zunächst mit Hinweis, dann mit Ergebnissen) und der Nutzer wieder auf die Liste wechseln kann.
    /// </summary>
    [Fact]
    public void DefaultViewFromSettings_Map_OpensSearchAsMap()
    {
        RunWithDiagnostics(() =>
        {
            OpenSettings();
            SelectRadio("Settings.View.Map");
            OpenSearch();

            WaitUntil(() => IsChipSelected("Search.View.Map"), "Die Kartenansicht ist nicht vorgewählt.");
            Assert.Equal("Noch keine Ergebnisse. Starte eine Suche, um die Tankstellen auf der Karte zu sehen.", WaitForAutomationId("Search.Map.Hint").Name);
            Assert.False(Exists("Map.StationCount"));

            Submit();

            WaitForMapCount("2 von 2 Stationen sichtbar");
            Assert.False(Exists("Search.Map.Hint"));
            Assert.False(Exists("Search.Station.Name"));

            ShowListView();
            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
        });
    }

    /// <summary>
    /// Prüft die sichtbare Quellenangabe, Zoomstufe und Legende sowie dass alle Kacheln vom Mock-Server stammen, mit identifizierender Kennung abgerufen werden und nur gültige Kachelpfade betreffen.
    /// </summary>
    [Fact]
    public void Map_ShowsAttributionLegendAndLoadsTilesFromMockServerOnly()
    {
        RunWithDiagnostics(() =>
        {
            SearchAndShowMap();

            Assert.Equal("© OpenStreetMap-Mitwirkende", WaitForAutomationId("Map.Attribution").Name);
            Assert.Matches(new Regex(@"^Zoom \d+$"), WaitForAutomationId("Map.Zoom").Name);
            Assert.True(Exists("Map.Legend"));
            WaitUntil(() => Tiles.Requests > 0, "Es wurden keine Kacheln vom Mock-Kachelserver abgerufen.");
            Assert.All(Tiles.Paths, path => Assert.Matches(new Regex(@"^/\d{1,2}/\d+/\d+\.png$"), path));
            Assert.All(Tiles.UserAgents, agent => Assert.Matches(new Regex(@"^Tankatlas/(?!0\.0\.0 )\d+(\.\d+){1,3} \(\+https://github\.com/martin-stromberg/Tankradar; [\w.]+\)$"), agent));
            Assert.Equal(1, Server.ListRequests);
        });
    }
}

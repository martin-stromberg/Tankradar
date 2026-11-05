namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Die Karte füllt die Ansicht; darüber liegen Suchschaltfläche, Legende und Quellenangabe, am unteren Rand die Trefferliste mit Trefferanzahl.
/// </summary>
public class MapE2ETests_Layout : MapE2ETestBase
{
    /// <summary>
    /// Prüft, dass die Karte die Seite bis unter die eingeklappte Trefferliste füllt, die Trefferliste bei Standardansicht „Karte“ eingeklappt ist und die Anzahl zeigt
    /// und die Symbolschaltfläche, Legende und Quellenangabe über der Karte liegen.
    /// </summary>
    [Fact]
    public void Map_FillsView_PanelCollapsedShowsCount()
    {
        RunWithDiagnostics(() =>
        {
            OpenSettings();
            SelectRadio("Settings.View.Map");
            OpenSearch();

            Assert.False(IsPanelExpanded());
            WaitUntil(() => ReadPanelCount() == "Keine Ergebnisse", "Die Trefferliste zeigt nicht „Keine Ergebnisse“.");
            Assert.True(Exists("Map.Legend"));
            Assert.True(Exists("Map.Attribution"));
            Assert.False(Exists("Search.Submit"));
            Assert.False(Exists("Search.View.List"));

            Submit();

            WaitForMapCount("2 von 2 Stationen sichtbar");
            WaitUntil(() => ReadPanelCount() == "2 Tankstellen", "Die Trefferliste zeigt nicht die Anzahl der Treffer.");
            Assert.False(IsPanelExpanded());

            var map = WaitForAutomationId("Search.Map").BoundingRectangle;
            var panel = WaitForAutomationId("Search.Panel.Toggle").BoundingRectangle;
            var search = WaitForAutomationId("Search.Overlay.Open").BoundingRectangle;
            Assert.True(map.Height > 0 && map.Width > 0, "Die Karte hat keine Fläche.");
            Assert.True(map.Bottom >= panel.Bottom - 2, $"Die Karte reicht nicht bis unter die Trefferliste (Karte unten {map.Bottom}, Trefferliste unten {panel.Bottom}).");
            Assert.True(panel.Top - map.Top > map.Height / 2, "Die eingeklappte Trefferliste nimmt mehr als die halbe Kartenhöhe ein.");
            Assert.True(search.Left - map.Left < map.Width / 4 && search.Top - map.Top < map.Height / 4, "Die Suchschaltfläche liegt nicht oben links auf der Karte.");
        });
    }
}

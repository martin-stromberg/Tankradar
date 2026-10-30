using System.Text.RegularExpressions;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Nach Verschieben oder Zoomen erscheint „Tankstellen in diesem Bereich laden“; erst die Schaltfläche fragt den sichtbaren Ausschnitt ab,
/// ersetzt Anzahl und Markierungen und behält den Ausschnitt. Ohne Bewegung erscheint keine Schaltfläche, bei zu großem Ausschnitt ist sie gesperrt.
/// </summary>
public class MapE2ETests_AreaReload : MapE2ETestBase
{
    /// <summary>
    /// Prüft, dass nach der Suche ohne Bewegung keine Schaltfläche erscheint und keine weitere Anfrage gesendet wird.
    /// </summary>
    [Fact]
    public void WithoutMovement_NoReloadButton()
    {
        RunWithDiagnostics(() =>
        {
            SearchAndShowMap();
            ExpandPanel();
            CollapsePanel();

            Assert.False(IsAreaReloadShown());
            Assert.Equal(1, Server.ListRequests);
        });
    }

    /// <summary>
    /// Prüft, dass Verschieben und Zoomen die Schaltfläche zeigen, ohne selbst abzufragen.
    /// </summary>
    [Fact]
    public void PanOrZoom_ShowsReloadButtonWithoutQuery()
    {
        RunWithDiagnostics(() =>
        {
            SearchAndShowMap();

            PressMapButton("Map.Pan.East");

            WaitUntil(IsAreaReloadShown, "Nach dem Verschieben erscheint keine Schaltfläche zum Nachladen.");
            Assert.Equal("Tankstellen in diesem Bereich laden", WaitForAutomationId("Map.ReloadArea").Name);
            Assert.True(IsAreaReloadEnabled());

            PressMapButton("Map.ZoomOut");

            Assert.True(IsAreaReloadShown());
            Assert.Equal(1, Server.ListRequests);
        });
    }

    /// <summary>
    /// Prüft, dass das Nachladen genau eine Anfrage um die neue Mitte sendet, Anzahl und Markierungen aktualisiert, den Ausschnitt (Zoomstufe) nicht neu einpasst
    /// und die Schaltfläche ausblendet; eine weitere Bewegung zeigt innerhalb der Sperre den Hinweis mit der Wartezeit bei inaktiver Schaltfläche.
    /// </summary>
    [Fact]
    public void ReloadArea_UpdatesCountAndMarkers_KeepsViewport()
    {
        RunWithDiagnostics(() =>
        {
            SearchAndShowMap();
            var searchedLongitude = Server.LastListLongitude!.Value;
            PressMapButton("Map.ZoomOut");
            PressMapButton("Map.Pan.East");
            PressMapButton("Map.Pan.East");
            var zoom = ReadZoom();
            WaitUntil(() => IsAreaReloadShown() && IsAreaReloadEnabled(), "Die Schaltfläche zum Nachladen ist nicht bedienbar.");

            PressAreaReload();

            WaitUntil(() => Server.ListRequests == 2, "Das Nachladen hat keine Anfrage gesendet.");
            WaitUntil(() => !IsAreaReloadShown(), "Die Schaltfläche blieb nach dem Nachladen sichtbar.");
            Assert.True(Server.LastListLongitude!.Value > searchedLongitude, "Die Anfrage verwendet nicht die nach Osten verschobene Mitte.");
            Assert.InRange(Server.LastListRadius!.Value, 1, 25);
            Assert.Equal(zoom, ReadZoom());
            var count = ReadMapCount();
            Assert.Matches(new Regex(@"^\d+ von \d+ Stationen? sichtbar$"), count);
            var parts = count.Split(' ');
            var visible = int.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
            var total = int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(visible, FindMarkers().Count);
            // Die Formulierung der Kopfzeile prüfen die Unit-Tests (StationCountText); hier zählt, dass sie die neue Gesamtzahl nennt („Keine Ergebnisse“ ohne Zahl).
            var panelTotal = Regex.Match(ReadPanelCount(), @"^\d+").Value;
            Assert.Equal(total, panelTotal.Length == 0 ? 0 : int.Parse(panelTotal, System.Globalization.CultureInfo.InvariantCulture));

            PressMapButton("Map.Pan.West");

            WaitUntil(IsAreaReloadShown, "Nach erneutem Verschieben erscheint keine Schaltfläche.");
            WaitUntil(
                () => Regex.IsMatch(FindByAutomationId("Map.ReloadArea.Hint")?.Name ?? string.Empty, @"^Der Bereich kann in \d+ s erneut geladen werden\.$"),
                "Der Hinweis auf die Wartezeit erscheint nicht.");
            Assert.False(IsAreaReloadEnabled(), "Die Schaltfläche ist während der Sperre bedienbar.");
            Assert.Equal(2, Server.ListRequests);
        });
    }

    /// <summary>
    /// Prüft, dass bei einem Ausschnitt über 25 km Radius der Hinweis „Bitte näher heranzoomen“ erscheint und die Schaltfläche gesperrt ist.
    /// </summary>
    [Fact]
    public void TooLargeArea_ShowsHintAndDisablesButton()
    {
        RunWithDiagnostics(() =>
        {
            SearchAndShowMap();

            for (var step = 0; step < 8; step++)
            {
                PressMapButton("Map.ZoomOut");
            }

            WaitUntil(() => FindByAutomationId("Map.ReloadArea.Hint")?.Name == "Bitte näher heranzoomen", "Der Hinweis „Bitte näher heranzoomen“ erscheint nicht.");
            Assert.True(IsAreaReloadShown());
            Assert.False(IsAreaReloadEnabled());
            Assert.Equal(1, Server.ListRequests);
        });
    }
}

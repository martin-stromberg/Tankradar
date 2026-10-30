namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Zoomen und Verschieben der Karte, Zurücksetzen des Ausschnitts und Auf- und Zuklappen der Trefferliste (Bedienung ohne Geste über Schaltflächen per UI Automation).
/// </summary>
public class MapE2ETests_Navigation : MapE2ETestBase
{
    /// <summary>
    /// Prüft, dass Vergrößern und Verkleinern die Zoomstufe um eins ändern, bei der kleinsten und größten Stufe enden und die Karte danach weiter benutzbar bleibt.
    /// </summary>
    [Fact]
    public void Zoom_ChangesLevelWithinLimits()
    {
        RunWithDiagnostics(() =>
        {
            SearchAndShowMap();
            var start = ReadZoom();

            PressMapButton("Map.ZoomIn");
            WaitUntil(() => ReadZoom() == start + 1, "Die Karte wurde nicht vergrößert.");
            PressMapButton("Map.ZoomOut");
            PressMapButton("Map.ZoomOut");
            WaitUntil(() => ReadZoom() == start - 1, "Die Karte wurde nicht verkleinert.");

            for (var step = 0; step < 20; step++)
            {
                PressMapButton("Map.ZoomOut");
            }

            WaitUntil(() => ReadZoom() == 4, "Die kleinste Zoomstufe wurde nicht erreicht.");
            for (var step = 0; step < 20; step++)
            {
                PressMapButton("Map.ZoomIn");
            }

            WaitUntil(() => ReadZoom() == 18, "Die größte Zoomstufe wurde nicht erreicht.");
        });
    }

    /// <summary>
    /// Prüft, dass starkes Vergrößern die Markierungen aus dem Ausschnitt schiebt (Zähler sinkt) und das Zurücksetzen des Ausschnitts wieder alle zeigt.
    /// </summary>
    [Fact]
    public void ZoomIn_HidesDistantMarkers_AndRecenterShowsAll()
    {
        RunWithDiagnostics(() =>
        {
            SearchAndShowMap();
            var start = ReadZoom();

            for (var step = start; step < 18; step++)
            {
                PressMapButton("Map.ZoomIn");
            }

            WaitForMapCount("0 von 2 Stationen sichtbar");
            Assert.Empty(FindMarkers());

            PressMapButton("Map.Recenter");

            WaitForMapCount("2 von 2 Stationen sichtbar");
            Assert.Equal(start, ReadZoom());
        });
    }

    /// <summary>
    /// Prüft, dass die Schaltfläche am Kopf der Trefferliste diese ohne Geste ein- und ausklappt und ihre Beschriftung wechselt.
    /// </summary>
    [Fact]
    public void PanelToggleButton_CollapsesAndExpandsWithoutGesture()
    {
        RunWithDiagnostics(() =>
        {
            SearchAndShowMap();
            Assert.Equal("Liste einblenden", WaitForAutomationId("Search.Panel.Toggle").Name);
            Assert.False(Exists("Search.Station.Name"));

            WaitForAutomationId("Search.Panel.Toggle").Patterns.Invoke.Pattern.Invoke();

            WaitUntil(IsPanelExpanded, "Die Trefferliste wurde über die Schaltfläche nicht ausgeklappt.");
            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");

            WaitForAutomationId("Search.Panel.Toggle").Patterns.Invoke.Pattern.Invoke();

            WaitUntil(() => !Exists("Search.Station.Name"), "Die Trefferliste wurde über die Schaltfläche nicht eingeklappt.");
            Assert.Equal("Liste einblenden", WaitForAutomationId("Search.Panel.Toggle").Name);
            Assert.Equal(1, Server.ListRequests);
        });
    }

    /// <summary>
    /// Prüft, dass das Verschieben nach Osten die westlichen Markierungen aus dem Ausschnitt schiebt, die Gegenrichtung sie zurückholt und das Zurücksetzen alle zeigt.
    /// </summary>
    [Fact]
    public void Pan_MovesMarkersOutOfAndBackIntoView()
    {
        RunWithDiagnostics(() =>
        {
            SearchAndShowMap();

            for (var step = 0; step < 8; step++)
            {
                PressMapButton("Map.Pan.East");
            }

            WaitForMapCount("0 von 2 Stationen sichtbar");
            Assert.Empty(FindMarkers());

            for (var step = 0; step < 8; step++)
            {
                PressMapButton("Map.Pan.West");
            }

            WaitForMapCount("2 von 2 Stationen sichtbar");

            for (var step = 0; step < 8; step++)
            {
                PressMapButton("Map.Pan.North");
            }

            WaitForMapCount("0 von 2 Stationen sichtbar");
            PressMapButton("Map.Recenter");
            WaitForMapCount("2 von 2 Stationen sichtbar");
        });
    }
}

using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Map;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Mausbedienung der Karte unter Windows: Ziehen mit der Maus verschiebt die Karte, das Mausrad zoomt; außerdem die Adresse der Quellenangabe.
/// </summary>
public class MapPointerTrackerTests_Mouse : BaseTest
{
    /// <summary>
    /// Prüft, dass Ziehen die Verschiebung seit der letzten Meldung liefert und ohne gedrückte Taste nichts verschoben wird.
    /// </summary>
    [Fact]
    public void Drag_ReportsIncrementalMovementOnlyWhilePressed()
    {
        var tracker = new MapPointerTracker();

        Assert.Null(tracker.Move(10, 10));

        tracker.Press(100, 200);
        Assert.True(tracker.IsDragging);
        Assert.Equal(new(15, -5), tracker.Move(115, 195));
        Assert.Equal(new(-5, 10), tracker.Move(110, 205));

        tracker.Release();
        Assert.False(tracker.IsDragging);
        Assert.Null(tracker.Move(300, 300));
    }

    /// <summary>
    /// Prüft, dass ein neues Ziehen von der neuen Position ausgeht (kein Sprung).
    /// </summary>
    [Fact]
    public void Drag_Restart_UsesNewStartPosition()
    {
        var tracker = new MapPointerTracker();
        tracker.Press(0, 0);
        tracker.Move(50, 50);
        tracker.Release();

        tracker.Press(500, 500);

        Assert.Equal(new(3, 4), tracker.Move(503, 504));
    }

    /// <summary>
    /// Prüft, dass jede volle Mausradraste eine Zoomstufe ergibt (nach vorn vergrößert, nach hinten verkleinert).
    /// </summary>
    [Fact]
    public void Wheel_FullNotches_ZoomByWholeLevels()
    {
        var tracker = new MapPointerTracker();

        Assert.Equal(1, tracker.Wheel(MapPointerTracker.WheelNotch));
        Assert.Equal(-1, tracker.Wheel(-MapPointerTracker.WheelNotch));
        Assert.Equal(2, tracker.Wheel(2 * MapPointerTracker.WheelNotch));
    }

    /// <summary>
    /// Prüft, dass Teilrasten (Touchpad, feinauflösende Maus) summiert werden und erst die volle Raste zoomt.
    /// </summary>
    [Fact]
    public void Wheel_PartialNotches_AccumulateUntilFullNotch()
    {
        var tracker = new MapPointerTracker();

        Assert.Equal(0, tracker.Wheel(40));
        Assert.Equal(0, tracker.Wheel(40));
        Assert.Equal(1, tracker.Wheel(40));
        Assert.Equal(0, tracker.Wheel(-60));
        Assert.Equal(-1, tracker.Wheel(-60));
    }

    /// <summary>
    /// Prüft, dass die Quellenangabe der Karte auf die Urheber- und Lizenzhinweise von OpenStreetMap verweist.
    /// </summary>
    [Fact]
    public void Attribution_LinksToOpenStreetMapCopyrightPage()
    {
        Assert.Equal("© OpenStreetMap-Mitwirkende", MapTexts.Attribution);
        Assert.Equal("https://www.openstreetmap.org/copyright", MapTexts.AttributionUrl);
        Assert.True(Uri.TryCreate(MapTexts.AttributionUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps);
    }
}

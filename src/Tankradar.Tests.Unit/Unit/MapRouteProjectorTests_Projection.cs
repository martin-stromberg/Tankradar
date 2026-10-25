using Tankradar.MAUI.Models.Map;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Map;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Projektion der Routenlinie auf Bildschirmkoordinaten: Zuschnitt auf den Ausschnitt, Unterbrechung außerhalb, Vereinfachung je Zoomstufe.
/// </summary>
public class MapRouteProjectorTests_Projection : BaseTest
{
    private const double Width = 800;
    private const double Height = 600;

    private static MapViewport ViewportAt(double latitude, double longitude, int zoom)
    {
        return MapViewport.Create(latitude, longitude, zoom, Width, Height);
    }

    /// <summary>
    /// Prüft, dass eine Linie vollständig im Ausschnitt als eine Teillinie ohne Zuschnitt erscheint.
    /// </summary>
    [Fact]
    public void Project_LineInsideViewport_ReturnsSingleLineThroughCenter()
    {
        var points = RouteTestData.Straight(52.515, 13.405, 52.525, 13.405, 10);

        var lines = MapRouteProjector.Project(ViewportAt(52.52, 13.405, 14), points);

        var line = Assert.Single(lines);
        Assert.True(line.Count >= 2);
        Assert.All(line, point =>
        {
            Assert.InRange(point.X, 0, Width);
            Assert.InRange(point.Y, 0, Height);
            Assert.Equal(Width / 2, point.X, 3);
        });
    }

    /// <summary>
    /// Prüft den Zuschnitt: Die Linie reicht aus dem Ausschnitt, die Punkte bleiben im Ausschnitt samt Rand.
    /// </summary>
    [Fact]
    public void Project_LineLeavingViewport_IsClippedToViewportWithMargin()
    {
        var points = RouteTestData.Straight(52.40, 13.405, 52.70, 13.405, 60);

        var lines = MapRouteProjector.Project(ViewportAt(52.52, 13.405, 12), points, margin: 10);

        var line = Assert.Single(lines);
        Assert.All(line, point =>
        {
            Assert.InRange(point.X, -10.0001, Width + 10.0001);
            Assert.InRange(point.Y, -10.0001, Height + 10.0001);
        });
        Assert.Equal(-10, line.Min(point => point.Y), 3);
        Assert.Equal(Height + 10, line.Max(point => point.Y), 3);
    }

    /// <summary>
    /// Prüft, dass eine Linie, die den Ausschnitt verlässt und wieder betritt, in zwei Teillinien zerfällt.
    /// </summary>
    [Fact]
    public void Project_LineLeavingAndReentering_ReturnsTwoLines()
    {
        var points = new List<GeoPosition>
        {
            new(52.52, 13.405),
            new(52.80, 13.405),
            new(52.80, 13.43),
            new(52.52, 13.43),
        };

        var lines = MapRouteProjector.Project(ViewportAt(52.52, 13.41, 12), points);

        Assert.Equal(2, lines.Count);
    }

    /// <summary>
    /// Prüft, dass eine Linie weit außerhalb nichts liefert.
    /// </summary>
    [Fact]
    public void Project_LineOutsideViewport_ReturnsNoLines()
    {
        var points = RouteTestData.Straight(48.0, 9.0, 48.1, 9.0, 5);

        Assert.Empty(MapRouteProjector.Project(ViewportAt(52.52, 13.405, 12), points));
    }

    /// <summary>
    /// Prüft die Vereinfachung je Zoomstufe: Bei kleiner Zoomstufe fallen die Punkte zusammen, bei großer bleiben sie erhalten; das Ende bleibt erhalten.
    /// </summary>
    [Fact]
    public void Project_ZoomedOut_MergesNearbyPointsAndKeepsLastPoint()
    {
        var points = RouteTestData.Straight(52.52, 13.405, 52.53, 13.405, 500);

        var zoomedOut = Assert.Single(MapRouteProjector.Project(ViewportAt(52.525, 13.405, 6), points));
        var zoomedIn = Assert.Single(MapRouteProjector.Project(ViewportAt(52.525, 13.405, 15), points));

        Assert.True(zoomedOut.Count <= 3);
        Assert.True(zoomedIn.Count > 100);
        var last = ViewportAt(52.525, 13.405, 6).ToScreen(52.53, 13.405);
        Assert.Equal(last.Y, zoomedOut[^1].Y, 3);
    }

    /// <summary>
    /// Prüft, dass weniger als zwei Punkte keine Linie ergeben.
    /// </summary>
    [Fact]
    public void Project_TooFewPoints_ReturnsNoLines()
    {
        Assert.Empty(MapRouteProjector.Project(ViewportAt(52.52, 13.405, 12), []));
        Assert.Empty(MapRouteProjector.Project(ViewportAt(52.52, 13.405, 12), [new GeoPosition(52.52, 13.405)]));
    }

    /// <summary>
    /// Prüft die Kartenroute: Start, Ziel und Begrenzungsbox, und dass weniger als zwei Punkte abgelehnt werden.
    /// </summary>
    [Fact]
    public void MapRoute_ExposesStartDestinationAndBounds()
    {
        var route = new MapRoute([new GeoPosition(52.0, 13.5), new GeoPosition(53.0, 12.5), new GeoPosition(52.5, 14.0)]);

        Assert.Equal(52.0, route.Start.Latitude);
        Assert.Equal(52.5, route.Destination.Latitude);
        Assert.Equal(52.0, route.Bounds.MinLatitude);
        Assert.Equal(53.0, route.Bounds.MaxLatitude);
        Assert.Equal(12.5, route.Bounds.MinLongitude);
        Assert.Equal(14.0, route.Bounds.MaxLongitude);
        Assert.Throws<ArgumentException>(() => new MapRoute([new GeoPosition(52.0, 13.5)]));
        Assert.DoesNotContain("52", route.ToString(), StringComparison.Ordinal);
    }
}

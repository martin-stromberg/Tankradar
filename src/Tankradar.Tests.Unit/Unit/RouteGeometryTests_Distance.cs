using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Routing;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Großkreisabstand, Abstand eines Punktes zu einer Teilstrecke und die Vereinfachung von Linien.
/// </summary>
public class RouteGeometryTests_Distance : BaseTest
{
    /// <summary>
    /// Prüft den Abstand bekannter Orte (Berlin - Hamburg rund 255 km).
    /// </summary>
    [Fact]
    public void HaversineKm_BerlinHamburg_IsAboutKnownDistance()
    {
        var distance = RouteGeometry.HaversineKm(52.52, 13.405, 53.5511, 9.9937);

        Assert.InRange(distance, 253, 258);
    }

    /// <summary>
    /// Prüft, dass ein Grad Breite rund 111,2 km entspricht und gleiche Punkte den Abstand 0 haben.
    /// </summary>
    [Fact]
    public void HaversineKm_OneDegreeLatitude_IsAbout111Km()
    {
        Assert.Equal(111.19, RouteGeometry.HaversineKm(50, 10, 51, 10), 1);
        Assert.Equal(0, RouteGeometry.HaversineKm(50, 10, 50, 10), 9);
    }

    /// <summary>
    /// Prüft den Abstand eines seitlich versetzten Punktes zur Mitte einer Strecke bei 52 Grad Breite.
    /// </summary>
    [Fact]
    public void DistanceToSegmentKm_PointBesideSegment_ReturnsPerpendicularDistance()
    {
        // 0,01 Grad Länge bei 52,5 Grad Breite entsprechen rund 0,677 km.
        var distance = RouteGeometry.DistanceToSegmentKm(52.55, 13.415, 52.52, 13.405, 52.58, 13.405, out var fraction);

        Assert.Equal(0.677, distance, 2);
        Assert.Equal(0.5, fraction, 2);
    }

    /// <summary>
    /// Prüft die Klemmung des Fußpunkts am Streckenanfang und -ende.
    /// </summary>
    [Fact]
    public void DistanceToSegmentKm_BeyondEnds_ClampsFraction()
    {
        var beforeStart = RouteGeometry.DistanceToSegmentKm(52.50, 13.405, 52.52, 13.405, 52.58, 13.405, out var startFraction);
        var afterEnd = RouteGeometry.DistanceToSegmentKm(52.60, 13.405, 52.52, 13.405, 52.58, 13.405, out var endFraction);

        Assert.Equal(0, startFraction);
        Assert.Equal(1, endFraction);
        Assert.Equal(2.22, beforeStart, 1);
        Assert.Equal(2.22, afterEnd, 1);
    }

    /// <summary>
    /// Prüft die entartete Strecke (Anfang gleich Ende): der Abstand ist der zum Punkt.
    /// </summary>
    [Fact]
    public void DistanceToSegmentKm_DegenerateSegment_ReturnsDistanceToPoint()
    {
        var distance = RouteGeometry.DistanceToSegmentKm(52.53, 13.405, 52.52, 13.405, 52.52, 13.405, out var fraction);

        Assert.Equal(0, fraction);
        Assert.Equal(RouteGeometry.HaversineKm(52.53, 13.405, 52.52, 13.405), distance, 6);
    }

    /// <summary>
    /// Prüft, dass die Vereinfachung eine gerade Linie auf Anfang und Ende reduziert.
    /// </summary>
    [Fact]
    public void Simplify_StraightLine_KeepsOnlyEndpoints()
    {
        var points = new List<GeoPosition>();
        for (var i = 0; i <= 50; i++)
        {
            points.Add(new GeoPosition(52.0 + (i * 0.01), 13.0));
        }

        var result = RouteGeometry.Simplify(points, 0.01);

        Assert.Equal(2, result.Count);
        Assert.Equal(points[0], result[0]);
        Assert.Equal(points[^1], result[^1]);
    }

    /// <summary>
    /// Prüft, dass ein Knick oberhalb der Toleranz erhalten bleibt.
    /// </summary>
    [Fact]
    public void Simplify_CornerAboveTolerance_IsKept()
    {
        var points = new List<GeoPosition> { new(52.0, 13.0), new(52.0, 13.1), new(52.1, 13.1) };

        Assert.Equal(3, RouteGeometry.Simplify(points, 1.0).Count);
        Assert.Equal(2, RouteGeometry.Simplify(points, 50.0).Count);
    }
}

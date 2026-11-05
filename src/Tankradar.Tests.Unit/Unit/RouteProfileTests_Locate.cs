using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Routing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Streckenposition, Abstand, Punkt auf der Strecke, Begrenzungsbox und Vereinfachung eines <see cref="RouteProfile"/>.
/// </summary>
public class RouteProfileTests_Locate : BaseTest
{
    /// <summary>
    /// Prüft, dass weniger als zwei Punkte abgelehnt werden.
    /// </summary>
    [Fact]
    public void Constructor_SinglePoint_Throws()
    {
        Assert.Throws<ArgumentException>(() => new RouteProfile([new GeoPosition(52, 13)]));
    }

    /// <summary>
    /// Prüft die Länge einer geraden Nordroute (0,5 Grad Breite).
    /// </summary>
    [Fact]
    public void LengthKm_NorthRoute_MatchesGreatCircle()
    {
        var route = RouteTestData.NorthFromBerlin(0.5);

        Assert.Equal(55.6, route.LengthKm, 1);
    }

    /// <summary>
    /// Prüft Streckenposition und Abstand eines Punktes neben der Route.
    /// </summary>
    [Fact]
    public void Locate_PointBesideRoute_ReturnsAlongAndOffset()
    {
        var route = RouteTestData.NorthFromBerlin(0.5);

        var location = route.Locate(52.52 + 0.25, 13.405 + 0.01);

        Assert.Equal(27.8, location.AlongKm, 1);
        Assert.Equal(0.673, location.OffsetKm, 2);
    }

    /// <summary>
    /// Prüft die Enden: Punkte vor dem Start und hinter dem Ziel liegen bei 0 bzw. der Länge.
    /// </summary>
    [Fact]
    public void Locate_BeyondEnds_ClampsAlongToRouteLength()
    {
        var route = RouteTestData.NorthFromBerlin(0.5);

        var before = route.Locate(52.50, 13.405);
        var after = route.Locate(53.1, 13.405);

        Assert.Equal(0, before.AlongKm, 6);
        Assert.Equal(route.LengthKm, after.AlongKm, 6);
        Assert.True(before.OffsetKm > 2);
    }

    /// <summary>
    /// Prüft die Schleife: Bei gleichem Abstand gilt die kleinere Streckenposition.
    /// </summary>
    [Fact]
    public void Locate_PointOnLoopCrossing_ReturnsSmallerAlong()
    {
        // Ein Kreuzungspunkt wird zweimal durchfahren: erst Nord, dann West, dann Süd durch denselben Punkt.
        var route = new RouteProfile(
        [
            new GeoPosition(52.0, 13.0),
            new GeoPosition(52.1, 13.0),
            new GeoPosition(52.1, 12.9),
            new GeoPosition(52.05, 12.9),
            new GeoPosition(52.05, 13.1),
        ]);

        var location = route.Locate(52.05, 13.0);

        Assert.Equal(0, location.OffsetKm, 3);
        Assert.Equal(RouteGeometry.HaversineKm(52.0, 13.0, 52.05, 13.0), location.AlongKm, 2);
    }

    /// <summary>
    /// Prüft, dass mit Grenzwert ein weit entfernter Punkt unendlichen Abstand erhält, ein naher aber exakt bestimmt wird.
    /// </summary>
    [Fact]
    public void Locate_WithLimit_SkipsFarSegmentsAndKeepsNearOnes()
    {
        var route = RouteTestData.NorthFromBerlin(0.5);

        Assert.True(double.IsPositiveInfinity(route.Locate(52.77, 14.5, 2).OffsetKm));
        var near = route.Locate(52.77, 13.415, 2);
        Assert.Equal(route.Locate(52.77, 13.415).OffsetKm, near.OffsetKm, 6);
        Assert.Equal(route.Locate(52.77, 13.415).AlongKm, near.AlongKm, 6);
    }

    /// <summary>
    /// Prüft den Punkt auf der Strecke an Anfang, Mitte, Ende und außerhalb.
    /// </summary>
    [Fact]
    public void PointAt_ReturnsInterpolatedPointsAndClamps()
    {
        var route = RouteTestData.NorthFromBerlin(0.5);

        Assert.Equal(52.52, route.PointAt(0).Latitude, 6);
        Assert.Equal(52.77, route.PointAt(route.LengthKm / 2).Latitude, 4);
        Assert.Equal(53.02, route.PointAt(route.LengthKm).Latitude, 6);
        Assert.Equal(52.52, route.PointAt(-5).Latitude, 6);
        Assert.Equal(53.02, route.PointAt(10_000).Latitude, 6);
        Assert.Equal(52.52, route.PointAt(double.NaN).Latitude, 6);
    }

    /// <summary>
    /// Prüft die Begrenzungsbox.
    /// </summary>
    [Fact]
    public void Bounds_ContainsAllPoints()
    {
        var route = new RouteProfile([new GeoPosition(52.0, 13.5), new GeoPosition(53.0, 12.5), new GeoPosition(52.5, 14.0)]);

        var bounds = route.Bounds;

        Assert.Equal(52.0, bounds.MinLatitude);
        Assert.Equal(12.5, bounds.MinLongitude);
        Assert.Equal(53.0, bounds.MaxLatitude);
        Assert.Equal(14.0, bounds.MaxLongitude);
    }

    /// <summary>
    /// Prüft, dass die Vereinfachung die Form hält und die Punktzahl begrenzt.
    /// </summary>
    [Fact]
    public void Simplify_CurvedRoute_RespectsMaxPointsAndKeepsEnds()
    {
        var route = RouteTestData.Curved(1.0, 0.05, 400);

        var simplified = route.Simplify(0.001, 60);

        Assert.True(simplified.Count <= 60);
        Assert.True(simplified.Count >= 10);
        Assert.Equal(route.Points[0], simplified[0]);
        Assert.Equal(route.Points[^1], simplified[^1]);
        foreach (var point in route.Points)
        {
            Assert.True(new RouteProfile(simplified).Locate(point.Latitude, point.Longitude).OffsetKm < 8);
        }
    }
}

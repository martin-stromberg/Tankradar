using Tankradar.MAUI.Services.Map;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Kartenmathematik der Gesten des <see cref="MapViewport"/>: Umrechnung des Kneiffaktors in Zoomstufen, Zoom um einen Fixpunkt und Zentrieren beim Doppeltipp.
/// </summary>
public class MapViewportTests_Gestures : BaseTest
{
    private const double Latitude = 52.52;
    private const double Longitude = 13.405;
    private const double Width = 800;
    private const double Height = 600;

    /// <summary>
    /// Prüft, dass der Faktor auf die nächste ganze Zoomstufe gerundet wird (Zweierlogarithmus).
    /// </summary>
    /// <param name="factor">Der Gesamtfaktor.</param>
    /// <param name="expected">Die erwartete Änderung der Zoomstufe.</param>
    [Theory]
    [InlineData(1.0, 0)]
    [InlineData(1.4, 0)]
    [InlineData(1.5, 1)]
    [InlineData(2.0, 1)]
    [InlineData(4.0, 2)]
    [InlineData(0.5, -1)]
    [InlineData(0.7, -1)]
    public void ZoomStepsFor_RoundsFactorToWholeLevels(double factor, int expected)
    {
        Assert.Equal(expected, MapViewport.ZoomStepsFor(factor));
    }

    /// <summary>
    /// Prüft, dass ungültige Faktoren keine Änderung der Zoomstufe ergeben.
    /// </summary>
    /// <param name="factor">Der ungültige Faktor.</param>
    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(-2.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ZoomStepsFor_InvalidFactor_IsNoChange(double factor)
    {
        Assert.Equal(0, MapViewport.ZoomStepsFor(factor));
    }

    /// <summary>
    /// Prüft, dass beim Vergrößern um eine ganze Stufe der Ort unter dem Gestenmittelpunkt an derselben Bildschirmposition bleibt.
    /// </summary>
    [Fact]
    public void ScaleAtPoint_IntegerFactor_KeepsGeoPointUnderOriginFixed()
    {
        var viewport = MapViewport.Create(Latitude, Longitude, 12, Width, Height);

        var scaled = viewport.ScaleAtPoint(2, 200, 450);

        Assert.Equal(13, scaled.Zoom);
        AssertFixedPoint(viewport, scaled, 200, 450);
    }

    /// <summary>
    /// Prüft, dass ein nicht ganzzahliger Faktor auf die nächste Stufe einrastet und der Fixpunkt erhalten bleibt.
    /// </summary>
    [Fact]
    public void ScaleAtPoint_NonIntegerFactor_SnapsAndKeepsOrigin()
    {
        var viewport = MapViewport.Create(Latitude, Longitude, 12, Width, Height);

        var scaled = viewport.ScaleAtPoint(1.6, 620, 120);

        Assert.Equal(13, scaled.Zoom);
        AssertFixedPoint(viewport, scaled, 620, 120);
    }

    /// <summary>
    /// Prüft, dass ein Faktor von 0,5 um eine Stufe verkleinert und der Fixpunkt erhalten bleibt.
    /// </summary>
    [Fact]
    public void ScaleAtPoint_ZoomOut_KeepsOrigin()
    {
        var viewport = MapViewport.Create(Latitude, Longitude, 12, Width, Height);

        var scaled = viewport.ScaleAtPoint(0.5, 100, 500);

        Assert.Equal(11, scaled.Zoom);
        AssertFixedPoint(viewport, scaled, 100, 500);
    }

    /// <summary>
    /// Prüft, dass die Zoomstufe an der größten Stufe gehalten wird.
    /// </summary>
    [Fact]
    public void ScaleAtPoint_AtMaxZoom_StaysAtMax()
    {
        var viewport = MapViewport.Create(Latitude, Longitude, MapViewport.MaxZoom, Width, Height);

        Assert.Equal(MapViewport.MaxZoom, viewport.ScaleAtPoint(4, 100, 100).Zoom);
    }

    /// <summary>
    /// Prüft, dass die Zoomstufe an der kleinsten Stufe gehalten wird.
    /// </summary>
    [Fact]
    public void ScaleAtPoint_AtMinZoom_StaysAtMin()
    {
        var viewport = MapViewport.Create(Latitude, Longitude, MapViewport.MinZoom, Width, Height);

        Assert.Equal(MapViewport.MinZoom, viewport.ScaleAtPoint(0.25, 100, 100).Zoom);
    }

    /// <summary>
    /// Prüft, dass ein Faktor unterhalb der Rundungsschwelle denselben Ausschnitt liefert.
    /// </summary>
    /// <param name="factor">Der Gesamtfaktor.</param>
    [Theory]
    [InlineData(1.0)]
    [InlineData(1.3)]
    public void ScaleAtPoint_FactorBelowRoundingThreshold_ReturnsSameInstance(double factor)
    {
        var viewport = MapViewport.Create(Latitude, Longitude, 12, Width, Height);

        Assert.Same(viewport, viewport.ScaleAtPoint(factor, 100, 100));
    }

    /// <summary>
    /// Prüft, dass an der erreichten Grenze derselbe Ausschnitt geliefert wird.
    /// </summary>
    [Fact]
    public void ScaleAtPoint_AtLimit_ReturnsSameInstance()
    {
        var viewport = MapViewport.Create(Latitude, Longitude, MapViewport.MaxZoom, Width, Height);

        Assert.Same(viewport, viewport.ScaleAtPoint(2, 100, 100));
    }

    /// <summary>
    /// Prüft, dass ein Faktor über die größte Stufe hinaus auf diese begrenzt wird und der Fixpunkt erhalten bleibt.
    /// </summary>
    [Fact]
    public void ScaleAtPoint_FactorBeyondMaxZoom_ClampsAndKeepsOrigin()
    {
        var viewport = MapViewport.Create(Latitude, Longitude, MapViewport.MaxZoom - 1, Width, Height);

        var scaled = viewport.ScaleAtPoint(8, 200, 450);

        Assert.Equal(MapViewport.MaxZoom, scaled.Zoom);
        AssertFixedPoint(viewport, scaled, 200, 450);
    }

    /// <summary>
    /// Prüft, dass ein Faktor unter die kleinste Stufe hinaus auf diese begrenzt wird und der Fixpunkt erhalten bleibt.
    /// </summary>
    [Fact]
    public void ScaleAtPoint_FactorBeyondMinZoom_ClampsAndKeepsOrigin()
    {
        var viewport = MapViewport.Create(Latitude, Longitude, MapViewport.MinZoom + 1, Width, Height);

        var scaled = viewport.ScaleAtPoint(0.125, 200, 450);

        Assert.Equal(MapViewport.MinZoom, scaled.Zoom);
        AssertFixedPoint(viewport, scaled, 200, 450);
    }

    /// <summary>
    /// Prüft, dass der Fixpunkt auch nahe der Datumsgrenze erhalten bleibt und die Mittellänge im gültigen Bereich liegt.
    /// </summary>
    [Fact]
    public void ScaleAtPoint_NearAntimeridian_WrapsCenterLongitude()
    {
        var viewport = MapViewport.Create(0, 179.99, 6, Width, Height);

        var scaled = viewport.ScaleAtPoint(2, Width, Height / 2);

        Assert.Equal(7, scaled.Zoom);
        Assert.InRange(scaled.CenterLongitude, -180, 180);
        AssertFixedPoint(viewport, scaled, Width, Height / 2);
    }

    /// <summary>
    /// Prüft, dass der Doppeltipp auf den angetippten Ort zentriert und eine Stufe vergrößert.
    /// </summary>
    [Fact]
    public void ZoomAndCenterAt_CentersOnTapAndZoomsOneLevel()
    {
        var viewport = MapViewport.Create(Latitude, Longitude, 12, Width, Height);
        var tapped = viewport.ToLatLon(250, 400);

        var result = viewport.ZoomAndCenterAt(1, 250, 400);

        Assert.Equal(13, result.Zoom);
        Assert.Equal(tapped.Latitude, result.CenterLatitude, 6);
        Assert.Equal(tapped.Longitude, result.CenterLongitude, 6);
    }

    /// <summary>
    /// Prüft, dass an der größten Zoomstufe nur zentriert wird.
    /// </summary>
    [Fact]
    public void ZoomAndCenterAt_AtMaxZoom_OnlyCenters()
    {
        var viewport = MapViewport.Create(Latitude, Longitude, MapViewport.MaxZoom, Width, Height);
        var tapped = viewport.ToLatLon(700, 50);

        var result = viewport.ZoomAndCenterAt(1, 700, 50);

        Assert.NotSame(viewport, result);
        Assert.Equal(MapViewport.MaxZoom, result.Zoom);
        Assert.Equal(tapped.Latitude, result.CenterLatitude, 6);
        Assert.Equal(tapped.Longitude, result.CenterLongitude, 6);
    }

    /// <summary>
    /// Prüft, dass ein Doppeltipp in einer Ecke einer weit herausgezoomten Karte eine Mittelbreite innerhalb der Grenzen der Projektion ergibt.
    /// </summary>
    /// <param name="x">Die horizontale Lage der Ecke in Pixeln.</param>
    /// <param name="y">Die vertikale Lage der Ecke in Pixeln.</param>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(Width, Height * 10)]
    public void ZoomAndCenterAt_AtCorner_KeepsLatitudeInProjection(double x, double y)
    {
        var viewport = MapViewport.Create(80, 0, MapViewport.MinZoom, Width, Height * 10);

        var result = viewport.ZoomAndCenterAt(1, x, y);

        Assert.InRange(result.CenterLatitude, -MapViewport.MaxLatitude - 1e-9, MapViewport.MaxLatitude + 1e-9);
    }

    /// <summary>
    /// Prüft, dass ein Doppeltipp in einer Ecke einer weit herausgezoomten Karte eine Mittellänge im gültigen Bereich ergibt.
    /// </summary>
    /// <param name="x">Die horizontale Lage der Ecke in Pixeln.</param>
    /// <param name="y">Die vertikale Lage der Ecke in Pixeln.</param>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(Width, Height * 10)]
    public void ZoomAndCenterAt_AtCorner_KeepsLongitudeInRange(double x, double y)
    {
        var viewport = MapViewport.Create(80, 0, MapViewport.MinZoom, Width, Height * 10);

        var result = viewport.ZoomAndCenterAt(1, x, y);

        Assert.InRange(result.CenterLongitude, -180, 180);
    }

    private static void AssertFixedPoint(MapViewport before, MapViewport after, double x, double y)
    {
        var position = before.ToLatLon(x, y);
        var (screenX, screenY) = after.ToScreen(position.Latitude, position.Longitude);
        Assert.InRange(screenX, x - 0.5, x + 0.5);
        Assert.InRange(screenY, y - 0.5, y + 0.5);
    }
}

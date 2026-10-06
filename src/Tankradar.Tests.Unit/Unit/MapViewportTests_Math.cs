using Tankradar.MAUI.Services.Map;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Kartenmathematik des <see cref="MapViewport"/>: Projektion, Einpassen, Verschieben, Zoomen und sichtbare Kacheln.
/// </summary>
public class MapViewportTests_Math : BaseTest
{
    private const double Latitude = 52.52;
    private const double Longitude = 13.405;

    /// <summary>
    /// Prüft, dass die Mitte in der Bildmitte liegt und Mitte und Zoomstufe der Erstellung entsprechen.
    /// </summary>
    [Fact]
    public void Create_PlacesCenterInMiddleOfView()
    {
        var viewport = MapViewport.Create(Latitude, Longitude, 12, 800, 600);

        var (x, y) = viewport.ToScreen(Latitude, Longitude);

        Assert.Equal(400, x, 3);
        Assert.Equal(300, y, 3);
        Assert.Equal(Latitude, viewport.CenterLatitude, 6);
        Assert.Equal(Longitude, viewport.CenterLongitude, 6);
        Assert.Equal(12, viewport.Zoom);
    }

    /// <summary>
    /// Prüft die Projektion gegen die bekannte Kachelnummer von Berlin-Mitte bei Zoomstufe 10 (Spalte 550, Zeile 335).
    /// </summary>
    [Fact]
    public void VisibleTiles_AtBerlin_ContainsKnownTile()
    {
        var viewport = MapViewport.Create(Latitude, Longitude, 10, 256, 256);

        var tiles = viewport.VisibleTiles();

        Assert.Contains(tiles, tile => tile.Key.Zoom == 10 && tile.Key.X == 550 && tile.Key.Y == 335);
    }

    /// <summary>
    /// Prüft, dass nach Norden gelegene Orte weiter oben und nach Osten gelegene weiter rechts erscheinen.
    /// </summary>
    [Fact]
    public void ToScreen_NorthIsUpAndEastIsRight()
    {
        var viewport = MapViewport.Create(Latitude, Longitude, 12, 800, 600);

        var (northX, northY) = viewport.ToScreen(Latitude + 0.01, Longitude);
        var (eastX, eastY) = viewport.ToScreen(Latitude, Longitude + 0.01);

        Assert.True(northY < 300);
        Assert.Equal(400, northX, 3);
        Assert.True(eastX > 400);
        Assert.Equal(300, eastY, 3);
    }

    /// <summary>
    /// Prüft, dass das Verschieben die Karte mit der Geste bewegt: ein Ort wandert um die Verschiebung, die Mitte entgegengesetzt.
    /// </summary>
    [Fact]
    public void PanByPixels_MovesPointsByDelta()
    {
        var viewport = MapViewport.Create(Latitude, Longitude, 12, 800, 600);
        var (x0, y0) = viewport.ToScreen(Latitude, Longitude);

        var panned = viewport.PanByPixels(100, -50);
        var (x1, y1) = panned.ToScreen(Latitude, Longitude);

        Assert.Equal(x0 + 100, x1, 3);
        Assert.Equal(y0 - 50, y1, 3);
        Assert.True(panned.CenterLongitude < viewport.CenterLongitude);
    }

    /// <summary>
    /// Prüft, dass das Verschieben nicht über die Pole hinausführt und die Länge um die Datumsgrenze umbricht.
    /// </summary>
    [Fact]
    public void PanByPixels_ClampsAtPolesAndWrapsLongitude()
    {
        var viewport = MapViewport.Create(80, 179.9, 6, 800, 600);

        var north = viewport.PanByPixels(0, 1_000_000);
        var wrapped = viewport.PanByPixels(-1000, 0);

        Assert.InRange(north.CenterLatitude, 84.9, 85.06);
        Assert.InRange(wrapped.CenterLongitude, -180, 180);
        Assert.True(wrapped.CenterLongitude < 0);
    }

    /// <summary>
    /// Prüft, dass Zoomen die Mitte beibehält, die Stufe um eins ändert und an den Grenzen endet.
    /// </summary>
    [Fact]
    public void ZoomBy_KeepsCenterAndStopsAtLimits()
    {
        var viewport = MapViewport.Create(Latitude, Longitude, 12, 800, 600);

        var closer = viewport.ZoomBy(1);

        Assert.Equal(13, closer.Zoom);
        Assert.Equal(Latitude, closer.CenterLatitude, 5);
        Assert.Equal(Longitude, closer.CenterLongitude, 5);
        Assert.Equal(MapViewport.MaxZoom, viewport.ZoomBy(100).Zoom);
        Assert.Equal(MapViewport.MinZoom, viewport.ZoomBy(-100).Zoom);
        var atMax = viewport.ZoomBy(100);
        Assert.Same(atMax, atMax.ZoomBy(1));
    }

    /// <summary>
    /// Prüft, dass das Einpassen alle Punkte mit Rand in den Ausschnitt legt und eine höhere Stufe nicht mehr passen würde.
    /// </summary>
    [Fact]
    public void Fit_ContainsAllPointsWithPadding()
    {
        (double, double)[] points = [(52.52, 13.405), (52.53, 13.415), (52.592, 13.405)];

        var viewport = MapViewport.Fit(points, 800, 440, 64);

        Assert.All(points, point => Assert.True(viewport.Contains(point.Item1, point.Item2, -64)));
        var closer = MapViewport.Create(viewport.CenterLatitude, viewport.CenterLongitude, viewport.Zoom + 1, 800, 440);
        Assert.Contains(points, point => !closer.Contains(point.Item1, point.Item2, -64));
    }

    /// <summary>
    /// Prüft, dass ein einzelner Punkt mit der größten Einpass-Stufe zentriert wird und eine leere Liste Mitteleuropa zeigt.
    /// </summary>
    [Fact]
    public void Fit_SinglePointAndNoPoints()
    {
        var single = MapViewport.Fit([(Latitude, Longitude)], 800, 440, 64, 15);
        var none = MapViewport.Fit([], 800, 440);

        Assert.Equal(15, single.Zoom);
        Assert.Equal(Latitude, single.CenterLatitude, 5);
        Assert.Equal(6, none.Zoom);
        Assert.Throws<ArgumentNullException>(() => MapViewport.Fit(null!, 10, 10));
    }

    /// <summary>
    /// Prüft, dass weit auseinanderliegende Punkte auf der kleinsten Stufe enden.
    /// </summary>
    [Fact]
    public void Fit_FarApartPoints_EndAtMinZoom()
    {
        var viewport = MapViewport.Fit([(-60.0, -170.0), (70.0, 170.0)], 300, 300, 10);

        Assert.Equal(MapViewport.MinZoom, viewport.Zoom);
    }

    /// <summary>
    /// Prüft die sichtbaren Kacheln: lückenlos, 256 Pixel groß, die Bildmitte liegt in einer Kachel, Spalten brechen um und Zeilen außerhalb der Welt entfallen.
    /// </summary>
    [Fact]
    public void VisibleTiles_CoverViewWrapColumnsAndSkipRowsOutsideWorld()
    {
        var viewport = MapViewport.Create(Latitude, Longitude, 12, 800, 600);
        var tiles = viewport.VisibleTiles();

        Assert.Contains(tiles, tile => tile.Left <= 400 && 400 < tile.Left + 256 && tile.Top <= 300 && 300 < tile.Top + 256);
        Assert.All(tiles, tile => Assert.InRange(tile.Key.X, 0, (1 << 12) - 1));
        Assert.True(tiles.Min(tile => tile.Left) <= 0);
        Assert.True(tiles.Max(tile => tile.Left) + 256 >= 800);

        var edge = MapViewport.Create(0, 179.99, 4, 800, 600).VisibleTiles();
        Assert.Contains(edge, tile => tile.Key.X == 15);
        Assert.Contains(edge, tile => tile.Key.X == 0);

        var pole = MapViewport.Create(85, 0, 4, 800, 600).VisibleTiles();
        Assert.All(pole, tile => Assert.InRange(tile.Key.Y, 0, 15));
    }

    /// <summary>
    /// Prüft, dass eine Größenänderung Mitte und Zoomstufe beibehält und die Enthaltensein-Prüfung den Rand berücksichtigt.
    /// </summary>
    [Fact]
    public void Resize_KeepsCenter_ContainsHonorsMargin()
    {
        var viewport = MapViewport.Create(Latitude, Longitude, 12, 800, 600);

        var resized = viewport.Resize(400, 300);

        Assert.Equal(Latitude, resized.CenterLatitude, 5);
        Assert.Equal(12, resized.Zoom);
        Assert.Equal(400, resized.Width);
        var (x, _) = viewport.ToScreen(Latitude, Longitude + 0.2);
        Assert.False(viewport.Contains(Latitude, Longitude + 0.2));
        Assert.True(viewport.Contains(Latitude, Longitude + 0.2, x));
    }
}

using Tankradar.MAUI.Services.Map;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Begrenzung des sichtbaren Ausschnitts (<see cref="MapViewport.GetBounds"/>): Mitte und Ecken liegen darin, an den Polen bleibt der Breitengrad in den Grenzen der Projektion.
/// </summary>
public class MapViewportTests_Bounds : BaseTest
{
    private const double Latitude = 52.52;
    private const double Longitude = 13.405;

    /// <summary>
    /// Prüft, dass die Begrenzung die Mitte enthält, ihre Ränder genau an den Ecken des Ausschnitts liegen und Norden oben ist.
    /// </summary>
    [Fact]
    public void GetBounds_ContainsCenterAndCorners()
    {
        var viewport = MapViewport.Create(Latitude, Longitude, 13, 400, 700);

        var bounds = viewport.GetBounds();

        Assert.InRange(Latitude, bounds.MinLatitude, bounds.MaxLatitude);
        Assert.InRange(Longitude, bounds.MinLongitude, bounds.MaxLongitude);
        Assert.True(bounds.MaxLatitude > bounds.MinLatitude);
        Assert.True(bounds.MaxLongitude > bounds.MinLongitude);
        var (westX, northY) = viewport.ToScreen(bounds.MaxLatitude, bounds.MinLongitude);
        var (eastX, southY) = viewport.ToScreen(bounds.MinLatitude, bounds.MaxLongitude);
        Assert.Equal(0, westX, 3);
        Assert.Equal(0, northY, 3);
        Assert.Equal(400, eastX, 3);
        Assert.Equal(700, southY, 3);
    }

    /// <summary>
    /// Prüft, dass die Textdarstellung der Begrenzung keine Koordinaten enthält (Positionen werden nie protokolliert).
    /// </summary>
    [Fact]
    public void MapBounds_ToString_HidesCoordinates()
    {
        var bounds = MapViewport.Create(Latitude, Longitude, 13, 400, 700).GetBounds();

        Assert.Equal(nameof(MAUI.Models.Map.MapBounds), bounds.ToString());
    }

    /// <summary>
    /// Prüft, dass ein Ausschnitt am Pol mit kleiner Zoomstufe keinen Breitengrad außerhalb der Web-Mercator-Grenzen (±85,05°) liefert.
    /// </summary>
    [Fact]
    public void GetBounds_ClampsAtPoles()
    {
        var north = MapViewport.Create(85, 0, MapViewport.MinZoom, 800, 2000).GetBounds();
        var south = MapViewport.Create(-85, 0, MapViewport.MinZoom, 800, 2000).GetBounds();

        Assert.Equal(MapViewport.MaxLatitude, north.MaxLatitude, 6);
        Assert.InRange(north.MinLatitude, -MapViewport.MaxLatitude, MapViewport.MaxLatitude);
        Assert.Equal(-MapViewport.MaxLatitude, south.MinLatitude, 6);
        Assert.InRange(south.MaxLatitude, -MapViewport.MaxLatitude, MapViewport.MaxLatitude);
    }
}

using Tankradar.MAUI.Models.Map;
using Tankradar.MAUI.Services.Map;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Einpassen des Kartenausschnitts auf die Begrenzungsbox einer Route.
/// </summary>
public class MapViewportTests_RouteBounds : BaseTest
{
    private static MapViewport FitTo(MapRoute route, double width = 800, double height = 440)
    {
        var bounds = route.Bounds;
        return MapViewport.Fit(
            [(bounds.MinLatitude, bounds.MinLongitude), (bounds.MaxLatitude, bounds.MaxLongitude)],
            width,
            height,
            64);
    }

    /// <summary>
    /// Prüft, dass alle Punkte der Route nach dem Einpassen im Ausschnitt liegen.
    /// </summary>
    [Fact]
    public void Fit_ToRouteBounds_ContainsAllRoutePoints()
    {
        var route = new MapRoute(RouteTestData.Curved(1.5, 0.4, 100).Points);

        var viewport = FitTo(route);

        Assert.All(route.Points, point => Assert.True(viewport.Contains(point.Latitude, point.Longitude)));
    }

    /// <summary>
    /// Prüft, dass eine längere Route eine kleinere Zoomstufe ergibt als eine kurze.
    /// </summary>
    [Fact]
    public void Fit_LongerRoute_UsesSmallerZoom()
    {
        var shortRoute = new MapRoute(RouteTestData.NorthFromBerlin(0.1).Points);
        var longRoute = new MapRoute(RouteTestData.NorthFromBerlin(2.5).Points);

        Assert.True(FitTo(longRoute).Zoom < FitTo(shortRoute).Zoom);
    }

    /// <summary>
    /// Prüft, dass eine Route von rund 300 km in den Ausschnitt eines Smartphones passt.
    /// </summary>
    [Fact]
    public void Fit_ThreeHundredKilometers_FitsPhoneSizedViewport()
    {
        var route = new MapRoute(RouteTestData.NorthFromBerlin(2.7).Points);

        var viewport = FitTo(route, 390, 440);

        Assert.True(viewport.Zoom >= MapViewport.MinZoom);
        Assert.All(route.Points, point => Assert.True(viewport.Contains(point.Latitude, point.Longitude)));
    }
}

using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Map;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Routing;

namespace Tankradar.MAUI.Services.Map;

/// <summary>
/// Leitet aus dem sichtbaren Kartenausschnitt eine einzelne Umkreissuche ab: Mittelpunkt ist die Mitte des Ausschnitts, der Radius die halbe Diagonale
/// in ganzen Kilometern (aufgerundet, mindestens 1 km). Die Preisquelle kennt nur Kreise bis 25 km; größere Ausschnitte ergeben keine Abfrage.
/// </summary>
public static class MapAreaQuery
{
    /// <summary>
    /// Erzeugt die Umkreissuche für den Ausschnitt.
    /// </summary>
    /// <param name="bounds">Die Begrenzung des sichtbaren Ausschnitts.</param>
    /// <param name="fuelTypes">Die gewünschten Spritsorten.</param>
    /// <returns>Die Suche; <see langword="null"/>, wenn der Radius über <see cref="SearchRadius.Max"/> läge.</returns>
    public static StationSearchQuery? From(MapBounds bounds, IReadOnlyList<FuelType> fuelTypes)
    {
        ArgumentNullException.ThrowIfNull(fuelTypes);
        if (IsTooLarge(bounds))
        {
            return null;
        }

        var centerLatitude = (bounds.MinLatitude + bounds.MaxLatitude) / 2;
        var centerLongitude = (bounds.MinLongitude + bounds.MaxLongitude) / 2;
        return new StationSearchQuery(centerLatitude, centerLongitude, (int)RadiusKmOf(bounds), fuelTypes);
    }

    /// <summary>
    /// Gibt an, ob der Ausschnitt für eine Umkreissuche zu groß ist (Radius über <see cref="SearchRadius.Max"/>).
    /// </summary>
    /// <param name="bounds">Die Begrenzung des sichtbaren Ausschnitts.</param>
    /// <returns><see langword="true"/>, wenn für den Ausschnitt keine Abfrage möglich ist.</returns>
    public static bool IsTooLarge(MapBounds bounds)
    {
        return RadiusKmOf(bounds) > SearchRadius.Max;
    }

    private static double RadiusKmOf(MapBounds bounds)
    {
        var diagonalKm = RouteGeometry.HaversineKm(bounds.MinLatitude, bounds.MinLongitude, bounds.MaxLatitude, bounds.MaxLongitude);
        return Math.Max(SearchRadius.Min, Math.Ceiling(diagonalKm / 2));
    }
}

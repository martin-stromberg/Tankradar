using Tankradar.MAUI.Models.Routing;

namespace Tankradar.MAUI.Services.Routing;

/// <summary>
/// Bestimmt die Umkreisabfragen entlang einer Route so, dass das Abstandsband um die Route lückenlos überdeckt ist.
/// Die Route der Länge L wird in n gleich lange Abschnitte der Länge s geteilt; der Mittelpunkt einer Abfrage liegt auf der Streckenmitte des Abschnitts.
/// Eine Tankstelle im Abstand d von einem Routenpunkt des Abschnitts ist höchstens s/2 + d vom Mittelpunkt entfernt (Dreiecksungleichung);
/// der Radius s/2/Faktor + d, auf ganze Kilometer aufgerundet, überdeckt sie daher sicher.
/// </summary>
public static class RouteSampler
{
    /// <summary>
    /// Berechnet die Abfragepunkte.
    /// </summary>
    /// <param name="route">Die Route.</param>
    /// <param name="distanceKm">Der Abstand zur Route in Kilometern.</param>
    /// <param name="options">Die Einstellungen der Routensuche.</param>
    /// <param name="queryPoints">Die Abfragepunkte bei Erfolg, sonst eine leere Liste.</param>
    /// <returns><see langword="false"/>, wenn die Route zu lang ist oder mehr als <see cref="RouteSearchOptions.MaxQueries"/> Abfragen nötig wären.</returns>
    public static bool TryCreateQueryPoints(RouteProfile route, int distanceKm, RouteSearchOptions options, out IReadOnlyList<RouteQueryPoint> queryPoints)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(options);
        queryPoints = [];

        var length = route.LengthKm;
        if (length > options.MaxRouteLengthKm || distanceKm >= options.QueryRadiusKm)
        {
            return false;
        }

        var step = 2 * (options.QueryRadiusKm - distanceKm) * options.CoverageFactor;
        var count = Math.Max(1, (int)Math.Ceiling(length / step));
        if (count > options.MaxQueries)
        {
            return false;
        }

        var section = length / count;
        var points = new List<RouteQueryPoint>(count);
        for (var index = 0; index < count; index++)
        {
            var along = (index + 0.5) * section;
            var center = route.PointAt(along);
            var radius = Math.Ceiling((section / 2 / options.CoverageFactor) + distanceKm);
            var clamped = (int)Math.Clamp(radius, 1, Math.Floor(options.QueryRadiusKm));
            points.Add(new RouteQueryPoint(center.Latitude, center.Longitude, clamped, along));
        }

        queryPoints = points;
        return true;
    }
}

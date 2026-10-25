using Tankradar.MAUI.Models.Search;

namespace Tankradar.MAUI.Services.Routing;

/// <summary>
/// Geometrische Grundfunktionen für Routen: Großkreisabstand, Abstand eines Punktes zu einer Teilstrecke und Vereinfachung von Linien.
/// Die Berechnungen erfolgen ohne externe Bibliotheken.
/// </summary>
public static class RouteGeometry
{
    /// <summary>
    /// Der mittlere Erdradius in Kilometern.
    /// </summary>
    public const double EarthRadiusKm = 6371.0088;

    private const double KmPerDegree = EarthRadiusKm * Math.PI / 180.0;

    /// <summary>
    /// Berechnet den Großkreisabstand zweier Punkte (Haversine-Formel).
    /// </summary>
    /// <param name="latitude1">Breitengrad des ersten Punktes.</param>
    /// <param name="longitude1">Längengrad des ersten Punktes.</param>
    /// <param name="latitude2">Breitengrad des zweiten Punktes.</param>
    /// <param name="longitude2">Längengrad des zweiten Punktes.</param>
    /// <returns>Der Abstand in Kilometern.</returns>
    public static double HaversineKm(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        var phi1 = ToRadians(latitude1);
        var phi2 = ToRadians(latitude2);
        var deltaPhi = phi2 - phi1;
        var deltaLambda = ToRadians(longitude2 - longitude1);
        var a = Math.Pow(Math.Sin(deltaPhi / 2), 2) + (Math.Cos(phi1) * Math.Cos(phi2) * Math.Pow(Math.Sin(deltaLambda / 2), 2));
        return 2 * EarthRadiusKm * Math.Asin(Math.Min(1.0, Math.Sqrt(a)));
    }

    /// <summary>
    /// Berechnet den kürzesten Abstand eines Punktes zu einer Teilstrecke. Der Punkt wird dazu lokal in Kilometer projiziert
    /// (äquirektangulär um die mittlere Breite der Teilstrecke); der Abstand zum Fußpunkt wird als Großkreisabstand bestimmt.
    /// </summary>
    /// <param name="pointLatitude">Breitengrad des Punktes.</param>
    /// <param name="pointLongitude">Längengrad des Punktes.</param>
    /// <param name="startLatitude">Breitengrad des Streckenanfangs.</param>
    /// <param name="startLongitude">Längengrad des Streckenanfangs.</param>
    /// <param name="endLatitude">Breitengrad des Streckenendes.</param>
    /// <param name="endLongitude">Längengrad des Streckenendes.</param>
    /// <param name="fraction">Der Anteil (0 bis 1) der Teilstrecke bis zum Fußpunkt; bei entarteter Strecke 0.</param>
    /// <returns>Der Abstand in Kilometern.</returns>
    public static double DistanceToSegmentKm(
        double pointLatitude,
        double pointLongitude,
        double startLatitude,
        double startLongitude,
        double endLatitude,
        double endLongitude,
        out double fraction)
    {
        var cosine = Math.Cos(ToRadians((startLatitude + endLatitude) / 2));
        var segmentX = (endLongitude - startLongitude) * cosine * KmPerDegree;
        var segmentY = (endLatitude - startLatitude) * KmPerDegree;
        var pointX = (pointLongitude - startLongitude) * cosine * KmPerDegree;
        var pointY = (pointLatitude - startLatitude) * KmPerDegree;
        var lengthSquared = (segmentX * segmentX) + (segmentY * segmentY);

        fraction = lengthSquared < 1e-18 ? 0 : Math.Clamp(((pointX * segmentX) + (pointY * segmentY)) / lengthSquared, 0, 1);
        var footLatitude = startLatitude + (fraction * (endLatitude - startLatitude));
        var footLongitude = startLongitude + (fraction * (endLongitude - startLongitude));
        return HaversineKm(pointLatitude, pointLongitude, footLatitude, footLongitude);
    }

    /// <summary>
    /// Vereinfacht eine Punktfolge nach Douglas-Peucker. Anfangs- und Endpunkt bleiben erhalten.
    /// </summary>
    /// <param name="points">Die Punktfolge.</param>
    /// <param name="toleranceKm">Der größte erlaubte Abstand der entfernten Punkte zur vereinfachten Linie in Kilometern.</param>
    /// <returns>Die vereinfachte Punktfolge.</returns>
    public static IReadOnlyList<GeoPosition> Simplify(IReadOnlyList<GeoPosition> points, double toleranceKm)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count <= 2)
        {
            return points.ToList();
        }

        var keep = new bool[points.Count];
        keep[0] = true;
        keep[^1] = true;
        var stack = new Stack<(int First, int Last)>();
        stack.Push((0, points.Count - 1));
        while (stack.Count > 0)
        {
            var (first, last) = stack.Pop();
            var maxDistance = -1.0;
            var maxIndex = -1;
            var a = points[first];
            var b = points[last];
            for (var index = first + 1; index < last; index++)
            {
                var p = points[index];
                var distance = DistanceToSegmentKm(p.Latitude, p.Longitude, a.Latitude, a.Longitude, b.Latitude, b.Longitude, out _);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    maxIndex = index;
                }
            }

            if (maxIndex >= 0 && maxDistance > toleranceKm)
            {
                keep[maxIndex] = true;
                stack.Push((first, maxIndex));
                stack.Push((maxIndex, last));
            }
        }

        var result = new List<GeoPosition>();
        for (var index = 0; index < points.Count; index++)
        {
            if (keep[index])
            {
                result.Add(points[index]);
            }
        }

        return result;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}

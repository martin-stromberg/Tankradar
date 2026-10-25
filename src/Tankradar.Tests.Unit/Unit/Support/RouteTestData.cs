using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Models.Search;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Testrouten für die Geometrie- und Auswahltests der Routensuche.
/// </summary>
public static class RouteTestData
{
    /// <summary>
    /// Erzeugt eine gerade Linie mit gleichmäßig verteilten Punkten.
    /// </summary>
    /// <param name="fromLatitude">Breitengrad des Starts.</param>
    /// <param name="fromLongitude">Längengrad des Starts.</param>
    /// <param name="toLatitude">Breitengrad des Ziels.</param>
    /// <param name="toLongitude">Längengrad des Ziels.</param>
    /// <param name="segments">Anzahl der Teilstrecken.</param>
    /// <returns>Die Punkte.</returns>
    public static List<GeoPosition> Straight(double fromLatitude, double fromLongitude, double toLatitude, double toLongitude, int segments = 20)
    {
        var points = new List<GeoPosition>();
        for (var index = 0; index <= segments; index++)
        {
            var t = index / (double)segments;
            points.Add(new GeoPosition(fromLatitude + (t * (toLatitude - fromLatitude)), fromLongitude + (t * (toLongitude - fromLongitude))));
        }

        return points;
    }

    /// <summary>
    /// Erzeugt eine nach Norden verlaufende Linie mit seitlicher Sinuskurve (Kurven und wechselnde Richtung).
    /// </summary>
    /// <param name="lengthDegrees">Ausdehnung nach Norden in Grad Breite.</param>
    /// <param name="amplitudeDegrees">Seitlicher Ausschlag in Grad Länge.</param>
    /// <param name="segments">Anzahl der Teilstrecken.</param>
    /// <returns>Das Profil.</returns>
    public static RouteProfile Curved(double lengthDegrees, double amplitudeDegrees, int segments = 80)
    {
        var points = new List<GeoPosition>();
        for (var index = 0; index <= segments; index++)
        {
            var t = index / (double)segments;
            points.Add(new GeoPosition(50.0 + (t * lengthDegrees), 10.0 + (amplitudeDegrees * Math.Sin(t * 6 * Math.PI))));
        }

        return new RouteProfile(points);
    }

    /// <summary>
    /// Erzeugt das Profil einer geraden Nordroute von Berlin-Mitte.
    /// </summary>
    /// <param name="lengthDegrees">Ausdehnung nach Norden in Grad Breite.</param>
    /// <returns>Das Profil.</returns>
    public static RouteProfile NorthFromBerlin(double lengthDegrees)
    {
        return new RouteProfile(Straight(52.52, 13.405, 52.52 + lengthDegrees, 13.405, 40));
    }
}

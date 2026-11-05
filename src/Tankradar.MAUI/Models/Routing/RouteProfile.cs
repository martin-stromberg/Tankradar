using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Routing;

namespace Tankradar.MAUI.Models.Routing;

/// <summary>
/// Eine berechnete Route als Punktfolge mit kumulierter Strecke. Die Route lebt nur im Arbeitsspeicher und wird nie gespeichert oder protokolliert.
/// </summary>
public sealed class RouteProfile
{
    private const double TieEpsilonKm = 1e-9;

    private readonly GeoPosition[] _points;
    private readonly double[] _cumulativeKm;

    /// <summary>
    /// Erstellt das Profil.
    /// </summary>
    /// <param name="points">Die Punktfolge, mindestens zwei Punkte.</param>
    /// <exception cref="ArgumentException">Es sind weniger als zwei Punkte angegeben.</exception>
    public RouteProfile(IReadOnlyList<GeoPosition> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 2)
        {
            throw new ArgumentException("Eine Route benötigt mindestens zwei Punkte.", nameof(points));
        }

        _points = points.ToArray();
        _cumulativeKm = new double[_points.Length];
        for (var index = 1; index < _points.Length; index++)
        {
            _cumulativeKm[index] = _cumulativeKm[index - 1]
                + RouteGeometry.HaversineKm(_points[index - 1].Latitude, _points[index - 1].Longitude, _points[index].Latitude, _points[index].Longitude);
        }
    }

    /// <summary>
    /// Die Punkte der Route vom Start zum Ziel.
    /// </summary>
    public IReadOnlyList<GeoPosition> Points
    {
        get { return _points; }
    }

    /// <summary>
    /// Die Länge der Route in Kilometern (Summe der Teilstrecken).
    /// </summary>
    public double LengthKm
    {
        get { return _cumulativeKm[^1]; }
    }

    /// <summary>
    /// Der umschließende Kasten der Route (Süd, West, Nord, Ost).
    /// </summary>
    public RouteBounds Bounds
    {
        get
        {
            return new RouteBounds(
                _points.Min(point => point.Latitude),
                _points.Min(point => point.Longitude),
                _points.Max(point => point.Latitude),
                _points.Max(point => point.Longitude));
        }
    }

    /// <summary>
    /// Bestimmt Abstand und Streckenposition eines Punktes. Bei gleichem Abstand (Schleifen, Überschneidungen) gilt die kleinere Streckenposition.
    /// </summary>
    /// <param name="latitude">Breitengrad des Punktes.</param>
    /// <param name="longitude">Längengrad des Punktes.</param>
    /// <param name="limitKm">Teilstrecken, die vom Punkt mehr als diesen Abstand haben, werden grob übersprungen; ohne Treffer ist der Abstand unendlich.</param>
    /// <returns>Die Lage des Punktes zur Route.</returns>
    public RouteLocation Locate(double latitude, double longitude, double limitKm = double.PositiveInfinity)
    {
        var marginLatitude = double.IsFinite(limitKm) ? limitKm / 111.0 : double.PositiveInfinity;
        var best = double.PositiveInfinity;
        var bestAlong = 0.0;
        for (var index = 0; index < _points.Length - 1; index++)
        {
            var a = _points[index];
            var b = _points[index + 1];
            if (double.IsFinite(marginLatitude))
            {
                var marginLongitude = marginLatitude / Math.Max(0.01, Math.Cos(Math.Max(Math.Abs(a.Latitude), Math.Abs(b.Latitude)) * Math.PI / 180.0));
                if (latitude < Math.Min(a.Latitude, b.Latitude) - marginLatitude
                    || latitude > Math.Max(a.Latitude, b.Latitude) + marginLatitude
                    || longitude < Math.Min(a.Longitude, b.Longitude) - marginLongitude
                    || longitude > Math.Max(a.Longitude, b.Longitude) + marginLongitude)
                {
                    continue;
                }
            }

            var distance = RouteGeometry.DistanceToSegmentKm(latitude, longitude, a.Latitude, a.Longitude, b.Latitude, b.Longitude, out var fraction);
            if (distance < best - TieEpsilonKm)
            {
                best = distance;
                bestAlong = _cumulativeKm[index] + (fraction * (_cumulativeKm[index + 1] - _cumulativeKm[index]));
            }
        }

        return new RouteLocation(best, bestAlong);
    }

    /// <summary>
    /// Liefert den Punkt auf der Route an einer Streckenposition.
    /// </summary>
    /// <param name="alongKm">Die Position in Kilometern; sie wird auf 0 bis <see cref="LengthKm"/> begrenzt.</param>
    /// <returns>Der Punkt.</returns>
    public GeoPosition PointAt(double alongKm)
    {
        var clamped = Math.Clamp(double.IsNaN(alongKm) ? 0 : alongKm, 0, LengthKm);
        var index = Array.BinarySearch(_cumulativeKm, clamped);
        if (index >= 0)
        {
            return _points[index];
        }

        var upper = ~index;
        if (upper <= 0)
        {
            return _points[0];
        }

        if (upper >= _points.Length)
        {
            return _points[^1];
        }

        var lower = upper - 1;
        var span = _cumulativeKm[upper] - _cumulativeKm[lower];
        var fraction = span <= 0 ? 0 : (clamped - _cumulativeKm[lower]) / span;
        return new GeoPosition(
            _points[lower].Latitude + (fraction * (_points[upper].Latitude - _points[lower].Latitude)),
            _points[lower].Longitude + (fraction * (_points[upper].Longitude - _points[lower].Longitude)));
    }

    /// <summary>
    /// Vereinfacht die Route für die Kartendarstellung (Douglas-Peucker) auf höchstens die angegebene Punktzahl.
    /// </summary>
    /// <param name="toleranceKm">Die Anfangstoleranz in Kilometern; sie wird verdoppelt, bis die Punktzahl genügt.</param>
    /// <param name="maxPoints">Die höchste Punktzahl, mindestens 2.</param>
    /// <returns>Die vereinfachte Punktfolge mit Start und Ziel.</returns>
    public IReadOnlyList<GeoPosition> Simplify(double toleranceKm, int maxPoints)
    {
        var limit = Math.Max(2, maxPoints);
        var tolerance = Math.Max(0, toleranceKm);
        var result = RouteGeometry.Simplify(_points, tolerance);
        for (var attempt = 0; result.Count > limit && attempt < 40; attempt++)
        {
            tolerance = tolerance <= 0 ? 0.001 : tolerance * 2;
            result = RouteGeometry.Simplify(_points, tolerance);
        }

        return result;
    }

    /// <summary>
    /// Gibt nur den Typnamen zurück, damit die Route nicht versehentlich protokolliert wird.
    /// </summary>
    /// <returns>Ein fester Text.</returns>
    public override string ToString()
    {
        return nameof(RouteProfile);
    }
}

using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Models.Search;

namespace Tankradar.MAUI.Models.Map;

/// <summary>
/// Die auf der Karte eingezeichnete Route als vereinfachte Linie mit Start und Ziel. Sie wird nur im Arbeitsspeicher gehalten
/// (nie gespeichert oder protokolliert) und bei jeder neuen Suche, jedem Wechsel der Suchart und jedem Fehlschlag verworfen.
/// </summary>
public sealed class MapRoute
{
    /// <summary>
    /// Erstellt die Kartenroute.
    /// </summary>
    /// <param name="points">Die Punkte der Linie vom Start zum Ziel, mindestens zwei.</param>
    /// <exception cref="ArgumentException">Es sind weniger als zwei Punkte angegeben.</exception>
    public MapRoute(IReadOnlyList<GeoPosition> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 2)
        {
            throw new ArgumentException("Eine Kartenroute benötigt mindestens zwei Punkte.", nameof(points));
        }

        Points = points;
        Bounds = new RouteBounds(
            points.Min(point => point.Latitude),
            points.Min(point => point.Longitude),
            points.Max(point => point.Latitude),
            points.Max(point => point.Longitude));
    }

    /// <summary>
    /// Die Punkte der Linie vom Start zum Ziel.
    /// </summary>
    public IReadOnlyList<GeoPosition> Points { get; }

    /// <summary>
    /// Der Startpunkt der Linie.
    /// </summary>
    public GeoPosition Start
    {
        get { return Points[0]; }
    }

    /// <summary>
    /// Der Zielpunkt der Linie.
    /// </summary>
    public GeoPosition Destination
    {
        get { return Points[Points.Count - 1]; }
    }

    /// <summary>
    /// Der umschließende Kasten der Linie.
    /// </summary>
    public RouteBounds Bounds { get; }

    /// <summary>
    /// Gibt nur den Typnamen zurück, damit die Route nicht versehentlich protokolliert wird.
    /// </summary>
    /// <returns>Ein fester Text.</returns>
    public override string ToString()
    {
        return nameof(MapRoute);
    }
}

using Tankradar.MAUI.Models.Map;

namespace Tankradar.MAUI.Services.Map;

/// <summary>
/// Der Ausschnitt einer Web-Mercator-Karte (Kachelschema von OpenStreetMap): Mitte, ganzzahlige Zoomstufe und Größe in Pixeln.
/// Die Klasse ist unveränderlich; Verschieben und Zoomen liefern einen neuen Ausschnitt. Sie enthält die gesamte Kartenmathematik
/// (Projektion, Einpassen, sichtbare Kacheln) und ist damit unabhängig von der Oberfläche prüfbar.
/// </summary>
public sealed class MapViewport
{
    /// <summary>
    /// Kantenlänge einer Kachel in Pixeln.
    /// </summary>
    public const int TileSize = 256;

    /// <summary>
    /// Die kleinste zugelassene Zoomstufe.
    /// </summary>
    public const int MinZoom = 4;

    /// <summary>
    /// Die größte zugelassene Zoomstufe (die Kachelserver von OpenStreetMap liefern bis Stufe 19; höhere Stufen sind für Tankstellen unnötig).
    /// </summary>
    public const int MaxZoom = 18;

    /// <summary>
    /// Der größte Breitengrad der Web-Mercator-Projektion.
    /// </summary>
    public const double MaxLatitude = 85.0511287798066;

    private readonly double _centerX;
    private readonly double _centerY;

    private MapViewport(double centerX, double centerY, int zoom, double width, double height)
    {
        Zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
        Width = Math.Max(width, 1);
        Height = Math.Max(height, 1);
        _centerX = centerX;
        _centerY = Math.Clamp(centerY, 0, WorldSize(Zoom));
    }

    /// <summary>
    /// Die Zoomstufe.
    /// </summary>
    public int Zoom { get; }

    /// <summary>
    /// Die Breite des Ausschnitts in Pixeln.
    /// </summary>
    public double Width { get; }

    /// <summary>
    /// Die Höhe des Ausschnitts in Pixeln.
    /// </summary>
    public double Height { get; }

    /// <summary>
    /// Der Breitengrad der Mitte.
    /// </summary>
    public double CenterLatitude
    {
        get { return LatitudeOf(_centerY, Zoom); }
    }

    /// <summary>
    /// Der Längengrad der Mitte.
    /// </summary>
    public double CenterLongitude
    {
        get { return LongitudeOf(_centerX, Zoom); }
    }

    /// <summary>
    /// Erstellt einen Ausschnitt um eine Position.
    /// </summary>
    /// <param name="latitude">Der Breitengrad der Mitte.</param>
    /// <param name="longitude">Der Längengrad der Mitte.</param>
    /// <param name="zoom">Die Zoomstufe (wird auf den zulässigen Bereich begrenzt).</param>
    /// <param name="width">Die Breite in Pixeln.</param>
    /// <param name="height">Die Höhe in Pixeln.</param>
    /// <returns>Der Ausschnitt.</returns>
    public static MapViewport Create(double latitude, double longitude, int zoom, double width, double height)
    {
        var clamped = Math.Clamp(zoom, MinZoom, MaxZoom);
        return new MapViewport(XOf(longitude, clamped), YOf(latitude, clamped), clamped, width, height);
    }

    /// <summary>
    /// Passt den Ausschnitt so ein, dass alle Punkte mit Rand sichtbar sind; bei einem einzelnen Punkt gilt <paramref name="maxZoom"/>.
    /// </summary>
    /// <param name="points">Die Punkte (Breite, Länge); bei einer leeren Liste wird um Mitteleuropa zentriert.</param>
    /// <param name="width">Die Breite in Pixeln.</param>
    /// <param name="height">Die Höhe in Pixeln.</param>
    /// <param name="padding">Der Rand in Pixeln, der von Punkten freibleibt (Markierungen sind größer als ein Punkt).</param>
    /// <param name="maxZoom">Die größte zu wählende Zoomstufe.</param>
    /// <returns>Der Ausschnitt.</returns>
    public static MapViewport Fit(IReadOnlyList<(double Latitude, double Longitude)> points, double width, double height, double padding = 48, int maxZoom = 16)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0)
        {
            return Create(51.0, 10.0, 6, width, height);
        }

        var availableWidth = Math.Max(width - (2 * padding), 1);
        var availableHeight = Math.Max(height - (2 * padding), 1);
        var limit = Math.Clamp(maxZoom, MinZoom, MaxZoom);
        var zoom = limit;
        while (zoom > MinZoom && !Fits(points, zoom, availableWidth, availableHeight))
        {
            zoom--;
        }

        var xs = points.Select(point => XOf(point.Longitude, zoom)).ToList();
        var ys = points.Select(point => YOf(point.Latitude, zoom)).ToList();
        return new MapViewport((xs.Max() + xs.Min()) / 2, (ys.Max() + ys.Min()) / 2, zoom, width, height);
    }

    /// <summary>
    /// Liefert den Ausschnitt mit anderer Größe bei gleicher Mitte und Zoomstufe.
    /// </summary>
    /// <param name="width">Die neue Breite in Pixeln.</param>
    /// <param name="height">Die neue Höhe in Pixeln.</param>
    /// <returns>Der Ausschnitt.</returns>
    public MapViewport Resize(double width, double height)
    {
        return new MapViewport(_centerX, _centerY, Zoom, width, height);
    }

    /// <summary>
    /// Verschiebt den Ausschnitt; ein positiver Wert bewegt die Karte nach rechts bzw. unten, der Ausschnitt also nach links bzw. oben.
    /// </summary>
    /// <param name="deltaX">Die Verschiebung der Karte nach rechts in Pixeln.</param>
    /// <param name="deltaY">Die Verschiebung der Karte nach unten in Pixeln.</param>
    /// <returns>Der verschobene Ausschnitt.</returns>
    public MapViewport PanByPixels(double deltaX, double deltaY)
    {
        var world = WorldSize(Zoom);
        var x = _centerX - deltaX;
        x = ((x % world) + world) % world;
        return new MapViewport(x, _centerY - deltaY, Zoom, Width, Height);
    }

    /// <summary>
    /// Ändert die Zoomstufe um den Wert und behält die Mitte bei.
    /// </summary>
    /// <param name="delta">Die Änderung (positiv vergrößert).</param>
    /// <returns>Der Ausschnitt; derselbe, wenn die Grenze erreicht ist.</returns>
    public MapViewport ZoomBy(int delta)
    {
        var target = Math.Clamp(Zoom + delta, MinZoom, MaxZoom);
        if (target == Zoom)
        {
            return this;
        }

        return Create(CenterLatitude, CenterLongitude, target, Width, Height);
    }

    /// <summary>
    /// Berechnet die Lage einer Position im Ausschnitt.
    /// </summary>
    /// <param name="latitude">Der Breitengrad.</param>
    /// <param name="longitude">Der Längengrad.</param>
    /// <returns>Die Lage in Pixeln, vom linken oberen Rand des Ausschnitts aus (auch außerhalb des Ausschnitts).</returns>
    public ScreenPoint ToScreen(double latitude, double longitude)
    {
        var world = WorldSize(Zoom);
        var dx = XOf(longitude, Zoom) - _centerX;

        // Kürzester Weg um die Weltkugel (Datumsgrenze).
        if (dx > world / 2)
        {
            dx -= world;
        }
        else if (dx < -world / 2)
        {
            dx += world;
        }

        return new ScreenPoint((Width / 2) + dx, (Height / 2) + (YOf(latitude, Zoom) - _centerY));
    }

    /// <summary>
    /// Gibt an, ob eine Position (mit Rand) im Ausschnitt liegt.
    /// </summary>
    /// <param name="latitude">Der Breitengrad.</param>
    /// <param name="longitude">Der Längengrad.</param>
    /// <param name="margin">Der Rand in Pixeln, um den der Ausschnitt gedanklich vergrößert wird.</param>
    /// <returns><see langword="true"/>, wenn die Position sichtbar ist.</returns>
    public bool Contains(double latitude, double longitude, double margin = 0)
    {
        var (x, y) = ToScreen(latitude, longitude);
        return x >= -margin && x <= Width + margin && y >= -margin && y <= Height + margin;
    }

    /// <summary>
    /// Liefert die sichtbaren Kacheln mit ihrer Lage; Zeilen außerhalb der Weltkarte entfallen, Spalten werden umgebrochen.
    /// </summary>
    /// <returns>Die Kacheln von links oben nach rechts unten.</returns>
    public IReadOnlyList<PlacedTile> VisibleTiles()
    {
        var count = 1 << Zoom;
        var originX = _centerX - (Width / 2);
        var originY = _centerY - (Height / 2);
        var firstColumn = (int)Math.Floor(originX / TileSize);
        var lastColumn = (int)Math.Floor((originX + Width) / TileSize);
        var firstRow = Math.Max((int)Math.Floor(originY / TileSize), 0);
        var lastRow = Math.Min((int)Math.Floor((originY + Height) / TileSize), count - 1);
        var tiles = new List<PlacedTile>();
        for (var row = firstRow; row <= lastRow; row++)
        {
            for (var column = firstColumn; column <= lastColumn; column++)
            {
                var wrapped = ((column % count) + count) % count;
                tiles.Add(new PlacedTile(new TileKey(Zoom, wrapped, row), (column * TileSize) - originX, (row * TileSize) - originY));
            }
        }

        return tiles;
    }

    private static bool Fits(IReadOnlyList<(double Latitude, double Longitude)> points, int zoom, double availableWidth, double availableHeight)
    {
        var xs = points.Select(point => XOf(point.Longitude, zoom)).ToList();
        var ys = points.Select(point => YOf(point.Latitude, zoom)).ToList();
        return xs.Max() - xs.Min() <= availableWidth && ys.Max() - ys.Min() <= availableHeight;
    }

    private static double WorldSize(int zoom)
    {
        return (double)TileSize * (1 << zoom);
    }

    private static double XOf(double longitude, int zoom)
    {
        return (longitude + 180.0) / 360.0 * WorldSize(zoom);
    }

    private static double YOf(double latitude, int zoom)
    {
        var radians = Math.Clamp(latitude, -MaxLatitude, MaxLatitude) * Math.PI / 180.0;
        var mercator = Math.Log(Math.Tan(radians) + (1.0 / Math.Cos(radians)));
        return (1.0 - (mercator / Math.PI)) / 2.0 * WorldSize(zoom);
    }

    private static double LongitudeOf(double x, int zoom)
    {
        var longitude = (x / WorldSize(zoom) * 360.0) - 180.0;
        return ((((longitude + 180.0) % 360.0) + 360.0) % 360.0) - 180.0;
    }

    private static double LatitudeOf(double y, int zoom)
    {
        var n = Math.PI - (2.0 * Math.PI * y / WorldSize(zoom));
        return 180.0 / Math.PI * Math.Atan(Math.Sinh(n));
    }
}

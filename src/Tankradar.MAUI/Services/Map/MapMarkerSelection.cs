using Tankradar.MAUI.Models.Map;

namespace Tankradar.MAUI.Services.Map;

/// <summary>
/// Wählt die Markierungen, für die die Karte tatsächlich Schaltflächen aufbaut: nur Stationen im sichtbaren Ausschnitt, höchstens <see cref="MaxRendered"/>
/// (die der Bildmitte nächsten zuerst). Jede Markierung ist ein Element im Oberflächenbaum und in der Bedienhilfe (UI Automation); ohne Obergrenze
/// würde eine große Ergebnismenge Speicher, Layout und Bedienhilfen unverhältnismäßig belasten.
/// </summary>
public static class MapMarkerSelection
{
    /// <summary>
    /// Obergrenze der gleichzeitig aufgebauten Markierungen.
    /// </summary>
    public const int MaxRendered = 100;

    /// <summary>
    /// Das Ergebnis der Auswahl.
    /// </summary>
    /// <param name="Rendered">Die aufzubauenden Markierungen.</param>
    /// <param name="InViewport">Die Anzahl aller Markierungen im Ausschnitt.</param>
    /// <returns>Der Wert.</returns>
    public sealed record Result(IReadOnlyList<MapMarker> Rendered, int InViewport)
    {
        /// <summary>
        /// Gibt an, ob wegen der Obergrenze nicht alle Markierungen im Ausschnitt aufgebaut werden.
        /// </summary>
        public bool IsTruncated => Rendered.Count < InViewport;
    }

    /// <summary>
    /// Wählt die aufzubauenden Markierungen.
    /// </summary>
    /// <param name="markers">Alle Markierungen der Ergebnismenge.</param>
    /// <param name="viewport">Der sichtbare Ausschnitt.</param>
    /// <returns>Die Auswahl.</returns>
    public static Result Select(IReadOnlyList<MapMarker> markers, MapViewport viewport)
    {
        ArgumentNullException.ThrowIfNull(markers);
        ArgumentNullException.ThrowIfNull(viewport);
        var inside = markers.Where(marker => viewport.Contains(marker.Latitude, marker.Longitude)).ToList();
        if (inside.Count <= MaxRendered)
        {
            return new Result(inside, inside.Count);
        }

        var centerX = viewport.Width / 2;
        var centerY = viewport.Height / 2;
        var nearest = inside
            .OrderBy(marker =>
            {
                var (x, y) = viewport.ToScreen(marker.Latitude, marker.Longitude);
                return ((x - centerX) * (x - centerX)) + ((y - centerY) * (y - centerY));
            })
            .Take(MaxRendered)
            .ToList();
        return new Result(nearest, inside.Count);
    }
}

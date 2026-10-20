using Tankradar.MAUI.Models.Map;
using Tankradar.MAUI.Models.Search;

namespace Tankradar.MAUI.Services.Map;

/// <summary>
/// Projiziert die Linie einer Route auf Bildschirmkoordinaten eines <see cref="MapViewport"/>: Punkte, die auf dem Bildschirm nahezu zusammenfallen,
/// werden zusammengefasst (Vereinfachung abhängig von der Zoomstufe), und die Linie wird auf den sichtbaren Ausschnitt zugeschnitten.
/// </summary>
public static class MapRouteProjector
{
    /// <summary>
    /// Der Mindestabstand in Pixeln, den aufeinanderfolgende Punkte der Linie haben.
    /// </summary>
    public const double MinPointDistance = 1.5;

    /// <summary>
    /// Projiziert die Punkte und schneidet die Linie auf den sichtbaren Ausschnitt (zuzüglich Rand) zu.
    /// </summary>
    /// <param name="viewport">Der sichtbare Ausschnitt.</param>
    /// <param name="points">Die Punkte der Linie.</param>
    /// <param name="margin">Der Rand in Pixeln, um den der Ausschnitt gedanklich vergrößert wird.</param>
    /// <returns>Die sichtbaren Teillinien; die Linie wird dort unterbrochen, wo sie den Ausschnitt verlässt.</returns>
    public static IReadOnlyList<IReadOnlyList<ScreenPoint>> Project(MapViewport viewport, IReadOnlyList<GeoPosition> points, double margin = 16)
    {
        ArgumentNullException.ThrowIfNull(viewport);
        ArgumentNullException.ThrowIfNull(points);

        var screen = new List<ScreenPoint>(Math.Min(points.Count, 2048));
        for (var index = 0; index < points.Count; index++)
        {
            var current = viewport.ToScreen(points[index].Latitude, points[index].Longitude);
            if (screen.Count > 0 && index < points.Count - 1)
            {
                var last = screen[^1];
                var dx = current.X - last.X;
                var dy = current.Y - last.Y;
                if ((dx * dx) + (dy * dy) < MinPointDistance * MinPointDistance)
                {
                    continue;
                }
            }

            screen.Add(current);
        }

        var lines = new List<IReadOnlyList<ScreenPoint>>();
        List<ScreenPoint>? currentLine = null;
        for (var index = 0; index < screen.Count - 1; index++)
        {
            if (!TryClip(screen[index], screen[index + 1], -margin, -margin, viewport.Width + margin, viewport.Height + margin, out var start, out var end, out var startClipped, out var endClipped))
            {
                currentLine = null;
                continue;
            }

            if (currentLine is null || startClipped)
            {
                currentLine = [start];
                lines.Add(currentLine);
            }

            currentLine.Add(end);
            if (endClipped)
            {
                currentLine = null;
            }
        }

        return lines;
    }

    private static bool TryClip(
        ScreenPoint from,
        ScreenPoint to,
        double left,
        double top,
        double right,
        double bottom,
        out ScreenPoint start,
        out ScreenPoint end,
        out bool startClipped,
        out bool endClipped)
    {
        // Liang-Barsky.
        var t0 = 0.0;
        var t1 = 1.0;
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        start = from;
        end = to;
        startClipped = false;
        endClipped = false;

        double[] p = [-dx, dx, -dy, dy];
        double[] q = [from.X - left, right - from.X, from.Y - top, bottom - from.Y];
        for (var side = 0; side < 4; side++)
        {
            if (p[side] == 0)
            {
                if (q[side] < 0)
                {
                    return false;
                }

                continue;
            }

            var ratio = q[side] / p[side];
            if (p[side] < 0)
            {
                if (ratio > t1)
                {
                    return false;
                }

                t0 = Math.Max(t0, ratio);
            }
            else
            {
                if (ratio < t0)
                {
                    return false;
                }

                t1 = Math.Min(t1, ratio);
            }
        }

        startClipped = t0 > 0;
        endClipped = t1 < 1;
        start = new ScreenPoint(from.X + (t0 * dx), from.Y + (t0 * dy));
        end = new ScreenPoint(from.X + (t1 * dx), from.Y + (t1 * dy));
        return true;
    }
}

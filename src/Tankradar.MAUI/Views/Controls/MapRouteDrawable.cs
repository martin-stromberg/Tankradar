using Tankradar.MAUI.Models.Map;

namespace Tankradar.MAUI.Views.Controls;

/// <summary>
/// Zeichnet die Linie einer Route (heller Rand, darüber Teal) auf der Karte. Die Punkte sind bereits auf Bildschirmkoordinaten projiziert und zugeschnitten.
/// </summary>
public sealed class MapRouteDrawable : IDrawable
{
    private static readonly Color Casing = Colors.White;
    private static readonly Color Line = Color.FromArgb("#0F766E");

    /// <summary>
    /// Die zu zeichnenden Teillinien in Bildschirmkoordinaten.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<ScreenPoint>> Lines { get; set; } = [];

    /// <inheritdoc />
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;
        foreach (var line in Lines)
        {
            if (line.Count < 2)
            {
                continue;
            }

            var path = new PathF();
            path.MoveTo((float)line[0].X, (float)line[0].Y);
            for (var index = 1; index < line.Count; index++)
            {
                path.LineTo((float)line[index].X, (float)line[index].Y);
            }

            canvas.StrokeColor = Casing;
            canvas.StrokeSize = 9;
            canvas.DrawPath(path);
            canvas.StrokeColor = Line;
            canvas.StrokeSize = 5;
            canvas.DrawPath(path);
        }
    }
}

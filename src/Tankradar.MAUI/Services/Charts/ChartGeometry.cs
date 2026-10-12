using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Charts;

/// <summary>
/// Reine Layoutberechnung der Diagramme: Wertebereich, Koordinaten und Beschriftungsstellen. Die Zeichnung selbst
/// (<c>LineChartDrawable</c>, <c>BarChartDrawable</c>) verwendet nur das Ergebnis.
/// </summary>
public static class ChartGeometry
{
    /// <summary>
    /// Höchstzahl der beschrifteten Stellen der Achse.
    /// </summary>
    public const int MaxLabels = 4;

    /// <summary>
    /// Bis zu dieser Zahl von Datenpunkten wird der Wert an jedem Punkt gezeichnet, darüber nur am letzten.
    /// </summary>
    public const int MaxValueLabels = 6;

    private const float PaddingLeft = 12f;
    private const float PaddingRight = 12f;
    private const float PaddingTop = 22f;
    private const float PaddingBottom = 26f;
    private const float BarWidthShare = 0.6f;
    private const decimal LineMargin = 0.15m;

    /// <summary>
    /// Berechnet das Layout eines Diagramms.
    /// </summary>
    /// <param name="data">Die Daten.</param>
    /// <param name="bounds">Die Fläche des Diagramms.</param>
    /// <returns>Das Layout; ohne Datenpunkte ohne Einträge.</returns>
    public static ChartLayout Layout(ChartData data, RectF bounds)
    {
        ArgumentNullException.ThrowIfNull(data);
        var plot = new RectF(
            bounds.X + PaddingLeft,
            bounds.Y + PaddingTop,
            Math.Max(1f, bounds.Width - PaddingLeft - PaddingRight),
            Math.Max(1f, bounds.Height - PaddingTop - PaddingBottom));
        var points = data.Points.Skip(Math.Max(0, data.Points.Count - TankbookStatisticsLimits.ChartMaxPoints)).ToList();
        if (points.Count == 0)
        {
            return new ChartLayout(plot, 0m, 1m, []);
        }

        var (minimum, maximum) = GetRange(data.Kind, points);
        var labelIndices = GetLabelIndices(points.Count);
        var items = new List<ChartItem>(points.Count);
        for (var index = 0; index < points.Count; index++)
        {
            var y = plot.Bottom - (float)((points[index].Value - minimum) / (maximum - minimum)) * plot.Height;
            var showValue = points.Count <= MaxValueLabels || index == points.Count - 1;
            var showLabel = labelIndices.Contains(index);
            if (data.Kind == ChartKind.Bar)
            {
                var slot = plot.Width / points.Count;
                var width = slot * BarWidthShare;
                var centerX = plot.Left + slot * (index + 0.5f);
                var bar = new RectF(centerX - width / 2f, y, width, plot.Bottom - y);
                items.Add(new ChartItem(points[index], new PointF(centerX, y), bar, showValue, showLabel));
            }
            else
            {
                var x = points.Count == 1 ? plot.Left + plot.Width / 2f : plot.Left + plot.Width * index / (points.Count - 1);
                items.Add(new ChartItem(points[index], new PointF(x, y), RectF.Zero, showValue, showLabel));
            }
        }

        return new ChartLayout(plot, minimum, maximum, items);
    }

    private static (decimal Minimum, decimal Maximum) GetRange(ChartKind kind, List<ChartPoint> points)
    {
        var lowest = points.Min(point => point.Value);
        var highest = points.Max(point => point.Value);
        if (kind == ChartKind.Bar)
        {
            return (0m, highest > 0m ? highest : 1m);
        }

        if (highest == lowest)
        {
            var margin = Math.Max(Math.Abs(highest) * 0.1m, 1m);
            return (lowest - margin, highest + margin);
        }

        var spread = (highest - lowest) * LineMargin;
        return (lowest - spread, highest + spread);
    }

    private static HashSet<int> GetLabelIndices(int count)
    {
        if (count <= MaxLabels)
        {
            return [.. Enumerable.Range(0, count)];
        }

        var result = new HashSet<int>();
        for (var slot = 0; slot < MaxLabels; slot++)
        {
            result.Add((int)Math.Round((double)slot * (count - 1) / (MaxLabels - 1), MidpointRounding.AwayFromZero));
        }

        return result;
    }
}

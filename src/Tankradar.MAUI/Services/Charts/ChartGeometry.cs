using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Charts;

/// <summary>
/// Reine Layoutberechnung der Diagramme: Wertebereich, Koordinaten und Beschriftungsstellen. Die Zeichnung selbst
/// (<c>BarChartDrawable</c>) verwendet nur das Ergebnis.
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

    /// <summary>
    /// Breite der Fläche eines Wertes bzw. einer Achsenbeschriftung; breiter als jeder vorkommende Text bei 11 pt.
    /// </summary>
    public const float TextAreaWidth = 60f;

    /// <summary>
    /// Höhe der Fläche eines Wertes bzw. einer Achsenbeschriftung.
    /// </summary>
    public const float TextAreaHeight = 16f;

    private const float ValueOffset = 18f;
    private const float LabelOffset = 4f;
    private const float PaddingLeft = 12f;
    private const float PaddingRight = 12f;
    private const float PaddingTop = 22f;
    private const float PaddingBottom = 26f;
    private const float BarWidthShare = 0.6f;
    private const float MaxBarWidth = 44f;

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

        var minimum = 0m;
        var maximum = Math.Max(points.Max(point => point.Value), 0m);
        if (maximum <= 0m)
        {
            maximum = 1m;
        }

        var labelIndices = GetLabelIndices(points.Count);
        var items = new List<ChartItem>(points.Count);
        var slot = plot.Width / points.Count;
        var width = Math.Min(slot * BarWidthShare, MaxBarWidth);
        for (var index = 0; index < points.Count; index++)
        {
            var y = plot.Bottom - (float)((points[index].Value - minimum) / (maximum - minimum)) * plot.Height;
            var showValue = points.Count <= MaxValueLabels || index == points.Count - 1;
            var showLabel = labelIndices.Contains(index);
            var centerX = plot.Left + slot * (index + 0.5f);
            var bar = new RectF(centerX - width / 2f, y, width, plot.Bottom - y);
            items.Add(new ChartItem(
                points[index],
                new PointF(centerX, y),
                bar,
                showValue,
                showLabel,
                PlaceText(centerX, y - ValueOffset, bounds),
                PlaceText(centerX, plot.Bottom + LabelOffset, bounds),
                index == points.Count - 1));
        }

        return new ChartLayout(plot, minimum, maximum, items);
    }

    /// <summary>
    /// Legt eine Textfläche mittig um die Stelle und schiebt sie in die Diagrammfläche, wenn sie über den Rand ragen würde.
    /// </summary>
    /// <param name="centerX">Die Mitte des Textes (Mitte des Balkens).</param>
    /// <param name="top">Die Oberkante der Fläche.</param>
    /// <param name="bounds">Die Diagrammfläche.</param>
    /// <returns>Die Fläche mit Ausrichtung.</returns>
    private static ChartTextArea PlaceText(float centerX, float top, RectF bounds)
    {
        var left = centerX - TextAreaWidth / 2f;
        var alignment = HorizontalAlignment.Center;
        if (left < bounds.Left)
        {
            left = bounds.Left;
            alignment = HorizontalAlignment.Left;
        }
        else if (left + TextAreaWidth > bounds.Right)
        {
            left = Math.Max(bounds.Left, bounds.Right - TextAreaWidth);
            alignment = HorizontalAlignment.Right;
        }

        return new ChartTextArea(new RectF(left, top, TextAreaWidth, TextAreaHeight), alignment);
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

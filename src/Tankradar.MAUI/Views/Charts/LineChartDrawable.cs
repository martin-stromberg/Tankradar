using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources;
using Tankradar.MAUI.Services.Charts;

namespace Tankradar.MAUI.Views.Charts;

/// <summary>
/// Zeichnet ein Liniendiagramm mit einem Punkt je Wert anhand von <see cref="ChartGeometry"/>.
/// </summary>
public sealed class LineChartDrawable : IDrawable
{
    private const float LineWidth = 2.5f;
    private const float PointRadius = 4f;
    private const float FontSize = 11f;

    private readonly ChartData _data;

    /// <summary>
    /// Erstellt die Zeichnung.
    /// </summary>
    /// <param name="data">Die Daten.</param>
    public LineChartDrawable(ChartData data)
    {
        _data = data;
    }

    /// <inheritdoc />
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var layout = ChartGeometry.Layout(_data, dirtyRect);
        if (layout.Items.Count == 0)
        {
            return;
        }

        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        canvas.StrokeColor = ChartPalette.Axis(isDark);
        canvas.StrokeSize = 1f;
        canvas.DrawLine(layout.Plot.Left, layout.Plot.Bottom, layout.Plot.Right, layout.Plot.Bottom);

        canvas.StrokeColor = ChartPalette.Series(isDark);
        canvas.StrokeSize = LineWidth;
        for (var index = 1; index < layout.Items.Count; index++)
        {
            canvas.DrawLine(layout.Items[index - 1].Point, layout.Items[index].Point);
        }

        canvas.FillColor = ChartPalette.Series(isDark);
        canvas.FontColor = ChartPalette.Text(isDark);
        canvas.FontSize = FontSize;
        foreach (var item in layout.Items)
        {
            canvas.FillCircle(item.Point, PointRadius);
            if (item.ShowValue)
            {
                canvas.DrawString(item.Source.ValueText, item.Point.X - 30f, item.Point.Y - 22f, 60f, 16f, HorizontalAlignment.Center, VerticalAlignment.Center);
            }

            if (item.ShowLabel)
            {
                canvas.DrawString(item.Source.Label, item.Point.X - 30f, layout.Plot.Bottom + 4f, 60f, 16f, HorizontalAlignment.Center, VerticalAlignment.Center);
            }
        }
    }
}

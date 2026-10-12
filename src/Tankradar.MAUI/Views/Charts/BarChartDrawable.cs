using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources;
using Tankradar.MAUI.Services.Charts;

namespace Tankradar.MAUI.Views.Charts;

/// <summary>
/// Zeichnet ein Balkendiagramm mit einem Balken je Wert anhand von <see cref="ChartGeometry"/>; die Achse beginnt bei 0.
/// </summary>
public sealed class BarChartDrawable : IDrawable
{
    private const float CornerRadius = 3f;
    private const float FontSize = 11f;

    private readonly ChartData _data;

    /// <summary>
    /// Erstellt die Zeichnung.
    /// </summary>
    /// <param name="data">Die Daten.</param>
    public BarChartDrawable(ChartData data)
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

        canvas.FillColor = ChartPalette.Series(isDark);
        canvas.FontColor = ChartPalette.Text(isDark);
        canvas.FontSize = FontSize;
        foreach (var item in layout.Items)
        {
            if (item.Bar.Height > 0f)
            {
                canvas.FillRoundedRectangle(item.Bar, CornerRadius);
            }

            if (item.ShowValue)
            {
                canvas.DrawString(item.Source.ValueText, item.Point.X - 30f, item.Point.Y - 18f, 60f, 16f, HorizontalAlignment.Center, VerticalAlignment.Center);
            }

            if (item.ShowLabel)
            {
                canvas.DrawString(item.Source.Label, item.Point.X - 30f, layout.Plot.Bottom + 4f, 60f, 16f, HorizontalAlignment.Center, VerticalAlignment.Center);
            }
        }
    }
}

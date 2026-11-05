using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources;
using Tankradar.MAUI.Services.Charts;

namespace Tankradar.MAUI.Views.Charts;

/// <summary>
/// Zeichnet ein Balkendiagramm mit einem Balken je Wert anhand von <see cref="ChartGeometry"/>; die Achse beginnt bei 0, der letzte Balken ist hervorgehoben.
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

        canvas.FontColor = ChartPalette.Text(isDark);
        canvas.FontSize = FontSize;
        foreach (var item in layout.Items)
        {
            canvas.FillColor = item.IsLast ? ChartPalette.Series(isDark) : ChartPalette.SeriesMuted(isDark);
            if (item.Bar.Height > 0f)
            {
                canvas.FillRoundedRectangle(item.Bar, CornerRadius);
            }

            canvas.Font = item.IsLast ? Microsoft.Maui.Graphics.Font.DefaultBold : Microsoft.Maui.Graphics.Font.Default;
            if (item.ShowValue)
            {
                DrawText(canvas, item.Source.ValueText, item.ValueArea);
            }

            if (item.ShowLabel)
            {
                DrawText(canvas, item.Source.Label, item.LabelArea);
            }
        }
    }

    private static void DrawText(ICanvas canvas, string text, ChartTextArea area)
    {
        canvas.DrawString(text, area.Area.X, area.Area.Y, area.Area.Width, area.Area.Height, area.Alignment, VerticalAlignment.Center);
    }
}

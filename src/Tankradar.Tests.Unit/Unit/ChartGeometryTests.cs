using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Charts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die reine Layoutberechnung <see cref="ChartGeometry"/>: Skalierung, Randfälle, Begrenzung, Beschriftungsstellen und Textflächen am Rand.
/// </summary>
public class ChartGeometryTests : BaseTest
{
    private static readonly RectF Bounds = new(0f, 0f, 300f, 200f);

    private static ChartData Data(params decimal[] values)
    {
        return new ChartData(values.Select((value, index) => new ChartPoint($"L{index}", value, value.ToString("0.00"))).ToList(), "x");
    }

    /// <summary>
    /// Prüft, dass alle Punkte im Zeichenbereich liegen, der höhere Wert höher steht und die Reihenfolge von links nach rechts erhalten bleibt.
    /// </summary>
    [Fact]
    public void Layout_ScalesValuesIntoBounds()
    {
        var layout = ChartGeometry.Layout(Data(5m, 6m, 5.5m), Bounds);

        Assert.All(layout.Items, item =>
        {
            Assert.InRange(item.Point.X, layout.Plot.Left, layout.Plot.Right);
            Assert.InRange(item.Point.Y, layout.Plot.Top, layout.Plot.Bottom);
        });
        Assert.True(layout.Items[1].Point.Y < layout.Items[2].Point.Y);
        Assert.True(layout.Items[2].Point.Y < layout.Items[0].Point.Y);
        Assert.True(layout.Items[0].Point.X < layout.Items[1].Point.X);
        Assert.True(layout.Items[1].Point.X < layout.Items[2].Point.X);
        Assert.Equal(0m, layout.Minimum);
        Assert.Equal(6m, layout.Maximum);
    }

    /// <summary>
    /// Prüft, dass ein einzelner Punkt in der Mitte liegt.
    /// </summary>
    [Fact]
    public void Layout_SinglePoint()
    {
        var layout = ChartGeometry.Layout(Data(6m), Bounds);

        var item = Assert.Single(layout.Items);
        Assert.Equal(layout.Plot.Left + layout.Plot.Width / 2f, item.Point.X, 0.01f);
        Assert.True(item.ShowValue);
        Assert.True(item.ShowLabel);
    }

    /// <summary>
    /// Prüft, dass gleiche Werte keine Division durch 0 und eine endliche Höhe ergeben.
    /// </summary>
    [Fact]
    public void Layout_AllEqualValues_NoDivisionByZero()
    {
        var layout = ChartGeometry.Layout(Data(6m, 6m, 6m), Bounds);

        Assert.All(layout.Items, item =>
        {
            Assert.True(float.IsFinite(item.Point.Y));
            Assert.InRange(item.Point.Y, layout.Plot.Top, layout.Plot.Bottom);
        });
        Assert.True(layout.Maximum > layout.Minimum);

        var zeros = ChartGeometry.Layout(Data(0m, 0m), Bounds);
        Assert.All(zeros.Items, item => Assert.True(float.IsFinite(item.Point.Y)));
        Assert.True(zeros.Maximum > zeros.Minimum);
    }

    /// <summary>
    /// Prüft, dass Balken bei 0 beginnen, der höchste die volle Höhe hat und ein Wert 0 keine Höhe.
    /// </summary>
    [Fact]
    public void Layout_Bars_StartAtZero()
    {
        var layout = ChartGeometry.Layout(Data(0m, 50m, 100m), Bounds);

        Assert.Equal(0m, layout.Minimum);
        Assert.Equal(100m, layout.Maximum);
        Assert.Equal(0f, layout.Items[0].Bar.Height, 0.01f);
        Assert.Equal(layout.Plot.Height / 2f, layout.Items[1].Bar.Height, 0.01f);
        Assert.Equal(layout.Plot.Height, layout.Items[2].Bar.Height, 0.01f);
        Assert.All(layout.Items, item => Assert.Equal(layout.Plot.Bottom, item.Bar.Bottom, 0.01f));
    }

    /// <summary>
    /// Prüft, dass nur die neuesten Datenpunkte gezeichnet werden und die Werte nur bei wenigen Punkten alle beschriftet sind.
    /// </summary>
    [Fact]
    public void Layout_LimitsToMaxPoints()
    {
        var values = Enumerable.Range(1, 20).Select(value => (decimal)value).ToArray();

        var layout = ChartGeometry.Layout(Data(values), Bounds);

        Assert.Equal(TankbookStatisticsLimits.ChartMaxPoints, layout.Items.Count);
        Assert.Equal("L8", layout.Items[0].Source.Label);
        Assert.Equal("L19", layout.Items[^1].Source.Label);
        Assert.Single(layout.Items, item => item.ShowValue);
        Assert.True(layout.Items[^1].ShowValue);
    }

    /// <summary>
    /// Prüft, dass höchstens vier Stellen der Achse beschriftet werden, erste und letzte immer.
    /// </summary>
    [Fact]
    public void Layout_LabelPositions_AtMostFour()
    {
        var many = ChartGeometry.Layout(Data(Enumerable.Range(1, 12).Select(value => (decimal)value).ToArray()), Bounds);
        var few = ChartGeometry.Layout(Data(1m, 2m, 3m), Bounds);

        Assert.InRange(many.Items.Count(item => item.ShowLabel), 1, ChartGeometry.MaxLabels);
        Assert.True(many.Items[0].ShowLabel);
        Assert.True(many.Items[^1].ShowLabel);
        Assert.Equal(3, few.Items.Count(item => item.ShowLabel));
    }

    /// <summary>
    /// Prüft, dass ein Diagramm ohne Daten ein leeres Layout ergibt.
    /// </summary>
    [Fact]
    public void Layout_NoPoints_IsEmpty()
    {
        var layout = ChartGeometry.Layout(new ChartData([], "x"), Bounds);

        Assert.Empty(layout.Items);
    }

    /// <summary>
    /// Prüft, dass nur der letzte Balken als hervorgehoben gilt.
    /// </summary>
    [Fact]
    public void Layout_OnlyLastItemIsHighlighted()
    {
        var layout = ChartGeometry.Layout(Data(5m, 6m, 5.5m), Bounds);

        Assert.Equal([false, false, true], layout.Items.Select(item => item.IsLast));
    }

    /// <summary>
    /// Prüft, dass alle Textflächen (Werte und Achsenbeschriftungen) innerhalb der Diagrammfläche liegen, auch bei vielen Balken und schmaler Fläche,
    /// damit am Rand nichts abgeschnitten wird.
    /// </summary>
    /// <param name="count">Die Zahl der Balken.</param>
    /// <param name="width">Die Breite der Diagrammfläche.</param>
    [Theory]
    [InlineData(1, 308f)]
    [InlineData(2, 308f)]
    [InlineData(6, 308f)]
    [InlineData(12, 308f)]
    [InlineData(12, 280f)]
    [InlineData(6, 200f)]
    public void Layout_TextAreas_StayInsideBounds(int count, float width)
    {
        var bounds = new RectF(0f, 0f, width, 140f);
        var layout = ChartGeometry.Layout(Data(Enumerable.Range(1, count).Select(value => (decimal)value).ToArray()), bounds);

        Assert.All(layout.Items, item =>
        {
            foreach (var text in new[] { item.ValueArea, item.LabelArea })
            {
                Assert.True(text.Area.Left >= bounds.Left - 0.01f, $"links: {text.Area.Left}");
                Assert.True(text.Area.Right <= bounds.Right + 0.01f, $"rechts: {text.Area.Right}");
                Assert.Equal(ChartGeometry.TextAreaWidth, text.Area.Width, 0.01f);
            }
        });
    }

    /// <summary>
    /// Prüft die Ausrichtung: Die erste Beschriftung wird am linken Rand linksbündig, die letzte am rechten Rand rechtsbündig gesetzt,
    /// sobald die Mitte des Balkens weniger als die halbe Textfläche vom Rand entfernt ist; sonst steht der Text mittig unter dem Balken.
    /// </summary>
    [Fact]
    public void Layout_TextAreas_AlignAtEdges()
    {
        var bounds = new RectF(0f, 0f, 308f, 140f);
        var twelve = ChartGeometry.Layout(Data(Enumerable.Range(1, 12).Select(value => (decimal)value).ToArray()), bounds);
        var three = ChartGeometry.Layout(Data(1m, 2m, 3m), bounds);

        Assert.Equal(HorizontalAlignment.Left, twelve.Items[0].LabelArea.Alignment);
        Assert.Equal(0f, twelve.Items[0].LabelArea.Area.Left, 0.01f);
        Assert.Equal(HorizontalAlignment.Right, twelve.Items[^1].LabelArea.Alignment);
        Assert.Equal(308f, twelve.Items[^1].LabelArea.Area.Right, 0.01f);
        Assert.Equal(HorizontalAlignment.Right, twelve.Items[^1].ValueArea.Alignment);
        Assert.All(three.Items, item =>
        {
            Assert.Equal(HorizontalAlignment.Center, item.LabelArea.Alignment);
            Assert.Equal(item.Point.X, item.LabelArea.Area.Center.X, 0.01f);
        });
    }

    /// <summary>
    /// Prüft, dass die Textflächen vertikal innerhalb der Diagrammfläche liegen (Wert über dem Balken, Beschriftung unter der Achse).
    /// </summary>
    [Fact]
    public void Layout_TextAreas_StayInsideVertically()
    {
        var layout = ChartGeometry.Layout(Data(1m, 50m, 100m), Bounds);

        Assert.All(layout.Items, item =>
        {
            Assert.True(item.ValueArea.Area.Top >= Bounds.Top);
            Assert.True(item.ValueArea.Area.Bottom <= item.Point.Y + 0.01f);
            Assert.True(item.LabelArea.Area.Top >= layout.Plot.Bottom);
            Assert.True(item.LabelArea.Area.Bottom <= Bounds.Bottom);
        });
    }

    /// <summary>
    /// Prüft, dass ein einzelner Balken nicht die ganze Breite füllt, die Balken aber bei mehreren Werten in ihren Platz passen.
    /// </summary>
    [Fact]
    public void Layout_BarWidth_IsCapped()
    {
        var single = ChartGeometry.Layout(Data(5m), Bounds);
        var twelve = ChartGeometry.Layout(Data(Enumerable.Range(1, 12).Select(value => (decimal)value).ToArray()), Bounds);

        Assert.True(single.Items[0].Bar.Width <= 44.01f);
        Assert.Equal(single.Plot.Left + single.Plot.Width / 2f, single.Items[0].Bar.Center.X, 0.01f);
        Assert.All(twelve.Items, item => Assert.True(item.Bar.Width < twelve.Plot.Width / 12f));
    }
}

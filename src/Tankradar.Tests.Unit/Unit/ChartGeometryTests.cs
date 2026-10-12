using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Charts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die reine Layoutberechnung <see cref="ChartGeometry"/>: Skalierung, Randfälle, Begrenzung und Beschriftungsstellen.
/// </summary>
public class ChartGeometryTests : BaseTest
{
    private static readonly RectF Bounds = new(0f, 0f, 300f, 200f);

    private static ChartData Data(ChartKind kind, params decimal[] values)
    {
        return new ChartData(kind, values.Select((value, index) => new ChartPoint($"L{index}", value, value.ToString("0.00"))).ToList(), "x");
    }

    /// <summary>
    /// Prüft, dass alle Punkte im Zeichenbereich liegen, der höhere Wert höher steht und die Reihenfolge von links nach rechts erhalten bleibt.
    /// </summary>
    [Fact]
    public void Layout_ScalesValuesIntoBounds()
    {
        var layout = ChartGeometry.Layout(Data(ChartKind.Line, 5m, 6m, 5.5m), Bounds);

        Assert.All(layout.Items, item =>
        {
            Assert.InRange(item.Point.X, layout.Plot.Left, layout.Plot.Right);
            Assert.InRange(item.Point.Y, layout.Plot.Top, layout.Plot.Bottom);
        });
        Assert.True(layout.Items[1].Point.Y < layout.Items[2].Point.Y);
        Assert.True(layout.Items[2].Point.Y < layout.Items[0].Point.Y);
        Assert.True(layout.Items[0].Point.X < layout.Items[1].Point.X);
        Assert.True(layout.Items[1].Point.X < layout.Items[2].Point.X);
        Assert.True(layout.Minimum < 5m);
        Assert.True(layout.Maximum > 6m);
    }

    /// <summary>
    /// Prüft, dass ein einzelner Punkt in der Mitte liegt.
    /// </summary>
    [Fact]
    public void Layout_SinglePoint()
    {
        var layout = ChartGeometry.Layout(Data(ChartKind.Line, 6m), Bounds);

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
        var layout = ChartGeometry.Layout(Data(ChartKind.Line, 6m, 6m, 6m), Bounds);

        Assert.All(layout.Items, item =>
        {
            Assert.True(float.IsFinite(item.Point.Y));
            Assert.InRange(item.Point.Y, layout.Plot.Top, layout.Plot.Bottom);
        });
        Assert.True(layout.Maximum > layout.Minimum);
    }

    /// <summary>
    /// Prüft, dass Balken bei 0 beginnen, der höchste die volle Höhe hat und ein Wert 0 keine Höhe.
    /// </summary>
    [Fact]
    public void Layout_Bars_StartAtZero()
    {
        var layout = ChartGeometry.Layout(Data(ChartKind.Bar, 0m, 50m, 100m), Bounds);

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

        var layout = ChartGeometry.Layout(Data(ChartKind.Line, values), Bounds);

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
        var many = ChartGeometry.Layout(Data(ChartKind.Bar, Enumerable.Range(1, 12).Select(value => (decimal)value).ToArray()), Bounds);
        var few = ChartGeometry.Layout(Data(ChartKind.Bar, 1m, 2m, 3m), Bounds);

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
        var layout = ChartGeometry.Layout(new ChartData(ChartKind.Line, [], "x"), Bounds);

        Assert.Empty(layout.Items);
    }
}

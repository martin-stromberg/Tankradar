using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.ViewModels;
using static Tankradar.Tests.Unit.Unit.Support.TankbookEntryFactory;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Monatskarte („Monatsanalyse“, Kosten und Menge des laufenden Monats), die Kacheln und die Diagrammdaten der Darstellung einer Art.
/// </summary>
public class TankbookKindStatisticsViewModelTests_MonthCard : BaseTest
{
    private static readonly DateTime Today = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Unspecified);

    private static TankbookKindStatisticsViewModel Block(IEnumerable<TankbookEntry> entries)
    {
        return new TankbookKindStatisticsViewModel(Statistics(entries, today: Today).Kinds.Single());
    }

    /// <summary>
    /// Prüft Überschrift, Beschriftung, Kosten und getankte Menge des laufenden Monats.
    /// </summary>
    [Fact]
    public void MonthCard_ShowsCostAndQuantityOfCurrentMonth()
    {
        var block = Block(
        [
            CreateAt(1, new DateTime(2026, 9, 20, 8, 0, 0), 9_500, 30m, 50m),
            CreateAt(2, new DateTime(2026, 10, 1, 8, 0, 0), 10_000, 50m, 80m),
            CreateAt(3, new DateTime(2026, 10, 3, 8, 0, 0), 10_500, 48.5m, 84.2m),
        ]);

        Assert.Equal("Monatsanalyse Oktober 2026", block.MonthAnalysisHeading);
        Assert.Equal("Gesamtkosten Oktober", block.MonthCostLabel);
        Assert.Equal("164,20 €", block.MonthCostText);
        Assert.Equal("98,50 L getankt", block.MonthQuantityText);
    }

    /// <summary>
    /// Prüft, dass ein laufender Monat ohne Vorgang 0 zeigt und Strom „geladen“ heißt.
    /// </summary>
    [Fact]
    public void MonthCard_EmptyCurrentMonth_ShowsZero()
    {
        var block = Block([Create(1, 10_000, 20m, kind: TankbookEntryKind.Electricity)]);

        Assert.Equal("0,00 €", block.MonthCostText);
        Assert.Equal("0,00 kWh geladen", block.MonthQuantityText);
        Assert.Equal("Gesamtkosten Oktober", block.MonthCostLabel);
    }

    /// <summary>
    /// Prüft die Kacheln: Zahlenwert und Einheit getrennt, bei fehlendem Wert „–“ und der Hinweis „noch nicht berechenbar“ als Einheit.
    /// </summary>
    [Fact]
    public void Tiles_SplitValueAndUnit()
    {
        var calculated = Block([Create(1, 10_000, 40m), Create(2, 10_500, 30m)]);
        var missing = Block([Create(1, 10_000, 40m)]);

        Assert.Equal("6,00", calculated.AverageValueText);
        Assert.Equal("L/100 km", calculated.AverageUnitText);
        Assert.Equal("€", calculated.CostPer100KmUnitText);
        Assert.Equal(TankbookStatisticsTexts.NoAverage, missing.AverageValueText);
        Assert.Equal(TankbookStatisticsTexts.NoAverage, missing.CostPer100KmValueText);
        Assert.Equal(TankbookStatisticsTexts.NotCalculable, missing.CostPer100KmUnitText);
    }

    /// <summary>
    /// Prüft, dass der Verbrauchsverlauf höchstens die letzten 6 Werte zeigt und ohne berechnete Werte keine Daten hat (das Diagramm wird dann ausgeblendet).
    /// </summary>
    [Fact]
    public void ConsumptionChart_ShowsLastSixValues_AndHasNoDataWithoutIntervals()
    {
        var many = Block(Enumerable.Range(1, 10).Select(index => Create(index, 10_000 + index * 500, 30m)));
        var none = Block([Create(1, 10_000, 40m)]);

        Assert.Equal(TankbookStatisticsLimits.ConsumptionChartMaxPoints, many.ConsumptionChart.Points.Count);
        Assert.True(many.ConsumptionChart.HasData);
        Assert.False(none.ConsumptionChart.HasData);
        Assert.True(none.CostChart.HasData);
    }
}

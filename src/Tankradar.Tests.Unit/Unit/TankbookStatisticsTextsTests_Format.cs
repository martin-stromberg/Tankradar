using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Formatierungen von <see cref="TankbookStatisticsTexts"/> (de-DE): Kennzahlen, Monate, Achsenbeschriftungen und Einheiten.
/// </summary>
public class TankbookStatisticsTextsTests_Format : BaseTest
{
    /// <summary>
    /// Prüft Durchschnittsverbrauch und Kosten pro 100 km samt Einheiten und Ersatztexten.
    /// </summary>
    [Fact]
    public void Format_AverageAndCost()
    {
        Assert.Equal("6,00 L/100 km", TankbookStatisticsTexts.FormatAverage(6m, TankbookEntryKind.Fuel));
        Assert.Equal("16,50 kWh/100 km", TankbookStatisticsTexts.FormatAverage(16.5m, TankbookEntryKind.Electricity));
        Assert.Equal("–", TankbookStatisticsTexts.FormatAverage(null, TankbookEntryKind.Fuel));
        Assert.Equal("11,15 € / 100 km", TankbookStatisticsTexts.FormatCostPer100Km(11.15m));
        Assert.Equal("noch nicht berechenbar", TankbookStatisticsTexts.FormatCostPer100Km(null));
    }

    /// <summary>
    /// Prüft die Monatszeile, auch für einen leeren Monat.
    /// </summary>
    [Fact]
    public void Format_MonthRow()
    {
        Assert.Equal("Oktober 2026: 74,36 €", TankbookStatisticsTexts.FormatMonthRow(new MonthlyCostSummary(2026, 10, 74.36m, 1)));
        Assert.Equal("Februar 2028: 0,00 €", TankbookStatisticsTexts.FormatMonthRow(new MonthlyCostSummary(2028, 2, 0m, 0)));
        Assert.Equal("März 2026: 1.234,50 €", TankbookStatisticsTexts.FormatMonthRow(new MonthlyCostSummary(2026, 3, 1234.5m, 4)));
    }

    /// <summary>
    /// Prüft die Kurzformen für Achsen und Werte.
    /// </summary>
    [Fact]
    public void Format_ChartLabels()
    {
        Assert.Equal("10/26", TankbookStatisticsTexts.FormatMonthShort(2026, 10));
        Assert.Equal("05.03.", TankbookStatisticsTexts.FormatDateShort(new DateTime(2026, 3, 5, 8, 0, 0)));
        Assert.Equal("74 €", TankbookStatisticsTexts.FormatAmountShort(74.36m));
        Assert.Equal("1.235 €", TankbookStatisticsTexts.FormatAmountShort(1234.5m));
        Assert.Equal("6,00", TankbookStatisticsTexts.FormatValueShort(6m));
    }

    /// <summary>
    /// Prüft die Zusammenfassungen der Diagramme für Barrierefreiheit.
    /// </summary>
    [Fact]
    public void Format_Summaries()
    {
        Assert.Equal(
            "Verlauf, 3 Werte, zuletzt 6,00 L/100 km, niedrigster 5,80, höchster 6,20",
            TankbookStatisticsTexts.FormatConsumptionSummary([6.2m, 5.8m, 6m], TankbookEntryKind.Fuel));
        Assert.Equal(
            "Verlauf, 1 Wert, zuletzt 16,00 kWh/100 km, niedrigster 16,00, höchster 16,00",
            TankbookStatisticsTexts.FormatConsumptionSummary([16m], TankbookEntryKind.Electricity));
        Assert.Equal(
            "Monatskosten, 3 Werte, zuletzt 74,36 €, niedrigster 0,00 €, höchster 120,00 €",
            TankbookStatisticsTexts.FormatCostSummary([120m, 0m, 74.36m]));
        Assert.Equal(TankbookStatisticsTexts.NoChartData, TankbookStatisticsTexts.FormatConsumptionSummary([], TankbookEntryKind.Fuel));
        Assert.Equal(TankbookStatisticsTexts.NoChartData, TankbookStatisticsTexts.FormatCostSummary([]));
    }
}

using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Darstellung der Kennzahlen einer Art (Kraftstoff bzw. Strom): Texte, Hinweise, Diagrammdaten und Monatszeilen.
/// </summary>
public sealed class TankbookKindStatisticsViewModel
{
    /// <summary>
    /// Erstellt die Darstellung.
    /// </summary>
    /// <param name="statistics">Die Kennzahlen der Art.</param>
    public TankbookKindStatisticsViewModel(TankbookKindStatistics statistics)
    {
        ArgumentNullException.ThrowIfNull(statistics);
        var kind = statistics.Kind;
        AutomationKey = kind.ToString();
        Heading = TankbookEntryTexts.GetKindLabel(kind);
        AverageText = TankbookStatisticsTexts.FormatAverage(statistics.AverageConsumption, kind);
        AverageSourceText = TankbookStatisticsTexts.GetSourceText(statistics.AverageSource);
        CostPer100KmText = TankbookStatisticsTexts.FormatCostPer100Km(statistics.CostPer100Km);
        TotalCostText = TankbookEntryTexts.FormatAmount(statistics.TotalCost);
        Notice = statistics.AverageSource == TankbookAverageSource.None ? TankbookStatisticsTexts.TooFewEntriesNotice : null;
        Months = statistics.Months.Select(month => new MonthlyCostRowViewModel(month, AutomationKey)).ToList();

        var trend = statistics.ConsumptionTrend
            .Skip(Math.Max(0, statistics.ConsumptionTrend.Count - TankbookStatisticsLimits.ChartMaxPoints))
            .ToList();
        ConsumptionChart = new ChartData(
            ChartKind.Line,
            trend.Select(point => new ChartPoint(
                TankbookStatisticsTexts.FormatDateShort(point.OccurredAt),
                point.Value,
                TankbookStatisticsTexts.FormatValueShort(point.Value))).ToList(),
            TankbookEntryTexts.GetConsumptionUnit(kind));
        ConsumptionChartSummary = TankbookStatisticsTexts.FormatConsumptionSummary(trend.Select(point => point.Value).ToList(), kind);

        var months = statistics.Months
            .Take(TankbookStatisticsLimits.ChartMaxPoints)
            .Reverse()
            .ToList();
        CostChart = new ChartData(
            ChartKind.Bar,
            months.Select(month => new ChartPoint(
                TankbookStatisticsTexts.FormatMonthShort(month.Year, month.Month),
                month.TotalCost,
                TankbookStatisticsTexts.FormatAmountShort(month.TotalCost))).ToList(),
            "€");
        CostChartSummary = TankbookStatisticsTexts.FormatCostSummary(months.Select(month => month.TotalCost).ToList());
    }

    /// <summary>
    /// Der Schlüssel für AutomationIds: <c>Fuel</c> bzw. <c>Electricity</c>.
    /// </summary>
    public string AutomationKey { get; }

    /// <summary>
    /// Die Überschrift des Blocks: „Kraftstoff“ bzw. „Strom“.
    /// </summary>
    public string Heading { get; }

    /// <summary>
    /// Der Durchschnittsverbrauch, z. B. „6,00 L/100 km“.
    /// </summary>
    public string AverageText { get; }

    /// <summary>
    /// Die Kennzeichnung der Herkunft des Durchschnitts; leer, wenn es keinen gibt.
    /// </summary>
    public string AverageSourceText { get; }

    /// <summary>
    /// Gibt an, ob eine Kennzeichnung der Herkunft angezeigt wird.
    /// </summary>
    public bool HasAverageSource
    {
        get { return AverageSourceText.Length > 0; }
    }

    /// <summary>
    /// Die Kosten pro 100 km oder „noch nicht berechenbar“.
    /// </summary>
    public string CostPer100KmText { get; }

    /// <summary>
    /// Die Gesamtkosten der Art.
    /// </summary>
    public string TotalCostText { get; }

    /// <summary>
    /// Ein Hinweis zur Art, z. B. bei zu wenigen vollen Vorgängen; <see langword="null"/>, wenn keiner anliegt.
    /// </summary>
    public string? Notice { get; }

    /// <summary>
    /// Gibt an, ob ein Hinweis anliegt.
    /// </summary>
    public bool HasNotice
    {
        get { return Notice is { Length: > 0 }; }
    }

    /// <summary>
    /// Die Daten des Verbrauchsverlaufs (Liniendiagramm).
    /// </summary>
    public ChartData ConsumptionChart { get; }

    /// <summary>
    /// Die Textzusammenfassung des Verbrauchsverlaufs.
    /// </summary>
    public string ConsumptionChartSummary { get; }

    /// <summary>
    /// Die Daten des Kostenverlaufs (Balkendiagramm der Monatskosten).
    /// </summary>
    public ChartData CostChart { get; }

    /// <summary>
    /// Die Textzusammenfassung des Kostenverlaufs.
    /// </summary>
    public string CostChartSummary { get; }

    /// <summary>
    /// Die Monatszeilen, neuester Monat zuerst.
    /// </summary>
    public IReadOnlyList<MonthlyCostRowViewModel> Months { get; }
}

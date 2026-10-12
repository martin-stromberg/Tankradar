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
        AverageValueText = TankbookStatisticsTexts.FormatAverageValue(statistics.AverageConsumption);
        AverageUnitText = TankbookEntryTexts.GetConsumptionUnit(kind);
        AverageSourceText = TankbookStatisticsTexts.GetSourceText(statistics.AverageSource);
        CostPer100KmValueText = TankbookStatisticsTexts.FormatCostPer100KmValue(statistics.CostPer100Km);
        CostPer100KmUnitText = statistics.CostPer100Km is null ? TankbookStatisticsTexts.NotCalculable : "€";
        Notice = statistics.AverageSource == TankbookAverageSource.None ? TankbookStatisticsTexts.GetTooFewEntriesNotice(statistics.OffersStartConsumption) : null;
        var currentMonth = statistics.Months.Count > 0 ? statistics.Months[0] : null;
        MonthAnalysisHeading = currentMonth is null ? string.Empty : TankbookStatisticsTexts.FormatMonthAnalysisHeading(currentMonth.Year, currentMonth.Month);
        MonthCostLabel = currentMonth is null ? string.Empty : TankbookStatisticsTexts.FormatMonthCostLabel(currentMonth.Month);
        MonthCostText = TankbookEntryTexts.FormatAmount(currentMonth?.TotalCost ?? 0m);
        MonthQuantityText = TankbookStatisticsTexts.FormatMonthQuantity(currentMonth?.TotalQuantity ?? 0m, kind);
        Months = statistics.Months
            .Take(TankbookStatisticsLimits.MonthListMaxRows)
            .Select(month => new MonthlyCostRowViewModel(month, AutomationKey))
            .ToList();
        MonthsNotice = statistics.Months.Count > TankbookStatisticsLimits.MonthListMaxRows ? TankbookStatisticsTexts.MonthsLimitedNotice : null;

        var trend = statistics.ConsumptionTrend
            .Skip(Math.Max(0, statistics.ConsumptionTrend.Count - TankbookStatisticsLimits.ConsumptionChartMaxPoints))
            .ToList();
        ConsumptionChart = new ChartData(
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
    /// Der Zahlenwert des Durchschnittsverbrauchs ohne Einheit, z. B. „6,00“, oder „–“.
    /// </summary>
    public string AverageValueText { get; }

    /// <summary>
    /// Die Einheit des Durchschnittsverbrauchs: „L/100 km“ bzw. „kWh/100 km“.
    /// </summary>
    public string AverageUnitText { get; }

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
    /// Der Zahlenwert der Kosten pro 100 km ohne Einheit, z. B. „11,15“, oder „–“.
    /// </summary>
    public string CostPer100KmValueText { get; }

    /// <summary>
    /// Die Einheit der Kosten pro 100 km („€“) bzw. „noch nicht berechenbar“, solange es keinen Wert gibt.
    /// </summary>
    public string CostPer100KmUnitText { get; }

    /// <summary>
    /// Die Überschrift der Monatskarte, z. B. „Monatsanalyse Oktober 2026“.
    /// </summary>
    public string MonthAnalysisHeading { get; }

    /// <summary>
    /// Die Beschriftung der Kosten des laufenden Monats, z. B. „Gesamtkosten Oktober“.
    /// </summary>
    public string MonthCostLabel { get; }

    /// <summary>
    /// Die Kosten des laufenden Monats, z. B. „164,20 €“.
    /// </summary>
    public string MonthCostText { get; }

    /// <summary>
    /// Die getankte bzw. geladene Menge des laufenden Monats, z. B. „98,50 L getankt“.
    /// </summary>
    public string MonthQuantityText { get; }

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
    /// Die Daten des Verbrauchsverlaufs (Balkendiagramm der letzten Vorgänge, der letzte hervorgehoben).
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
    /// Hinweis, dass die Monatsliste gekürzt ist; <see langword="null"/>, wenn sie vollständig ist.
    /// </summary>
    public string? MonthsNotice { get; }

    /// <summary>
    /// Gibt an, ob die Monatsliste gekürzt ist.
    /// </summary>
    public bool HasMonthsNotice
    {
        get { return MonthsNotice is { Length: > 0 }; }
    }

    /// <summary>
    /// Die Monatszeilen, neuester Monat zuerst (höchstens <see cref="TankbookStatisticsLimits.MonthListMaxRows"/>).
    /// </summary>
    public IReadOnlyList<MonthlyCostRowViewModel> Months { get; }
}

namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Kennzahlen einer Art (Kraftstoff bzw. Strom) eines Fahrzeugs.
/// </summary>
/// <param name="Kind">Die Art.</param>
/// <param name="AverageConsumption">Der Durchschnittsverbrauch pro 100 km; <see langword="null"/>, wenn es keinen gibt.</param>
/// <param name="AverageSource">Die Herkunft des Durchschnittsverbrauchs.</param>
/// <param name="CostPer100Km">Die Kosten pro 100 km in Euro; <see langword="null"/>, wenn kein berechnetes Intervall vorliegt.</param>
/// <param name="OffersStartConsumption">Gibt an, ob für die Art ein Startverbrauch am Fahrzeug hinterlegt werden könnte: Die Art ist die Startart des Fahrzeugs und es ist noch keiner hinterlegt.</param>
/// <param name="CalculatedIntervals">Die Zahl der berechneten Intervalle.</param>
/// <param name="TotalDistanceKm">Die Summe der Strecken der berechneten Intervalle.</param>
/// <param name="TotalCost">Die Summe der Gesamtbeträge aller Vorgänge der Art in Euro.</param>
/// <param name="Months">Die lückenlose Monatsreihe, neuester Monat zuerst.</param>
/// <param name="ConsumptionTrend">Die Verbrauchswerte der berechneten Vorgänge, ältester zuerst.</param>
/// <param name="IsStartKind">Gibt an, ob die Art die Startart des Fahrzeugs ist (Kraftstoff, bei Elektrofahrzeugen Strom): Für sie genügt ein voller Vorgang, weil der Start-Kilometerstand die Basis liefert.</param>
/// <returns>Der Wert.</returns>
public sealed record TankbookKindStatistics(
    TankbookEntryKind Kind,
    decimal? AverageConsumption,
    TankbookAverageSource AverageSource,
    decimal? CostPer100Km,
    bool OffersStartConsumption,
    int CalculatedIntervals,
    int TotalDistanceKm,
    decimal TotalCost,
    IReadOnlyList<MonthlyCostSummary> Months,
    IReadOnlyList<TrendPoint> ConsumptionTrend,
    bool IsStartKind = false);

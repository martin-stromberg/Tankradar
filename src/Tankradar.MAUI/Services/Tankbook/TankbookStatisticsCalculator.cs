using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// Rechenbaustein der Auswertung. Aggregiert die vom <see cref="ConsumptionCalculator"/> gelieferten Intervalle je Art:
/// Durchschnittsverbrauch = Σ Intervallmengen / Σ Intervallstrecken × 100, Kosten pro 100 km = Σ Intervallkosten / Σ Intervallstrecken × 100
/// (jeweils auf 2 Nachkommastellen gerundet, <see cref="MidpointRounding.AwayFromZero"/>). Monatskosten umfassen alle Vorgänge der Art
/// und bilden eine lückenlose Reihe bis zum aktuellen Monat; Zeitpunkte sind lokale Wanduhrzeit ohne Umrechnung.
/// </summary>
public static class TankbookStatisticsCalculator
{
    private const int ResultDecimals = 2;

    /// <summary>
    /// Berechnet die Auswertung eines Fahrzeugs.
    /// </summary>
    /// <param name="vehicle">Das Fahrzeug (Startverbrauch und Kraftstoffart).</param>
    /// <param name="overviews">Die Vorgänge mit Intervallwerten in beliebiger Reihenfolge.</param>
    /// <param name="today">Das Referenzdatum (lokale Wanduhrzeit); der Monat, bis zu dem die Monatsreihe läuft.</param>
    /// <returns>Die Auswertung mit einem Block je vorhandener Art (Kraftstoff vor Strom).</returns>
    public static TankbookStatistics Calculate(Vehicle vehicle, IEnumerable<TankbookEntryOverview> overviews, DateTime today)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        ArgumentNullException.ThrowIfNull(overviews);
        var ordered = overviews.OrderBy(overview => overview.Entry, TankbookEntryOrder.Ascending).ToList();
        var startKind = vehicle.FuelKind == VehicleFuelKind.Electric ? TankbookEntryKind.Electricity : TankbookEntryKind.Fuel;
        var kinds = new List<TankbookKindStatistics>();
        foreach (var kind in new[] { TankbookEntryKind.Fuel, TankbookEntryKind.Electricity })
        {
            var ofKind = ordered.Where(overview => overview.Entry.Kind == kind).ToList();
            if (ofKind.Count > 0)
            {
                kinds.Add(CalculateKind(kind, ofKind, kind == startKind ? vehicle.StartConsumption : null, today));
            }
        }

        return new TankbookStatistics(vehicle.Id, kinds);
    }

    private static TankbookKindStatistics CalculateKind(TankbookEntryKind kind, List<TankbookEntryOverview> ofKind, decimal? startConsumption, DateTime today)
    {
        var intervals = ofKind
            .Where(overview => overview.ConsumptionState == TankbookConsumptionState.Calculated
                && overview.DistanceKm is > 0
                && overview.IntervalQuantity is not null
                && overview.IntervalCost is not null)
            .ToList();
        decimal? average = null;
        decimal? costPer100Km = null;
        var source = TankbookAverageSource.None;
        var distance = intervals.Sum(overview => overview.DistanceKm!.Value);
        if (intervals.Count > 0 && distance > 0)
        {
            average = Round(intervals.Sum(overview => overview.IntervalQuantity!.Value) / distance * 100m);
            costPer100Km = Round(intervals.Sum(overview => overview.IntervalCost!.Value) / distance * 100m);
            source = TankbookAverageSource.Calculated;
        }
        else if (startConsumption is { } start)
        {
            average = start;
            source = TankbookAverageSource.StartConsumption;
        }

        var trend = intervals
            .Where(overview => overview.Consumption is not null)
            .Select(overview => new TrendPoint(overview.Entry.OccurredAt, overview.Consumption!.Value))
            .ToList();
        return new TankbookKindStatistics(
            kind,
            average,
            source,
            costPer100Km,
            intervals.Count,
            distance,
            ofKind.Sum(overview => overview.Entry.TotalAmount),
            BuildMonths(ofKind, today),
            trend);
    }

    private static List<MonthlyCostSummary> BuildMonths(List<TankbookEntryOverview> ofKind, DateTime today)
    {
        var sums = ofKind
            .GroupBy(overview => (overview.Entry.OccurredAt.Year, overview.Entry.OccurredAt.Month))
            .ToDictionary(group => group.Key, group => (Cost: group.Sum(overview => overview.Entry.TotalAmount), Count: group.Count()));
        var first = ofKind.Min(overview => overview.Entry.OccurredAt);
        var last = ofKind.Max(overview => overview.Entry.OccurredAt);
        var end = last > today ? last : today;
        var firstIndex = first.Year * 12 + first.Month - 1;
        var endIndex = end.Year * 12 + end.Month - 1;
        var months = new List<MonthlyCostSummary>();
        for (var index = endIndex; index >= firstIndex; index--)
        {
            var year = index / 12;
            var month = index % 12 + 1;
            sums.TryGetValue((year, month), out var sum);
            months.Add(new MonthlyCostSummary(year, month, sum.Cost, sum.Count));
        }

        return months;
    }

    private static decimal Round(decimal value)
    {
        return Math.Round(value, ResultDecimals, MidpointRounding.AwayFromZero);
    }
}

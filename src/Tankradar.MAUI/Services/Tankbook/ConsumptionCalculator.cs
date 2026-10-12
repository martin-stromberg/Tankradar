using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// Rechenbaustein für den Verbrauch pro 100 km. Je Art (Kraftstoff bzw. Strom) gibt es eine eigene Kette in chronologischer Ordnung:
/// Ein voller Vorgang mit einem vorherigen vollen Vorgang derselben Art erhält
/// (Menge + Mengen der Teilbetankungen dazwischen) / gefahrene Strecke × 100, auf 2 Nachkommastellen gerundet.
/// Teilbetankungen erhalten keinen eigenen Wert; Teilbetankungen vor dem ersten vollen Vorgang fließen nicht ein.
/// Bei berechnetem Verbrauch werden zusätzlich Menge und Kosten des Intervalls (voller Vorgang plus Teilbetankungen dazwischen) geliefert.
/// </summary>
public static class ConsumptionCalculator
{
    private const int ConsumptionDecimals = 2;

    /// <summary>
    /// Berechnet Strecke und Verbrauch aller Vorgänge eines Fahrzeugs.
    /// </summary>
    /// <param name="entries">Die Vorgänge eines Fahrzeugs in beliebiger Reihenfolge.</param>
    /// <returns>Die Vorgänge in chronologischer Ordnung (ältester zuerst) mit Strecke, Verbrauch und Zustand.</returns>
    public static IReadOnlyList<TankbookEntryOverview> Calculate(IEnumerable<TankbookEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var ordered = entries.OrderBy(entry => entry, TankbookEntryOrder.Ascending).ToList();
        var lastFull = new Dictionary<TankbookEntryKind, TankbookEntry>();
        var partialSums = new Dictionary<TankbookEntryKind, decimal>();
        var partialCosts = new Dictionary<TankbookEntryKind, decimal>();
        var result = new List<TankbookEntryOverview>(ordered.Count);
        foreach (var entry in ordered)
        {
            if (entry.IsPartial)
            {
                if (lastFull.ContainsKey(entry.Kind))
                {
                    partialSums[entry.Kind] = partialSums.GetValueOrDefault(entry.Kind) + entry.Quantity;
                    partialCosts[entry.Kind] = partialCosts.GetValueOrDefault(entry.Kind) + entry.TotalAmount;
                }

                result.Add(new TankbookEntryOverview(entry, null, null, TankbookConsumptionState.Partial));
                continue;
            }

            if (!lastFull.TryGetValue(entry.Kind, out var previous))
            {
                result.Add(new TankbookEntryOverview(entry, null, null, TankbookConsumptionState.NoPreviousFullEntry));
            }
            else
            {
                var distance = entry.OdometerKm - previous.OdometerKm;
                if (distance <= 0)
                {
                    result.Add(new TankbookEntryOverview(entry, distance, null, TankbookConsumptionState.NoDistance));
                }
                else
                {
                    var quantity = entry.Quantity + partialSums.GetValueOrDefault(entry.Kind);
                    var consumption = Math.Round(quantity / distance * 100m, ConsumptionDecimals, MidpointRounding.AwayFromZero);
                    var cost = entry.TotalAmount + partialCosts.GetValueOrDefault(entry.Kind);
                    result.Add(new TankbookEntryOverview(entry, distance, consumption, TankbookConsumptionState.Calculated, quantity, cost));
                }
            }

            lastFull[entry.Kind] = entry;
            partialSums[entry.Kind] = 0m;
            partialCosts[entry.Kind] = 0m;
        }

        return result;
    }
}

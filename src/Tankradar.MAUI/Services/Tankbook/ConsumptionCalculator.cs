using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// Rechenbaustein für den Verbrauch pro 100 km. Je Art (Kraftstoff bzw. Strom) gibt es eine eigene Kette in chronologischer Ordnung:
/// Ein voller Vorgang mit einem vorherigen vollen Vorgang derselben Art erhält
/// (Menge + Mengen der Teilbetankungen dazwischen) / gefahrene Strecke × 100, auf 2 Nachkommastellen gerundet.
/// Teilbetankungen erhalten keinen eigenen Wert; Teilbetankungen vor dem ersten vollen Vorgang fließen nur mit Start-Kilometerstand (siehe unten) ein.
/// Bei berechnetem Verbrauch werden zusätzlich Menge und Kosten des Intervalls (voller Vorgang plus Teilbetankungen dazwischen) geliefert.
/// Ist der Start-Kilometerstand des Fahrzeugs bekannt, dient er für die Startart (Kraftstoff, bei Elektrofahrzeugen Strom) als Ausgangsbasis des ersten
/// vollen Vorgangs (Annahme: Der Tank war beim Start voll); Teilbetankungen davor zählen dann mit, und das Intervall ist als „ab Startstand“ gekennzeichnet.
/// </summary>
public static class ConsumptionCalculator
{
    private const int ConsumptionDecimals = 2;

    /// <summary>
    /// Berechnet Strecke und Verbrauch aller Vorgänge eines Fahrzeugs.
    /// </summary>
    /// <param name="entries">Die Vorgänge eines Fahrzeugs in beliebiger Reihenfolge.</param>
    /// <param name="startOdometerKm">Der Start-Kilometerstand des Fahrzeugs als Ausgangsbasis des ersten vollen Vorgangs der Startart; <see langword="null"/> ohne diese Basis.</param>
    /// <param name="startKind">Die Art, für die der Start-Kilometerstand gilt (bei Hybridfahrzeugen nur Kraftstoff).</param>
    /// <returns>Die Vorgänge in chronologischer Ordnung (ältester zuerst) mit Strecke, Verbrauch und Zustand.</returns>
    public static IReadOnlyList<TankbookEntryOverview> Calculate(IEnumerable<TankbookEntry> entries, int? startOdometerKm = null, TankbookEntryKind startKind = TankbookEntryKind.Fuel)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var ordered = entries.OrderBy(entry => entry, TankbookEntryOrder.Ascending).ToList();
        var lastFull = new Dictionary<TankbookEntryKind, int>();
        var fromStart = new HashSet<TankbookEntryKind>();
        if (startOdometerKm is { } start)
        {
            lastFull[startKind] = start;
            fromStart.Add(startKind);
        }

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

            if (!lastFull.TryGetValue(entry.Kind, out var previousOdometer))
            {
                result.Add(new TankbookEntryOverview(entry, null, null, TankbookConsumptionState.NoPreviousFullEntry));
            }
            else
            {
                var isFromStart = fromStart.Contains(entry.Kind);
                var distance = entry.OdometerKm - previousOdometer;
                if (distance <= 0)
                {
                    result.Add(new TankbookEntryOverview(entry, distance, null, TankbookConsumptionState.NoDistance, IsFromStart: isFromStart));
                }
                else
                {
                    var quantity = entry.Quantity + partialSums.GetValueOrDefault(entry.Kind);
                    var consumption = Math.Round(quantity / distance * 100m, ConsumptionDecimals, MidpointRounding.AwayFromZero);
                    var cost = entry.TotalAmount + partialCosts.GetValueOrDefault(entry.Kind);
                    result.Add(new TankbookEntryOverview(entry, distance, consumption, TankbookConsumptionState.Calculated, quantity, cost, isFromStart));
                }
            }

            lastFull[entry.Kind] = entry.OdometerKm;
            fromStart.Remove(entry.Kind);
            partialSums[entry.Kind] = 0m;
            partialCosts[entry.Kind] = 0m;
        }

        return result;
    }
}

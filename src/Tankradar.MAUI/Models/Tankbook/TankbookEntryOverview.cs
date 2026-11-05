namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Ein Tank- oder Ladevorgang mit der beim Lesen berechneten Strecke und dem Verbrauch pro 100 km.
/// </summary>
/// <param name="Entry">Der Vorgang.</param>
/// <param name="DistanceKm">Die seit dem vorherigen vollen Vorgang derselben Art gefahrene Strecke; <see langword="null"/>, wenn es keinen gibt oder der Vorgang eine Teilbetankung ist.</param>
/// <param name="Consumption">Der Verbrauch pro 100 km (L bzw. kWh); <see langword="null"/>, wenn keiner berechnet werden kann.</param>
/// <param name="ConsumptionState">Der Zustand der Berechnung.</param>
/// <param name="IntervalQuantity">Die Menge des Intervalls (voller Vorgang plus dazwischenliegende Teilbetankungen); nur bei berechnetem Verbrauch belegt.</param>
/// <param name="IntervalCost">Die Kosten des Intervalls in Euro (voller Vorgang plus dazwischenliegende Teilbetankungen); nur bei berechnetem Verbrauch belegt.</param>
/// <param name="IsFromStart">Gibt an, ob als Ausgangsbasis der Start-Kilometerstand des Fahrzeugs diente (erster voller Vorgang der Startart; Annahme: Tank beim Start voll).</param>
/// <returns>Der Wert.</returns>
public sealed record TankbookEntryOverview(
    TankbookEntry Entry,
    int? DistanceKm,
    decimal? Consumption,
    TankbookConsumptionState ConsumptionState,
    decimal? IntervalQuantity = null,
    decimal? IntervalCost = null,
    bool IsFromStart = false);

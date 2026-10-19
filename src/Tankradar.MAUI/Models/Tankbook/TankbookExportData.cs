namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Aufbereitete Daten eines Exports: Fahrzeug, die Vorgänge des Zeitraums (ältester zuerst) und die daraus berechneten Kennzahlen.
/// </summary>
/// <param name="Vehicle">Das Fahrzeug.</param>
/// <param name="Entries">Die Vorgänge des Zeitraums mit Verbrauch, ältester zuerst.</param>
/// <param name="Statistics">Die Kennzahlen der Vorgänge des Zeitraums.</param>
/// <param name="From">Der erste Tag des Zeitraums; <see langword="null"/> ohne untere Grenze.</param>
/// <param name="To">Der letzte Tag des Zeitraums; <see langword="null"/> ohne obere Grenze.</param>
/// <param name="CreatedAt">Der Zeitpunkt der Erstellung (lokale Wanduhrzeit).</param>
/// <returns>Der Wert.</returns>
public sealed record TankbookExportData(
    Vehicle Vehicle,
    IReadOnlyList<TankbookEntryOverview> Entries,
    TankbookStatistics Statistics,
    DateTime? From,
    DateTime? To,
    DateTime CreatedAt);

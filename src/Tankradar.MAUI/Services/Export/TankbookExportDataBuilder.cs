using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.MAUI.Services.Export;

/// <summary>
/// Bereitet die Daten eines Exports auf: filtert die Vorgänge auf den Zeitraum und berechnet die Kennzahlen mit den vorhandenen Rechnern
/// (<see cref="TankbookStatisticsCalculator"/>, ADR 0010). Die Verbrauchswerte stammen aus der vollständigen Kette der Vorgänge; ein Vorgang am Anfang des
/// Zeitraums behält daher seine Strecke zum Vorgänger davor.
/// </summary>
public static class TankbookExportDataBuilder
{
    /// <summary>
    /// Baut die Exportdaten.
    /// </summary>
    /// <param name="vehicle">Das Fahrzeug.</param>
    /// <param name="overviews">Alle Vorgänge des Fahrzeugs mit Verbrauch, in beliebiger Reihenfolge.</param>
    /// <param name="from">Der erste Tag des Zeitraums (ab 00:00); <see langword="null"/> ohne untere Grenze.</param>
    /// <param name="to">Der letzte Tag des Zeitraums (bis 23:59:59,999); <see langword="null"/> ohne obere Grenze.</param>
    /// <param name="now">Der aktuelle Zeitpunkt (lokale Wanduhrzeit).</param>
    /// <returns>Die Exportdaten mit den Vorgängen des Zeitraums, ältester zuerst.</returns>
    public static TankbookExportData Build(Vehicle vehicle, IEnumerable<TankbookEntryOverview> overviews, DateTime? from, DateTime? to, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        ArgumentNullException.ThrowIfNull(overviews);
        var lower = from?.Date;
        var upperExclusive = to?.Date.AddDays(1);
        var filtered = overviews
            .Where(overview => (lower is null || overview.Entry.OccurredAt >= lower) && (upperExclusive is null || overview.Entry.OccurredAt < upperExclusive))
            .OrderBy(overview => overview.Entry, TankbookEntryOrder.Ascending)
            .ToList();
        var statisticsToday = to?.Date ?? now;
        var statistics = TankbookStatisticsCalculator.Calculate(vehicle, filtered, statisticsToday);
        return new TankbookExportData(vehicle, filtered, statistics, lower, to?.Date, now);
    }
}

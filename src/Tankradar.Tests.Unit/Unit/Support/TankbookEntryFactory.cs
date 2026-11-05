using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Erzeugt Vorgänge für die Tests der reinen Rechenbausteine.
/// </summary>
public static class TankbookEntryFactory
{
    /// <summary>
    /// Der Ausgangszeitpunkt der erzeugten Vorgänge.
    /// </summary>
    public static DateTime Start
    {
        get { return new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Unspecified); }
    }

    /// <summary>
    /// Erzeugt einen Vorgang.
    /// </summary>
    /// <param name="id">Die Kennung; bestimmt auch den Zeitpunkt (Start + <paramref name="id"/> Tage).</param>
    /// <param name="odometerKm">Der Kilometerstand.</param>
    /// <param name="quantity">Die Menge.</param>
    /// <param name="isPartial">Gibt an, ob es eine Teilbetankung ist.</param>
    /// <param name="kind">Die Art.</param>
    /// <returns>Der Vorgang.</returns>
    public static TankbookEntry Create(long id, int odometerKm, decimal quantity, bool isPartial = false, TankbookEntryKind kind = TankbookEntryKind.Fuel)
    {
        return new TankbookEntry(id, 1, Start.AddDays(id), kind, quantity, 1.5m, quantity * 1.5m, odometerKm, isPartial, "Alpha Tankstelle", null, null, null);
    }

    /// <summary>
    /// Erzeugt einen Vorgang mit frei wählbarem Zeitpunkt und Gesamtbetrag.
    /// </summary>
    /// <param name="id">Die Kennung.</param>
    /// <param name="occurredAt">Der Zeitpunkt (lokale Wanduhrzeit).</param>
    /// <param name="odometerKm">Der Kilometerstand.</param>
    /// <param name="quantity">Die Menge.</param>
    /// <param name="totalAmount">Der Gesamtbetrag.</param>
    /// <param name="isPartial">Gibt an, ob es eine Teilbetankung ist.</param>
    /// <param name="kind">Die Art.</param>
    /// <returns>Der Vorgang.</returns>
    public static TankbookEntry CreateAt(long id, DateTime occurredAt, int odometerKm, decimal quantity, decimal totalAmount, bool isPartial = false, TankbookEntryKind kind = TankbookEntryKind.Fuel)
    {
        return new TankbookEntry(id, 1, occurredAt, kind, quantity, 1.5m, totalAmount, odometerKm, isPartial, "Alpha Tankstelle", null, null, null);
    }

    /// <summary>
    /// Berechnet die Auswertung eines Fahrzeugs aus Vorgängen (Verbrauchskette wie im Dienst).
    /// </summary>
    /// <param name="entries">Die Vorgänge.</param>
    /// <param name="fuelKind">Die Kraftstoffart des Fahrzeugs.</param>
    /// <param name="startConsumption">Der optionale Startverbrauch.</param>
    /// <param name="today">Das Referenzdatum; Standard ist der 05.10.2026.</param>
    /// <param name="fromStart">Gibt an, ob der Start-Kilometerstand (10.000 km) die Basis des ersten vollen Vorgangs der Startart ist.</param>
    /// <returns>Die Auswertung.</returns>
    public static TankbookStatistics Statistics(IEnumerable<TankbookEntry> entries, VehicleFuelKind fuelKind = VehicleFuelKind.Diesel, decimal? startConsumption = null, DateTime? today = null, bool fromStart = false)
    {
        var vehicle = new Vehicle(1, "Golf", fuelKind, 10_000, startConsumption);
        var startKind = fuelKind == VehicleFuelKind.Electric ? TankbookEntryKind.Electricity : TankbookEntryKind.Fuel;
        var overviews = ConsumptionCalculator.Calculate(entries, fromStart ? vehicle.StartOdometerKm : null, startKind).Reverse().ToList();
        return TankbookStatisticsCalculator.Calculate(vehicle, overviews, today ?? new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Unspecified));
    }
}

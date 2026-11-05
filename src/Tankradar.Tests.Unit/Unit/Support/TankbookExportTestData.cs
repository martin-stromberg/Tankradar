using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Export;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Erzeugt Exportdaten für die Tests der Schreiber und des Datenaufbaus aus Vorgängen.
/// </summary>
public static class TankbookExportTestData
{
    /// <summary>
    /// Der Zeitpunkt der Erstellung in den Tests: 19.10.2026, 09:30.
    /// </summary>
    public static DateTime Now
    {
        get { return new DateTime(2026, 10, 19, 9, 30, 0, DateTimeKind.Unspecified); }
    }

    /// <summary>
    /// Erzeugt ein Fahrzeug (Start-Kilometerstand 10.000 km).
    /// </summary>
    /// <param name="name">Der Name.</param>
    /// <param name="fuelKind">Die Kraftstoffart.</param>
    /// <returns>Das Fahrzeug.</returns>
    public static Vehicle CreateVehicle(string name = "Golf", VehicleFuelKind fuelKind = VehicleFuelKind.Diesel)
    {
        return new Vehicle(1, name, fuelKind, 10_000, null);
    }

    /// <summary>
    /// Erzeugt einen Vorgang mit frei wählbaren Textfeldern.
    /// </summary>
    /// <param name="id">Die Kennung.</param>
    /// <param name="occurredAt">Der Zeitpunkt.</param>
    /// <param name="odometerKm">Der Kilometerstand.</param>
    /// <param name="quantity">Die Menge.</param>
    /// <param name="totalAmount">Der Gesamtbetrag.</param>
    /// <param name="station">Der Name der Tankstelle.</param>
    /// <param name="note">Die Notiz.</param>
    /// <param name="isPartial">Gibt an, ob es eine Teilbetankung ist.</param>
    /// <param name="kind">Die Art.</param>
    /// <returns>Der Vorgang.</returns>
    public static TankbookEntry CreateEntry(
        long id,
        DateTime occurredAt,
        int odometerKm,
        decimal quantity = 40m,
        decimal totalAmount = 60m,
        string station = "Alpha Tankstelle",
        string? note = null,
        bool isPartial = false,
        TankbookEntryKind kind = TankbookEntryKind.Fuel)
    {
        return new TankbookEntry(id, 1, occurredAt, kind, quantity, quantity == 0 ? 0 : Math.Round(totalAmount / quantity, 3), totalAmount, odometerKm, isPartial, station, null, note, null);
    }

    /// <summary>
    /// Berechnet den Verbrauch der Vorgänge mit der vollständigen Kette und baut die Exportdaten.
    /// </summary>
    /// <param name="entries">Die Vorgänge.</param>
    /// <param name="from">Der erste Tag.</param>
    /// <param name="to">Der letzte Tag.</param>
    /// <param name="vehicle">Das Fahrzeug; Standard ist ein Dieselfahrzeug.</param>
    /// <returns>Die Exportdaten.</returns>
    public static TankbookExportData Build(IEnumerable<TankbookEntry> entries, DateTime? from = null, DateTime? to = null, Vehicle? vehicle = null)
    {
        vehicle ??= CreateVehicle();
        var startKind = vehicle.FuelKind == VehicleFuelKind.Electric ? TankbookEntryKind.Electricity : TankbookEntryKind.Fuel;
        var overviews = ConsumptionCalculator.Calculate(entries, vehicle.StartOdometerKm, startKind);
        return TankbookExportDataBuilder.Build(vehicle, overviews, from, to, Now);
    }

    /// <summary>
    /// Erzeugt eine Folge voller Vorgänge im Abstand von zwei Wochen mit 500 km Strecke und 40 Litern.
    /// </summary>
    /// <param name="count">Die Zahl der Vorgänge.</param>
    /// <returns>Die Vorgänge, ältester zuerst.</returns>
    public static List<TankbookEntry> CreateSeries(int count)
    {
        return Enumerable.Range(1, count)
            .Select(index => CreateEntry(index, new DateTime(2026, 1, 1, 8, 0, 0).AddDays(index * 2), 10_000 + (index * 500), 40m, 60m, $"Station {index}"))
            .ToList();
    }
}

using Tankradar.MAUI.Models.Tankbook;

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
}

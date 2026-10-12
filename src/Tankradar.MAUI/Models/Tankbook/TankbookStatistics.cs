namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Die Auswertung der Tank- und Ladevorgänge eines Fahrzeugs.
/// </summary>
/// <param name="VehicleId">Die Kennung des Fahrzeugs.</param>
/// <param name="Kinds">Je vorhandener Art ein Block (Kraftstoff vor Strom); leer ohne Vorgänge.</param>
/// <returns>Der Wert.</returns>
public sealed record TankbookStatistics(long VehicleId, IReadOnlyList<TankbookKindStatistics> Kinds)
{
    /// <summary>
    /// Gibt an, ob es Vorgänge gibt.
    /// </summary>
    public bool HasEntries
    {
        get { return Kinds.Count > 0; }
    }
}

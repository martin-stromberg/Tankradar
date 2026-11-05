namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Art eines Tank- oder Ladevorgangs. Die Namen der Werte sind Persistenzvertrag (Spalte <c>TankbookEntries.Kind</c>).
/// </summary>
public enum TankbookEntryKind
{
    /// <summary>
    /// Kraftstoff; Menge in Litern, Verbrauch in L/100 km.
    /// </summary>
    Fuel,

    /// <summary>
    /// Strom; Menge in kWh, Verbrauch in kWh/100 km.
    /// </summary>
    Electricity,
}

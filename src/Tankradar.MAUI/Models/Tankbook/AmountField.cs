namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Einer der drei sich ergänzenden Werte eines Tank- oder Ladevorgangs.
/// </summary>
public enum AmountField
{
    /// <summary>
    /// Die Menge (L bzw. kWh).
    /// </summary>
    Quantity,

    /// <summary>
    /// Der Preis pro Einheit (€/L bzw. €/kWh).
    /// </summary>
    UnitPrice,

    /// <summary>
    /// Der Gesamtbetrag (€).
    /// </summary>
    TotalAmount,
}

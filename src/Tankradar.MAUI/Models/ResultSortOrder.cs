namespace Tankradar.MAUI.Models;

/// <summary>
/// Standardsortierung der Suchergebnisse. Die Namen der Werte sind ein Persistenzvertrag.
/// </summary>
public enum ResultSortOrder
{
    /// <summary>
    /// Sortierung nach Preis.
    /// </summary>
    Price,

    /// <summary>
    /// Sortierung nach Entfernung.
    /// </summary>
    Distance,

    /// <summary>
    /// Sortierung nach Name.
    /// </summary>
    Name,
}

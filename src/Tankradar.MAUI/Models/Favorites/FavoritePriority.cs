namespace Tankradar.MAUI.Models.Favorites;

/// <summary>
/// Optionale Priorität eines Favoriten innerhalb einer Gruppe; die Liste der Gruppe ist von hoch nach niedrig geordnet. Der Name des Werts wird gespeichert.
/// </summary>
public enum FavoritePriority
{
    /// <summary>
    /// Keine Priorität gesetzt.
    /// </summary>
    None,

    /// <summary>
    /// Niedrige Priorität.
    /// </summary>
    Low,

    /// <summary>
    /// Mittlere Priorität.
    /// </summary>
    Medium,

    /// <summary>
    /// Hohe Priorität.
    /// </summary>
    High,
}

namespace Tankradar.MAUI.Data;

/// <summary>
/// Tabelle <c>FavoriteEntries</c>: die Zuordnung einer Tankstelle zu einer Favoritengruppe mit optionaler Notiz und Priorität. Eine Tankstelle darf mehreren Gruppen angehören,
/// je Gruppe aber nur einmal.
/// </summary>
public class FavoriteEntryEntity
{
    /// <summary>
    /// Primärschlüssel.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Fremdschlüssel auf <see cref="FavoriteGroupEntity"/>; beim Löschen der Gruppe entfallen ihre Einträge.
    /// </summary>
    public long GroupId { get; set; }

    /// <summary>
    /// Fremdschlüssel auf <see cref="StationEntity"/>.
    /// </summary>
    public string StationId { get; set; } = string.Empty;

    /// <summary>
    /// Optionale Notiz.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Name des <see cref="Models.Favorites.FavoritePriority"/>-Werts.
    /// </summary>
    public string Priority { get; set; } = string.Empty;

    /// <summary>
    /// Zeitpunkt (UTC) des Hinzufügens.
    /// </summary>
    public DateTime AddedUtc { get; set; }

    /// <summary>
    /// Die Gruppe.
    /// </summary>
    public FavoriteGroupEntity? Group { get; set; }

    /// <summary>
    /// Die Tankstelle.
    /// </summary>
    public StationEntity? Station { get; set; }
}

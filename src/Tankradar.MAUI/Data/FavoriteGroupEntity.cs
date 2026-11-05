namespace Tankradar.MAUI.Data;

/// <summary>
/// Tabelle <c>FavoriteGroups</c>: vom Nutzer benannte Gruppen favorisierter Tankstellen. Der Name ist ohne Beachtung der Groß- und Kleinschreibung eindeutig.
/// </summary>
public class FavoriteGroupEntity
{
    /// <summary>
    /// Primärschlüssel.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Name der Gruppe.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optionale kurze Beschreibung.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Zeitpunkt (UTC) des Anlegens.
    /// </summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// Die Tankstellen der Gruppe.
    /// </summary>
    public List<FavoriteEntryEntity> Entries { get; set; } = [];
}

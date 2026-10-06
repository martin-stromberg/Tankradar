namespace Tankradar.MAUI.Models.Favorites;

/// <summary>
/// Längengrenzen der Texte von Favoritengruppen und Favoriten.
/// </summary>
public static class FavoriteLimits
{
    /// <summary>
    /// Höchstzahl der Zeichen eines Gruppennamens.
    /// </summary>
    public const int MaxNameLength = 40;

    /// <summary>
    /// Höchstzahl der Zeichen einer Gruppenbeschreibung.
    /// </summary>
    public const int MaxDescriptionLength = 200;

    /// <summary>
    /// Höchstzahl der Zeichen einer Notiz.
    /// </summary>
    public const int MaxNoteLength = 500;
}

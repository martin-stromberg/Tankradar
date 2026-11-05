using Tankradar.MAUI.Models.Search;

namespace Tankradar.MAUI.Models.Favorites;

/// <summary>
/// Eine Tankstelle auf der Startseite (in einer Favoritengruppe oder im Bereich „In der Nähe“).
/// </summary>
/// <param name="Item">Die aufbereitete Tankstellenzeile mit Preiszeilen, Alter, Hinweisen und Entfernung.</param>
/// <param name="Note">Die Notiz des Favoriten; <see langword="null"/>, wenn keine hinterlegt ist (immer bei „In der Nähe“).</param>
/// <param name="Priority">Die Priorität des Favoriten (bei „In der Nähe“ <see cref="FavoritePriority.None"/>).</param>
/// <returns>Der Wert.</returns>
public sealed record HomeStation(StationListItem Item, string? Note, FavoritePriority Priority)
{
    /// <summary>
    /// Gibt an, ob mindestens ein Preis der gewählten Sorten bekannt ist.
    /// </summary>
    public bool HasPrices => Item.PriceLines.Count > 0;

    /// <summary>
    /// Gibt an, ob eine Notiz hinterlegt ist.
    /// </summary>
    public bool HasNote => Note is { Length: > 0 };
}

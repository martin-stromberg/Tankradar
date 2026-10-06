namespace Tankradar.MAUI.Models.Favorites;

/// <summary>
/// Eine Tankstelle in einer Favoritengruppe mit optionaler Notiz und Priorität.
/// </summary>
/// <param name="GroupId">Die Kennung der Gruppe.</param>
/// <param name="StationId">Die Kennung der Tankstelle.</param>
/// <param name="StationName">Der Name der Tankstelle.</param>
/// <param name="AddressText">Die Adresse der Tankstelle in einer Zeile; leer, wenn die Quelle keine liefert.</param>
/// <param name="Note">Die optionale Notiz; <see langword="null"/>, wenn keine hinterlegt ist.</param>
/// <param name="Priority">Die Priorität.</param>
/// <returns>Der Wert.</returns>
public sealed record FavoriteEntry(long GroupId, string StationId, string StationName, string AddressText, string? Note, FavoritePriority Priority);

namespace Tankradar.MAUI.Models.Favorites;

/// <summary>
/// Eine Favoritengruppe mit Name, optionaler Beschreibung und Zahl der zugeordneten Tankstellen.
/// </summary>
/// <param name="Id">Die Kennung der Gruppe.</param>
/// <param name="Name">Der Name der Gruppe.</param>
/// <param name="Description">Die optionale Beschreibung; <see langword="null"/>, wenn keine hinterlegt ist.</param>
/// <param name="StationCount">Die Zahl der Tankstellen in der Gruppe.</param>
/// <returns>Der Wert.</returns>
public sealed record FavoriteGroup(long Id, string Name, string? Description, int StationCount);

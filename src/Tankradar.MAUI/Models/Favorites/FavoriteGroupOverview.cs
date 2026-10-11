namespace Tankradar.MAUI.Models.Favorites;

/// <summary>
/// Eine Favoritengruppe mit ihren Einträgen für die Startseite.
/// </summary>
/// <param name="Group">Die Gruppe.</param>
/// <param name="Entries">Die Einträge nach Priorität (hoch zuerst), dann nach Namen; leer bei einer Gruppe ohne Tankstellen.</param>
/// <returns>Der Wert.</returns>
public sealed record FavoriteGroupOverview(FavoriteGroup Group, IReadOnlyList<FavoriteEntry> Entries);

namespace Tankradar.MAUI.Models.Favorites;

/// <summary>
/// Eine Favoritengruppe auf der Startseite mit ihren Tankstellen.
/// </summary>
/// <param name="Id">Die Kennung der Gruppe.</param>
/// <param name="Name">Der Name der Gruppe.</param>
/// <param name="Description">Die optionale Beschreibung; <see langword="null"/>, wenn keine hinterlegt ist.</param>
/// <param name="CountText">Die Zahl der Tankstellen als Text („3 Tankstellen“).</param>
/// <param name="Stations">Die Tankstellen in der Reihenfolge der Gruppe.</param>
/// <returns>Der Wert.</returns>
public sealed record HomeGroup(long Id, string Name, string? Description, string CountText, IReadOnlyList<HomeStation> Stations)
{
    /// <summary>
    /// Gibt an, ob eine Beschreibung hinterlegt ist.
    /// </summary>
    public bool HasDescription => Description is { Length: > 0 };

    /// <summary>
    /// Gibt an, ob die Gruppe keine Tankstellen enthält.
    /// </summary>
    public bool IsEmpty => Stations.Count == 0;
}

using System.Windows.Input;
using Tankradar.MAUI.Models.Favorites;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Eine Favoritengruppe in der Gruppenübersicht mit Name, Beschreibung und Zahl der Tankstellen; Antippen öffnet die Gruppenansicht.
/// </summary>
public sealed class FavoriteGroupItemViewModel : BaseViewModel
{
    /// <summary>
    /// Erstellt den Eintrag.
    /// </summary>
    /// <param name="group">Die Gruppe.</param>
    /// <param name="open">Wird aufgerufen, wenn der Anwender die Gruppe öffnet.</param>
    public FavoriteGroupItemViewModel(FavoriteGroup group, Action<FavoriteGroupItemViewModel> open)
    {
        ArgumentNullException.ThrowIfNull(group);
        GroupId = group.Id;
        Name = group.Name;
        Description = group.Description ?? string.Empty;
        CountText = FavoritesTexts.FormatStationCount(group.StationCount);
        OpenCommand = new Command(() => open(this));
    }

    /// <summary>
    /// Die Kennung der Gruppe.
    /// </summary>
    public long GroupId { get; }

    /// <summary>
    /// Der Name der Gruppe.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Die Beschreibung; leer, wenn keine hinterlegt ist.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gibt an, ob eine Beschreibung hinterlegt ist.
    /// </summary>
    public bool HasDescription => Description.Length > 0;

    /// <summary>
    /// Die Zahl der Tankstellen als Text („3 Tankstellen“).
    /// </summary>
    public string CountText { get; }

    /// <summary>
    /// Befehl, der die Gruppenansicht öffnet.
    /// </summary>
    public ICommand OpenCommand { get; }
}

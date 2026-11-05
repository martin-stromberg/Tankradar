using System.Windows.Input;
using Tankradar.MAUI.Models.Favorites;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Eine Favoritengruppe der Startseite: Kopf mit Name, Beschreibung und Anzahl, einklappbar, mit den Tankstellenkacheln der Gruppe.
/// </summary>
public sealed class HomeGroupViewModel : BaseViewModel
{
    private readonly Action<HomeGroupViewModel> _expandedChanged;
    private bool _isExpanded;

    /// <summary>
    /// Erstellt die Gruppenkarte.
    /// </summary>
    /// <param name="group">Die aufbereitete Gruppe.</param>
    /// <param name="stations">Die Kacheln der Tankstellen in der Reihenfolge der Gruppe.</param>
    /// <param name="isExpanded">Gibt an, ob die Gruppe aufgeklappt ist.</param>
    /// <param name="open">Wird aufgerufen, wenn der Anwender die Gruppenansicht öffnet.</param>
    /// <param name="expandedChanged">Wird aufgerufen, wenn der Anwender die Gruppe ein- oder ausklappt.</param>
    public HomeGroupViewModel(
        HomeGroup group,
        IReadOnlyList<HomeStationViewModel> stations,
        bool isExpanded,
        Action<HomeGroupViewModel> open,
        Action<HomeGroupViewModel> expandedChanged)
    {
        ArgumentNullException.ThrowIfNull(group);
        Group = group;
        Stations = stations;
        _isExpanded = isExpanded;
        _expandedChanged = expandedChanged;
        ToggleCommand = new Command(Toggle);
        OpenCommand = new Command(() => open(this));
    }

    /// <summary>
    /// Die aufbereitete Gruppe.
    /// </summary>
    public HomeGroup Group { get; }

    /// <summary>
    /// Die Kennung der Gruppe.
    /// </summary>
    public long GroupId => Group.Id;

    /// <summary>
    /// Der Name der Gruppe.
    /// </summary>
    public string Name => Group.Name;

    /// <summary>
    /// Die Beschreibung; leer, wenn keine hinterlegt ist.
    /// </summary>
    public string Description => Group.Description ?? string.Empty;

    /// <summary>
    /// Gibt an, ob eine Beschreibung hinterlegt ist.
    /// </summary>
    public bool HasDescription => Group.HasDescription;

    /// <summary>
    /// Die Zahl der Tankstellen als Text („3 Tankstellen“).
    /// </summary>
    public string CountText => Group.CountText;

    /// <summary>
    /// Die Kacheln der Tankstellen in der Reihenfolge der Gruppe.
    /// </summary>
    public IReadOnlyList<HomeStationViewModel> Stations { get; }

    /// <summary>
    /// Gibt an, ob die Gruppe aufgeklappt ist (Standard).
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        private set
        {
            if (SetProperty(ref _isExpanded, value))
            {
                OnPropertyChanged(nameof(ShowStations));
                OnPropertyChanged(nameof(ShowEmptyNote));
                OnPropertyChanged(nameof(ToggleGlyph));
                OnPropertyChanged(nameof(ToggleDescription));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob die Tankstellenkacheln angezeigt werden (aufgeklappt und mindestens eine Tankstelle).
    /// </summary>
    public bool ShowStations => IsExpanded && !Group.IsEmpty;

    /// <summary>
    /// Gibt an, ob der Hinweis „Noch keine Tankstellen“ angezeigt wird (aufgeklappt und leer).
    /// </summary>
    public bool ShowEmptyNote => IsExpanded && Group.IsEmpty;

    /// <summary>
    /// Der Hinweis „Noch keine Tankstellen“.
    /// </summary>
    public string EmptyNoteText => FavoritesTexts.GroupNoStations;

    /// <summary>
    /// Das Zeichen des Umschalters.
    /// </summary>
    public string ToggleGlyph => IsExpanded ? FavoritesTexts.ToggleExpandedGlyph : FavoritesTexts.ToggleCollapsedGlyph;

    /// <summary>
    /// Die Beschreibung des Umschalters für die Sprachausgabe (Gruppenname und Zustand).
    /// </summary>
    public string ToggleDescription
    {
        get { return FavoritesTexts.ToggleGroupDescription(Name, IsExpanded); }
    }

    /// <summary>
    /// Gibt an, ob mindestens eine Tankstelle der Gruppe einen veralteten Preis zeigt.
    /// </summary>
    public bool HasStalePrice
    {
        get { return Stations.Any(station => station.HasStalePrice); }
    }

    /// <summary>
    /// Befehl, der die Gruppe ein- oder ausklappt.
    /// </summary>
    public ICommand ToggleCommand { get; }

    /// <summary>
    /// Befehl, der die Gruppenansicht öffnet.
    /// </summary>
    public ICommand OpenCommand { get; }

    private void Toggle()
    {
        IsExpanded = !IsExpanded;
        _expandedChanged(this);
    }
}

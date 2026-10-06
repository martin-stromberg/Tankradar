using System.Windows.Input;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Eine Favoritengruppe als Auswahlpunkt in der Detailansicht (Hinzufügen: Tippen wählt die Gruppe; Entfernen: Tippen schaltet die Auswahl um).
/// </summary>
public sealed class GroupOptionViewModel : BaseViewModel
{
    private readonly Action<GroupOptionViewModel> _invoked;
    private bool _isSelected;

    /// <summary>
    /// Erstellt den Auswahlpunkt.
    /// </summary>
    /// <param name="groupId">Die Kennung der Gruppe.</param>
    /// <param name="name">Der Name der Gruppe.</param>
    /// <param name="invoked">Wird aufgerufen, wenn der Anwender den Punkt antippt.</param>
    public GroupOptionViewModel(long groupId, string name, Action<GroupOptionViewModel> invoked)
    {
        GroupId = groupId;
        Name = name;
        _invoked = invoked;
        InvokeCommand = new Command(() => _invoked(this));
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
    /// Befehl beim Antippen.
    /// </summary>
    public ICommand InvokeCommand { get; }

    /// <summary>
    /// Gibt an, ob der Punkt zum Entfernen ausgewählt ist.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                OnPropertyChanged(nameof(SelectionHint));
            }
        }
    }

    /// <summary>
    /// Zugänglichkeitshinweis zum Auswahlzustand („Ausgewählt“ oder leer).
    /// </summary>
    public string SelectionHint => IsSelected ? ChoiceTexts.Selected : string.Empty;
}

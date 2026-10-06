using System.Windows.Input;
using Tankradar.MAUI.Models.Favorites;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Eine Tankstelle in der Gruppenansicht mit Adresse, Notiz und Priorität; „Notiz und Priorität“ öffnet das Formular zum Ändern.
/// </summary>
public sealed class FavoriteEntryItemViewModel : BaseViewModel
{
    private readonly Func<FavoriteEntryItemViewModel, Task> _save;
    private bool _isEditing;
    private string _editNote = string.Empty;

    /// <summary>
    /// Erstellt den Eintrag.
    /// </summary>
    /// <param name="entry">Der Eintrag.</param>
    /// <param name="save">Speichert die geänderte Notiz und Priorität.</param>
    public FavoriteEntryItemViewModel(FavoriteEntry entry, Func<FavoriteEntryItemViewModel, Task> save)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _save = save;
        GroupId = entry.GroupId;
        StationId = entry.StationId;
        StationName = entry.StationName;
        AddressText = entry.AddressText;
        Note = entry.Note ?? string.Empty;
        Priority = entry.Priority;
        PriorityOptions = Enum.GetValues<FavoritePriority>()
            .Select(value => new ChoiceOptionViewModel<FavoritePriority>(value, FavoritesTexts.GetPriorityLabel(value), OnPrioritySelected))
            .ToList();
        EditCommand = new Command(BeginEdit);
        SaveCommand = new Command(() => LastSaveTask = _save(this));
        CancelCommand = new Command(() => IsEditing = false);
        LastSaveTask = Task.CompletedTask;
    }

    /// <summary>
    /// Die Kennung der Gruppe.
    /// </summary>
    public long GroupId { get; }

    /// <summary>
    /// Die Kennung der Tankstelle.
    /// </summary>
    public string StationId { get; }

    /// <summary>
    /// Der Name der Tankstelle.
    /// </summary>
    public string StationName { get; }

    /// <summary>
    /// Die Adresse in einer Zeile; leer, wenn die Quelle keine liefert.
    /// </summary>
    public string AddressText { get; }

    /// <summary>
    /// Gibt an, ob eine Adresse vorliegt.
    /// </summary>
    public bool HasAddress => AddressText.Length > 0;

    /// <summary>
    /// Die gespeicherte Notiz; leer, wenn keine hinterlegt ist.
    /// </summary>
    public string Note { get; }

    /// <summary>
    /// Gibt an, ob eine Notiz hinterlegt ist.
    /// </summary>
    public bool HasNote => Note.Length > 0;

    /// <summary>
    /// Die gespeicherte Priorität.
    /// </summary>
    public FavoritePriority Priority { get; }

    /// <summary>
    /// Die Priorität als Text („Priorität: Hoch“).
    /// </summary>
    public string PriorityText
    {
        get { return FavoritesTexts.FormatPriority(Priority); }
    }

    /// <summary>
    /// Der zuletzt gestartete Speichervorgang (für Tests und Abwarten).
    /// </summary>
    public Task LastSaveTask { get; private set; }

    /// <summary>
    /// Die wählbaren Prioritäten des Formulars.
    /// </summary>
    public IReadOnlyList<ChoiceOptionViewModel<FavoritePriority>> PriorityOptions { get; }

    /// <summary>
    /// Die im Formular gewählte Priorität.
    /// </summary>
    public FavoritePriority SelectedPriority
    {
        get { return PriorityOptions.FirstOrDefault(option => option.IsSelected)?.Value ?? FavoritePriority.None; }
    }

    /// <summary>
    /// Gibt an, ob das Formular geöffnet ist.
    /// </summary>
    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    /// <summary>
    /// Die im Formular eingegebene Notiz.
    /// </summary>
    public string EditNote
    {
        get => _editNote;
        set => SetProperty(ref _editNote, value ?? string.Empty);
    }

    /// <summary>
    /// Befehl, der das Formular öffnet.
    /// </summary>
    public ICommand EditCommand { get; }

    /// <summary>
    /// Befehl, der Notiz und Priorität speichert.
    /// </summary>
    public ICommand SaveCommand { get; }

    /// <summary>
    /// Befehl, der das Formular schließt.
    /// </summary>
    public ICommand CancelCommand { get; }

    private void BeginEdit()
    {
        EditNote = Note;
        foreach (var option in PriorityOptions)
        {
            option.SetSelectedSilently(option.Value == Priority);
        }

        IsEditing = true;
    }

    private void OnPrioritySelected(ChoiceOptionViewModel<FavoritePriority> selected)
    {
        foreach (var option in PriorityOptions.Where(option => !ReferenceEquals(option, selected)))
        {
            option.SetSelectedSilently(false);
        }
    }
}

using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models.Favorites;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Favorites;
using Tankradar.MAUI.Services.Navigation;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// ViewModel der Gruppenansicht: zeigt Name, Beschreibung und die nach Priorität geordneten Tankstellen einer Favoritengruppe, erlaubt Umbenennen, Ändern der Beschreibung
/// und (nach Rückfrage in der Ansicht) Löschen der Gruppe sowie das Pflegen von Notiz und Priorität je Tankstelle. Tankstellen werden nur in der Detailansicht zu- oder abgeordnet.
/// </summary>
public class FavoriteGroupViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IFavoritesService _service;
    private readonly IFavoriteGroupNavigator _navigator;
    private readonly ILogger<FavoriteGroupViewModel> _logger;
    private long? _groupId;
    private FavoriteGroup? _group;
    private IReadOnlyList<FavoriteEntryItemViewModel> _entries = [];
    private bool _isEditing;
    private bool _isConfirmingDelete;
    private string _editName = string.Empty;
    private string _editDescription = string.Empty;
    private string? _statusMessage;
    private bool _loaded;
    private int _loadVersion;

    /// <summary>
    /// Erstellt das ViewModel mit dem Seitentitel „Favoritengruppe“.
    /// </summary>
    /// <param name="service">Der Favoritendienst.</param>
    /// <param name="navigator">Schließt die Ansicht nach dem Löschen.</param>
    /// <param name="logger">Logger (protokolliert nie Namen).</param>
    public FavoriteGroupViewModel(IFavoritesService service, IFavoriteGroupNavigator navigator, ILogger<FavoriteGroupViewModel> logger)
    {
        _service = service;
        _navigator = navigator;
        _logger = logger;
        Title = FavoritesTexts.GroupPageTitle;
        LastTask = Task.CompletedTask;
        EditCommand = new Command(BeginEdit);
        SaveEditCommand = new Command(() => LastTask = SaveEditAsync());
        CancelEditCommand = new Command(() => IsEditing = false);
        DeleteCommand = new Command(BeginDelete);
        ConfirmDeleteCommand = new Command(() => LastTask = DeleteAsync());
        CancelDeleteCommand = new Command(() => IsConfirmingDelete = false);
    }

    /// <summary>
    /// Der zuletzt gestartete Vorgang (für Tests und Abwarten).
    /// </summary>
    public Task LastTask { get; private set; }

    /// <summary>
    /// Der Name der Gruppe; leer, solange sie nicht geladen ist.
    /// </summary>
    public string Name => _group?.Name ?? string.Empty;

    /// <summary>
    /// Die Beschreibung der Gruppe; leer, wenn keine hinterlegt ist.
    /// </summary>
    public string Description => _group?.Description ?? string.Empty;

    /// <summary>
    /// Gibt an, ob eine Beschreibung hinterlegt ist.
    /// </summary>
    public bool HasDescription => Description.Length > 0;

    /// <summary>
    /// Die Tankstellen der Gruppe, nach Priorität (hoch zuerst) und Namen geordnet.
    /// </summary>
    public IReadOnlyList<FavoriteEntryItemViewModel> Entries
    {
        get => _entries;
        private set
        {
            if (SetProperty(ref _entries, value))
            {
                OnPropertyChanged(nameof(HasEntries));
                OnPropertyChanged(nameof(ShowEmptyHint));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob die Gruppe Tankstellen enthält.
    /// </summary>
    public bool HasEntries => Entries.Count > 0;

    /// <summary>
    /// Gibt an, ob der Hinweis „Diese Gruppe enthält noch keine Tankstellen“ angezeigt wird.
    /// </summary>
    public bool ShowEmptyHint => _loaded && _group is not null && Entries.Count == 0;

    /// <summary>
    /// Gibt an, ob die Gruppe geladen ist (sonst fehlt sie oder das Laden schlug fehl).
    /// </summary>
    public bool HasGroup => _group is not null;

    /// <summary>
    /// Gibt an, ob das Formular zum Umbenennen geöffnet ist.
    /// </summary>
    public bool IsEditing
    {
        get => _isEditing;
        private set
        {
            if (SetProperty(ref _isEditing, value))
            {
                OnPropertyChanged(nameof(IsNotEditing));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob das Formular zum Umbenennen geschlossen ist.
    /// </summary>
    public bool IsNotEditing => !IsEditing;

    /// <summary>
    /// Gibt an, ob die Rückfrage zum Löschen angezeigt wird.
    /// </summary>
    public bool IsConfirmingDelete
    {
        get => _isConfirmingDelete;
        private set
        {
            if (SetProperty(ref _isConfirmingDelete, value))
            {
                OnPropertyChanged(nameof(DeleteQuestion));
            }
        }
    }

    /// <summary>
    /// Die Rückfrage vor dem Löschen.
    /// </summary>
    public string DeleteQuestion
    {
        get { return _group is null ? string.Empty : FavoritesTexts.FormatDeleteQuestion(_group.Name, _group.StationCount); }
    }

    /// <summary>
    /// Der im Formular eingegebene Name.
    /// </summary>
    public string EditName
    {
        get => _editName;
        set => SetProperty(ref _editName, value ?? string.Empty);
    }

    /// <summary>
    /// Die im Formular eingegebene Beschreibung.
    /// </summary>
    public string EditDescription
    {
        get => _editDescription;
        set => SetProperty(ref _editDescription, value ?? string.Empty);
    }

    /// <summary>
    /// Ein Hinweis oder eine Fehlermeldung; <see langword="null"/>, wenn keine anliegt.
    /// </summary>
    public string? StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob eine <see cref="StatusMessage"/> anliegt.
    /// </summary>
    public bool HasStatusMessage => StatusMessage is { Length: > 0 };

    /// <summary>
    /// Befehl „Gruppe bearbeiten“: öffnet das Formular zum Umbenennen.
    /// </summary>
    public ICommand EditCommand { get; }

    /// <summary>
    /// Befehl „Speichern“ des Formulars.
    /// </summary>
    public ICommand SaveEditCommand { get; }

    /// <summary>
    /// Befehl „Abbrechen“ des Formulars.
    /// </summary>
    public ICommand CancelEditCommand { get; }

    /// <summary>
    /// Befehl „Gruppe löschen“: zeigt die Rückfrage.
    /// </summary>
    public ICommand DeleteCommand { get; }

    /// <summary>
    /// Befehl „Ja, löschen“ der Rückfrage.
    /// </summary>
    public ICommand ConfirmDeleteCommand { get; }

    /// <summary>
    /// Befehl „Abbrechen“ der Rückfrage.
    /// </summary>
    public ICommand CancelDeleteCommand { get; }

    /// <summary>
    /// Übernimmt die Kennung der Gruppe aus den Navigationsparametern.
    /// </summary>
    /// <param name="query">Die Navigationsparameter; erwartet wird <see cref="ShellFavoriteGroupNavigator.GroupIdParameter"/>.</param>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue(ShellFavoriteGroupNavigator.GroupIdParameter, out var value) && value is long groupId)
        {
            Show(groupId);
        }
    }

    /// <summary>
    /// Wechselt zu einer Gruppe und setzt die Formulare zurück.
    /// </summary>
    /// <param name="groupId">Die Kennung der Gruppe.</param>
    public void Show(long groupId)
    {
        _groupId = groupId;
        _group = null;
        _loaded = false;
        Entries = [];
        IsEditing = false;
        IsConfirmingDelete = false;
        StatusMessage = null;
        NotifyGroupChanged();
    }

    /// <summary>
    /// Lädt die Gruppe beim Erscheinen der Seite.
    /// </summary>
    public override void OnAppearing()
    {
        LastTask = LoadAsync();
    }

    /// <summary>
    /// Lädt Gruppe und Tankstellen.
    /// </summary>
    /// <returns>Ein Task, der nach dem Laden abgeschlossen ist.</returns>
    public async Task LoadAsync()
    {
        if (_groupId is not { } groupId)
        {
            return;
        }

        var version = Interlocked.Increment(ref _loadVersion);
        try
        {
            var group = await _service.GetGroupAsync(groupId).ConfigureAwait(true);
            var entries = group is null ? [] : await _service.GetEntriesAsync(groupId).ConfigureAwait(true);
            if (_groupId != groupId || version != _loadVersion)
            {
                return;
            }

            _group = group;
            _loaded = true;
            Entries = entries.Select(entry => new FavoriteEntryItemViewModel(entry, SaveEntryAsync)).ToList();
            StatusMessage = group is null ? FavoritesTexts.GetResultMessage(FavoriteResult.GroupNotFound) : null;
            NotifyGroupChanged();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Die Favoritengruppe konnte nicht geladen werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = FavoritesTexts.LoadFailed;
        }
    }

    private void NotifyGroupChanged()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(HasDescription));
        OnPropertyChanged(nameof(HasGroup));
        OnPropertyChanged(nameof(ShowEmptyHint));
        OnPropertyChanged(nameof(DeleteQuestion));
    }

    private void BeginEdit()
    {
        EditName = Name;
        EditDescription = Description;
        StatusMessage = null;
        IsConfirmingDelete = false;
        IsEditing = true;
    }

    private void BeginDelete()
    {
        StatusMessage = null;
        IsEditing = false;
        IsConfirmingDelete = true;
    }

    private async Task SaveEditAsync()
    {
        if (_groupId is not { } groupId)
        {
            return;
        }

        try
        {
            var result = await _service.UpdateGroupAsync(groupId, EditName, EditDescription).ConfigureAwait(true);
            if (result == FavoriteResult.Ok)
            {
                IsEditing = false;
                StatusMessage = null;
                await LoadAsync().ConfigureAwait(true);
            }
            else
            {
                StatusMessage = FavoritesTexts.GetResultMessage(result);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Die Favoritengruppe konnte nicht geändert werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = FavoritesTexts.SaveFailed;
        }
    }

    private async Task DeleteAsync()
    {
        if (_groupId is not { } groupId || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _service.DeleteGroupAsync(groupId).ConfigureAwait(true);
            if (result is FavoriteResult.Ok or FavoriteResult.GroupNotFound)
            {
                IsConfirmingDelete = false;
                await _navigator.CloseAsync().ConfigureAwait(true);
            }
            else
            {
                StatusMessage = FavoritesTexts.GetResultMessage(result);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Die Favoritengruppe konnte nicht gelöscht werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = FavoritesTexts.SaveFailed;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveEntryAsync(FavoriteEntryItemViewModel item)
    {
        try
        {
            var result = await _service.UpdateEntryAsync(item.GroupId, item.StationId, item.EditNote, item.SelectedPriority).ConfigureAwait(true);
            if (result == FavoriteResult.Ok)
            {
                StatusMessage = null;
                await LoadAsync().ConfigureAwait(true);
            }
            else
            {
                StatusMessage = FavoritesTexts.GetResultMessage(result);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Der Favorit konnte nicht geändert werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = FavoritesTexts.SaveFailed;
        }
    }
}

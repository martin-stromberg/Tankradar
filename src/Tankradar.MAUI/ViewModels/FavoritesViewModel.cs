using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models.Favorites;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Favorites;
using Tankradar.MAUI.Services.Navigation;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// ViewModel für den Bereich „Favoriten“: listet die Favoritengruppen mit Beschreibung und Zahl der Tankstellen, legt neue Gruppen an und öffnet die Gruppenansicht.
/// Die Startseite mit Preisen und Entfernungen folgt in einem späteren Schritt; Tankstellen werden nur in der Detailansicht zugeordnet.
/// </summary>
public class FavoritesViewModel : BaseViewModel
{
    private readonly IFavoritesService _service;
    private readonly IFavoriteGroupNavigator _navigator;
    private readonly ILogger<FavoritesViewModel> _logger;
    private IReadOnlyList<FavoriteGroupItemViewModel> _groups = [];
    private bool _isCreating;
    private string _newName = string.Empty;
    private string _newDescription = string.Empty;
    private string? _statusMessage;
    private bool _loaded;

    /// <summary>
    /// Erstellt das ViewModel mit dem Seitentitel „Favoriten“.
    /// </summary>
    /// <param name="service">Der Favoritendienst.</param>
    /// <param name="navigator">Öffnet die Gruppenansicht.</param>
    /// <param name="logger">Logger (protokolliert nie Namen).</param>
    public FavoritesViewModel(IFavoritesService service, IFavoriteGroupNavigator navigator, ILogger<FavoritesViewModel> logger)
    {
        _service = service;
        _navigator = navigator;
        _logger = logger;
        Title = FavoritesTexts.PageTitle;
        LastTask = Task.CompletedTask;
        ShowCreateCommand = new Command(ShowCreate);
        CreateCommand = new Command(() => LastTask = CreateAsync());
        CancelCreateCommand = new Command(CloseCreate);
        FavoritesChangeSubscription.Attach(service, this, static self => self.OnFavoritesChanged());
    }

    /// <summary>
    /// Der zuletzt gestartete Vorgang (für Tests und Abwarten).
    /// </summary>
    public Task LastTask { get; private set; }

    /// <summary>
    /// Die Favoritengruppen nach Namen geordnet.
    /// </summary>
    public IReadOnlyList<FavoriteGroupItemViewModel> Groups
    {
        get => _groups;
        private set
        {
            if (SetProperty(ref _groups, value))
            {
                OnPropertyChanged(nameof(HasGroups));
                OnPropertyChanged(nameof(ShowEmptyHint));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob es Gruppen gibt.
    /// </summary>
    public bool HasGroups => Groups.Count > 0;

    /// <summary>
    /// Gibt an, ob der Hinweis „Noch keine Favoritengruppen“ angezeigt wird (nach dem Laden ohne Gruppen).
    /// </summary>
    public bool ShowEmptyHint => _loaded && Groups.Count == 0;

    /// <summary>
    /// Gibt an, ob das Formular für eine neue Gruppe geöffnet ist.
    /// </summary>
    public bool IsCreating
    {
        get => _isCreating;
        private set
        {
            if (SetProperty(ref _isCreating, value))
            {
                OnPropertyChanged(nameof(IsNotCreating));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob das Formular für eine neue Gruppe geschlossen ist.
    /// </summary>
    public bool IsNotCreating => !IsCreating;

    /// <summary>
    /// Der eingegebene Name der neuen Gruppe.
    /// </summary>
    public string NewName
    {
        get => _newName;
        set => SetProperty(ref _newName, value ?? string.Empty);
    }

    /// <summary>
    /// Die eingegebene Beschreibung der neuen Gruppe.
    /// </summary>
    public string NewDescription
    {
        get => _newDescription;
        set => SetProperty(ref _newDescription, value ?? string.Empty);
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
    /// Befehl „Neue Gruppe“: öffnet das Formular.
    /// </summary>
    public ICommand ShowCreateCommand { get; }

    /// <summary>
    /// Befehl „Gruppe anlegen“.
    /// </summary>
    public ICommand CreateCommand { get; }

    /// <summary>
    /// Befehl „Abbrechen“: schließt das Formular.
    /// </summary>
    public ICommand CancelCreateCommand { get; }

    /// <summary>
    /// Lädt die Gruppen beim Erscheinen der Seite (auch nach der Rückkehr aus der Gruppen- oder Detailansicht).
    /// </summary>
    public override void OnAppearing()
    {
        LastTask = LoadAsync();
    }

    /// <summary>
    /// Lädt die Gruppen.
    /// </summary>
    /// <returns>Ein Task, der nach dem Laden abgeschlossen ist.</returns>
    public async Task LoadAsync()
    {
        try
        {
            var groups = await _service.GetGroupsAsync().ConfigureAwait(true);
            Groups = groups.Select(group => new FavoriteGroupItemViewModel(group, Open)).ToList();
            _loaded = true;
            OnPropertyChanged(nameof(ShowEmptyHint));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Nur der Typ wird protokolliert: Ausnahmetexte könnten Pfade enthalten.
            _logger.LogWarning("Die Favoritengruppen konnten nicht geladen werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = FavoritesTexts.LoadFailed;
        }
    }

    private void OnFavoritesChanged()
    {
        if (_loaded)
        {
            LastTask = Task.WhenAll(LastTask, LoadAsync());
        }
    }

    private void Open(FavoriteGroupItemViewModel item)
    {
        LastTask = _navigator.OpenGroupAsync(item.GroupId);
    }

    private void ShowCreate()
    {
        NewName = string.Empty;
        NewDescription = string.Empty;
        StatusMessage = null;
        IsCreating = true;
    }

    private void CloseCreate()
    {
        IsCreating = false;
        StatusMessage = null;
    }

    private async Task CreateAsync()
    {
        try
        {
            var result = await _service.CreateGroupAsync(NewName, NewDescription).ConfigureAwait(true);
            if (result.Result == FavoriteResult.Ok)
            {
                IsCreating = false;
                StatusMessage = null;
                await LoadAsync().ConfigureAwait(true);
            }
            else
            {
                StatusMessage = FavoritesTexts.GetResultMessage(result.Result);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Die Favoritengruppe konnte nicht angelegt werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = FavoritesTexts.SaveFailed;
        }
    }
}

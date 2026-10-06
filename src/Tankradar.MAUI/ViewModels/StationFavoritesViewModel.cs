using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models.Favorites;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Favorites;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// ViewModel der Karte „Favoritengruppen“ in der Tankstellen-Detailansicht: zeigt die Gruppen der Tankstelle und bietet „Zu Favoriten hinzufügen“ (bestehende oder neue Gruppe)
/// sowie „Aus Favoriten entfernen“ (eine Gruppe direkt, bei mehreren Gruppen mit Auswahl). Die Bedienung läuft in der Karte selbst, ohne Dialoge des Betriebssystems.
/// </summary>
public class StationFavoritesViewModel : BaseViewModel
{
    private readonly IFavoritesService _service;
    private readonly ILogger<StationFavoritesViewModel> _logger;
    private string? _stationId;
    private IReadOnlyList<FavoriteGroup> _assignedGroups = [];
    private IReadOnlyList<GroupOptionViewModel> _addOptions = [];
    private IReadOnlyList<GroupOptionViewModel> _removeOptions = [];
    private bool _isAddOpen;
    private bool _isRemoveOpen;
    private string _newGroupName = string.Empty;
    private string? _statusMessage;
    private int _loadVersion;

    /// <summary>
    /// Erstellt das ViewModel.
    /// </summary>
    /// <param name="service">Der Favoritendienst.</param>
    /// <param name="logger">Logger (protokolliert nie Namen oder Kennungen).</param>
    public StationFavoritesViewModel(IFavoritesService service, ILogger<StationFavoritesViewModel> logger)
    {
        _service = service;
        _logger = logger;
        Title = FavoritesTexts.DetailHeading;
        LastTask = Task.CompletedTask;
        ToggleAddCommand = new Command(() => LastTask = ToggleAddAsync());
        RemoveCommand = new Command(() => LastTask = StartRemoveAsync());
        ConfirmRemoveCommand = new Command(() => LastTask = ConfirmRemoveAsync());
        CreateAndAddCommand = new Command(() => LastTask = CreateAndAddAsync());
        CancelCommand = new Command(ClosePanels);

        // Änderungen aus anderen Ansichten (z. B. Umbenennen oder Löschen einer Gruppe im Bereich „Favoriten“) wirken auch auf eine im Hintergrund geöffnete Detailansicht.
        FavoritesChangeSubscription.Attach(service, this, static self => self.OnFavoritesChanged());
    }

    /// <summary>
    /// Der zuletzt gestartete Vorgang (für Tests und Abwarten).
    /// </summary>
    public Task LastTask { get; private set; }

    /// <summary>
    /// Die Gruppen, denen die Tankstelle angehört.
    /// </summary>
    public IReadOnlyList<FavoriteGroup> AssignedGroups
    {
        get => _assignedGroups;
        private set
        {
            if (SetProperty(ref _assignedGroups, value))
            {
                OnPropertyChanged(nameof(HasAssignedGroups));
                OnPropertyChanged(nameof(HasNoAssignedGroups));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob die Tankstelle mindestens einer Gruppe angehört (dann gibt es zusätzlich „Aus Favoriten entfernen“).
    /// </summary>
    public bool HasAssignedGroups => AssignedGroups.Count > 0;

    /// <summary>
    /// Gibt an, ob die Tankstelle keiner Gruppe angehört.
    /// </summary>
    public bool HasNoAssignedGroups => AssignedGroups.Count == 0;

    /// <summary>
    /// Die Gruppen, die zum Hinzufügen angeboten werden (alle, denen die Tankstelle noch nicht angehört).
    /// </summary>
    public IReadOnlyList<GroupOptionViewModel> AddOptions
    {
        get => _addOptions;
        private set
        {
            if (SetProperty(ref _addOptions, value))
            {
                OnPropertyChanged(nameof(HasAddOptions));
                OnPropertyChanged(nameof(HasNoAddOptions));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob bestehende Gruppen zum Hinzufügen zur Wahl stehen.
    /// </summary>
    public bool HasAddOptions => AddOptions.Count > 0;

    /// <summary>
    /// Gibt an, ob es bestehende Gruppen gibt, denen die Tankstelle noch nicht angehört, nicht.
    /// </summary>
    public bool HasNoAddOptions => AddOptions.Count == 0;

    /// <summary>
    /// Die Gruppen, aus denen die Tankstelle entfernt werden kann (Mehrfachauswahl).
    /// </summary>
    public IReadOnlyList<GroupOptionViewModel> RemoveOptions
    {
        get => _removeOptions;
        private set => SetProperty(ref _removeOptions, value);
    }

    /// <summary>
    /// Gibt an, ob die Auswahl zum Hinzufügen geöffnet ist.
    /// </summary>
    public bool IsAddOpen
    {
        get => _isAddOpen;
        private set => SetProperty(ref _isAddOpen, value);
    }

    /// <summary>
    /// Gibt an, ob die Auswahl zum Entfernen geöffnet ist.
    /// </summary>
    public bool IsRemoveOpen
    {
        get => _isRemoveOpen;
        private set
        {
            if (SetProperty(ref _isRemoveOpen, value))
            {
                OnPropertyChanged(nameof(CanConfirmRemove));
            }
        }
    }

    /// <summary>
    /// Der eingegebene Name einer neuen Gruppe.
    /// </summary>
    public string NewGroupName
    {
        get => _newGroupName;
        set
        {
            if (SetProperty(ref _newGroupName, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(CanCreate));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob „Anlegen und hinzufügen“ bedienbar ist (ein Name ist eingegeben und nichts läuft).
    /// </summary>
    public bool CanCreate
    {
        get { return !IsBusy && NewGroupName.Trim().Length > 0; }
    }

    /// <summary>
    /// Gibt an, ob „Entfernen“ bedienbar ist (mindestens eine Gruppe ausgewählt und nichts läuft).
    /// </summary>
    public bool CanConfirmRemove
    {
        get { return !IsBusy && RemoveOptions.Any(option => option.IsSelected); }
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
    /// Befehl „Zu Favoriten hinzufügen“: öffnet oder schließt die Auswahl.
    /// </summary>
    public ICommand ToggleAddCommand { get; }

    /// <summary>
    /// Befehl „Aus Favoriten entfernen“: entfernt bei einer Gruppe sofort, bei mehreren öffnet er die Auswahl.
    /// </summary>
    public ICommand RemoveCommand { get; }

    /// <summary>
    /// Befehl „Entfernen“ der Auswahl: entfernt die Tankstelle aus den gewählten Gruppen.
    /// </summary>
    public ICommand ConfirmRemoveCommand { get; }

    /// <summary>
    /// Befehl „Anlegen und hinzufügen“: legt die neue Gruppe an und ordnet die Tankstelle zu.
    /// </summary>
    public ICommand CreateAndAddCommand { get; }

    /// <summary>
    /// Befehl „Abbrechen“: schließt die Auswahl.
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// Wechselt zu einer anderen Tankstelle und lädt deren Gruppen.
    /// </summary>
    /// <param name="stationId">Die Kennung der Tankstelle.</param>
    /// <returns>Ein Task, der nach dem Laden abgeschlossen ist.</returns>
    public Task SetStationAsync(string stationId)
    {
        ArgumentNullException.ThrowIfNull(stationId);
        _stationId = stationId;
        ClosePanels();
        StatusMessage = null;
        LastTask = ReloadAsync();
        return LastTask;
    }

    /// <summary>
    /// Lädt die Gruppen der Tankstelle neu (beim Erscheinen der Seite, falls Gruppen inzwischen geändert wurden).
    /// </summary>
    public override void OnAppearing()
    {
        if (_stationId is not null)
        {
            LastTask = ReloadAsync();
        }
    }

    private void OnFavoritesChanged()
    {
        if (_stationId is not null)
        {
            LastTask = Task.WhenAll(LastTask, ReloadAsync());
        }
    }

    private async Task ToggleAddAsync()
    {
        if (IsAddOpen)
        {
            ClosePanels();
            return;
        }

        StatusMessage = null;
        if (!await ReloadAsync().ConfigureAwait(true))
        {
            return;
        }

        IsRemoveOpen = false;
        NewGroupName = string.Empty;
        IsAddOpen = true;
    }

    private async Task StartRemoveAsync()
    {
        if (IsRemoveOpen)
        {
            ClosePanels();
            return;
        }

        StatusMessage = null;
        if (!await ReloadAsync().ConfigureAwait(true) || AssignedGroups.Count == 0)
        {
            return;
        }

        if (AssignedGroups.Count == 1)
        {
            await RemoveFromAsync([AssignedGroups[0].Id]).ConfigureAwait(true);
            return;
        }

        IsAddOpen = false;
        RemoveOptions = AssignedGroups.Select(group => new GroupOptionViewModel(group.Id, group.Name, ToggleRemoveOption)).ToList();
        IsRemoveOpen = true;
    }

    private async Task ConfirmRemoveAsync()
    {
        var ids = RemoveOptions.Where(option => option.IsSelected).Select(option => option.GroupId).ToList();
        if (ids.Count == 0)
        {
            return;
        }

        await RemoveFromAsync(ids).ConfigureAwait(true);
    }

    private async Task RemoveFromAsync(IReadOnlyCollection<long> groupIds)
    {
        if (_stationId is not { } stationId || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _service.RemoveStationAsync(stationId, groupIds).ConfigureAwait(true);
            ClosePanels();

            // „Nicht (mehr) zugeordnet“ ist das gewünschte Ergebnis (z. B. nach einer Änderung in einer anderen Ansicht), kein Fehler.
            StatusMessage = result == FavoriteResult.NotMember ? null : NullIfEmpty(FavoritesTexts.GetResultMessage(result));
            await ReloadAsync().ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Nur der Typ wird protokolliert: Ausnahmetexte könnten Namen enthalten.
            _logger.LogWarning("Die Favoriten konnten nicht geändert werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = FavoritesTexts.SaveFailed;
        }
        finally
        {
            IsBusy = false;
            RaiseCanChanged();
        }
    }

    private async Task CreateAndAddAsync()
    {
        if (_stationId is not { } stationId || !CanCreate)
        {
            return;
        }

        IsBusy = true;
        RaiseCanChanged();
        try
        {
            var result = await _service.AddStationToNewGroupAsync(NewGroupName, stationId).ConfigureAwait(true);
            if (result.Result == FavoriteResult.Ok)
            {
                ClosePanels();
                StatusMessage = null;
                await ReloadAsync().ConfigureAwait(true);
            }
            else
            {
                StatusMessage = NullIfEmpty(FavoritesTexts.GetResultMessage(result.Result));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Die Favoritengruppe konnte nicht angelegt werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = FavoritesTexts.SaveFailed;
        }
        finally
        {
            IsBusy = false;
            RaiseCanChanged();
        }
    }

    private async Task AddToAsync(GroupOptionViewModel option)
    {
        if (_stationId is not { } stationId || IsBusy)
        {
            return;
        }

        IsBusy = true;
        RaiseCanChanged();
        try
        {
            var result = await _service.AddStationAsync(option.GroupId, stationId).ConfigureAwait(true);
            if (result is FavoriteResult.Ok or FavoriteResult.AlreadyMember)
            {
                ClosePanels();
                StatusMessage = null;
                await ReloadAsync().ConfigureAwait(true);
            }
            else
            {
                StatusMessage = NullIfEmpty(FavoritesTexts.GetResultMessage(result));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Die Tankstelle konnte keiner Favoritengruppe zugeordnet werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = FavoritesTexts.SaveFailed;
        }
        finally
        {
            IsBusy = false;
            RaiseCanChanged();
        }
    }

    private void ToggleRemoveOption(GroupOptionViewModel option)
    {
        option.IsSelected = !option.IsSelected;
        OnPropertyChanged(nameof(CanConfirmRemove));
    }

    private void ClosePanels()
    {
        IsAddOpen = false;
        IsRemoveOpen = false;
        NewGroupName = string.Empty;
    }

    private void RaiseCanChanged()
    {
        OnPropertyChanged(nameof(CanCreate));
        OnPropertyChanged(nameof(CanConfirmRemove));
    }

    private async Task<bool> ReloadAsync()
    {
        if (_stationId is not { } stationId)
        {
            return false;
        }

        var version = Interlocked.Increment(ref _loadVersion);
        try
        {
            var assigned = await _service.GetGroupsOfStationAsync(stationId).ConfigureAwait(true);
            var all = await _service.GetGroupsAsync().ConfigureAwait(true);
            if (version != _loadVersion || stationId != _stationId)
            {
                return false;
            }

            var assignedIds = assigned.Select(group => group.Id).ToHashSet();
            AssignedGroups = assigned;
            AddOptions = all.Where(group => !assignedIds.Contains(group.Id)).Select(group => new GroupOptionViewModel(group.Id, group.Name, option => LastTask = AddToAsync(option))).ToList();
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Die Favoritengruppen konnten nicht geladen werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = FavoritesTexts.LoadFailed;
            return false;
        }
    }

    private static string? NullIfEmpty(string value)
    {
        return value.Length == 0 ? null : value;
    }
}

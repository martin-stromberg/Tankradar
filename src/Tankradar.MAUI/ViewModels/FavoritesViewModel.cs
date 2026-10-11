using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Favorites;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services;
using Tankradar.MAUI.Services.Favorites;
using Tankradar.MAUI.Services.Location;
using Tankradar.MAUI.Services.Navigation;
using Tankradar.MAUI.Services.Pricing;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// ViewModel der Startseite („Favoriten“): zeigt je Favoritengruppe die Tankstellen mit Name, Entfernung (nur bei erlaubter Standortnutzung), Preisen der gewählten Sorten samt Alter
/// und Hinweisen, aktualisiert die Preise beim Erscheinen und auf Wunsch, zeigt offline den letzten Stand mit Hinweis und aktualisiert nach Wiederverbindung. Optional listet der Bereich
/// „In der Nähe“ Tankstellen um den Standort. Der Standort wird nur bei Bedarf ermittelt, nur im Arbeitsspeicher gehalten und nie gespeichert oder protokolliert.
/// </summary>
public class FavoritesViewModel : BaseViewModel
{
    private readonly IFavoritesService _service;
    private readonly IFavoriteGroupNavigator _navigator;
    private readonly ISettingsService _settingsService;
    private readonly IFuelPriceService _priceService;
    private readonly ILocationService _locationService;
    private readonly IConnectionMonitor _connection;
    private readonly IStationNavigator _stationNavigator;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<FavoritesViewModel> _logger;
    private readonly SynchronizationContext? _context = SynchronizationContext.Current;
    private readonly HashSet<long> _collapsed = [];
    private IReadOnlyList<HomeGroupViewModel> _groups = [];
    private IReadOnlyList<FavoriteGroupOverview> _overview = [];
    private IReadOnlyDictionary<string, StationInfo> _stations = new Dictionary<string, StationInfo>();
    private AppSettings? _settings;
    private GeoPosition? _position;
    private StationRefreshResult? _lastRefresh;
    private CancellationTokenSource? _refreshCancellation;
    private bool _subscribed;
    private bool _isOpeningStation;
    private bool _isOffline;
    private bool _isCreating;
    private string _newName = string.Empty;
    private string _newDescription = string.Empty;
    private string? _statusMessage;
    private bool _loaded;
    private int _loadVersion;

    /// <summary>
    /// Erstellt das ViewModel mit dem Seitentitel „Favoriten“.
    /// </summary>
    /// <param name="service">Der Favoritendienst.</param>
    /// <param name="navigator">Öffnet die Gruppenansicht.</param>
    /// <param name="settingsService">Lädt die Einstellungen (Spritsorten, Standortnutzung, „In der Nähe“).</param>
    /// <param name="priceService">Der Preisdienst für die Aktualisierung nach Kennungen.</param>
    /// <param name="locationService">Der Standortdienst (nur bei Bedarf aufgerufen).</param>
    /// <param name="connection">Die Verbindungserkennung (Offline-Hinweis, Wiederverbindung).</param>
    /// <param name="stationNavigator">Öffnet die Detailansicht einer Tankstelle.</param>
    /// <param name="timeProvider">Die Zeitquelle für Altersangaben.</param>
    /// <param name="nearby">Der Bereich „In der Nähe“.</param>
    /// <param name="logger">Logger (protokolliert nie Namen oder Koordinaten).</param>
    public FavoritesViewModel(
        IFavoritesService service,
        IFavoriteGroupNavigator navigator,
        ISettingsService settingsService,
        IFuelPriceService priceService,
        ILocationService locationService,
        IConnectionMonitor connection,
        IStationNavigator stationNavigator,
        TimeProvider timeProvider,
        NearbySectionViewModel nearby,
        ILogger<FavoritesViewModel> logger)
    {
        _service = service;
        _navigator = navigator;
        _settingsService = settingsService;
        _priceService = priceService;
        _locationService = locationService;
        _connection = connection;
        _stationNavigator = stationNavigator;
        _timeProvider = timeProvider;
        Nearby = nearby;
        _logger = logger;
        Title = FavoritesTexts.PageTitle;
        LastTask = Task.CompletedTask;
        _isOffline = !connection.IsOnline;
        ShowCreateCommand = new Command(ShowCreate);
        CreateCommand = new Command(() => LastTask = CreateAsync());
        CancelCreateCommand = new Command(CloseCreate);
        RefreshCommand = new Command(RefreshRequested);
        FavoritesChangeSubscription.Attach(service, this, static self => self.OnFavoritesChanged());
    }

    /// <summary>
    /// Der zuletzt gestartete Vorgang (für Tests und Abwarten).
    /// </summary>
    public Task LastTask { get; private set; }

    /// <summary>
    /// Der Bereich „In der Nähe“.
    /// </summary>
    public NearbySectionViewModel Nearby { get; }

    /// <summary>
    /// Die Favoritengruppen nach Namen geordnet mit ihren Tankstellen.
    /// </summary>
    public IReadOnlyList<HomeGroupViewModel> Groups
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
    /// Gibt an, ob der Offline-Hinweis angezeigt wird (keine Verbindung oder zuletzt bekannte Preise statt eines Abrufs).
    /// </summary>
    public bool IsOffline
    {
        get => _isOffline;
        private set
        {
            if (SetProperty(ref _isOffline, value))
            {
                OnPropertyChanged(nameof(OfflineMessage));
            }
        }
    }

    /// <summary>
    /// Der Text des Offline-Hinweises; <see langword="null"/>, wenn <see cref="IsOffline"/> nicht gilt.
    /// </summary>
    public string? OfflineMessage => IsOffline ? SearchTexts.OfflineBanner : null;

    /// <summary>
    /// Gibt an, ob „Preise aktualisieren“ bedienbar ist (online und nicht beschäftigt).
    /// </summary>
    public bool CanRefresh => _connection.IsOnline && !IsBusy;

    /// <summary>
    /// Die Beschriftung der Schaltfläche „Preise aktualisieren“.
    /// </summary>
    public string RefreshText => FavoritesTexts.RefreshPrices;

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
    /// Befehl „Preise aktualisieren“: ruft die Preise unabhängig von der Frische ab (nur online).
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// Meldet sich beim Verbindungsmonitor an, lädt die Gruppen aus dem lokalen Stand und aktualisiert die Preise (auch nach der Rückkehr aus der Gruppen- oder Detailansicht).
    /// </summary>
    public override void OnAppearing()
    {
        if (!_subscribed)
        {
            _connection.ConnectionChanged += OnConnectionChanged;
            _connection.ConnectionRestored += OnConnectionRestored;
            _subscribed = true;
        }

        UpdateOfflineState();
        LastTask = LoadAndRefreshAsync();
    }

    /// <summary>
    /// Meldet sich vom Verbindungsmonitor ab, bricht laufende Abrufe ab und verwirft den Standort.
    /// </summary>
    public override void OnDisappearing()
    {
        if (_subscribed)
        {
            _connection.ConnectionChanged -= OnConnectionChanged;
            _connection.ConnectionRestored -= OnConnectionRestored;
            _subscribed = false;
        }

        CancelRefresh();
        SetBusy(false);
        _position = null;
    }

    /// <summary>
    /// Lädt Gruppen und lokal bekannte Preise ohne Abruf beim Preisdienst und baut die Anzeige auf.
    /// </summary>
    /// <returns>Ein Task, der nach dem Laden abgeschlossen ist.</returns>
    public async Task LoadAsync()
    {
        var version = Interlocked.Increment(ref _loadVersion);
        try
        {
            var settings = await _settingsService.LoadAsync().ConfigureAwait(true);
            var overview = await _service.GetOverviewAsync().ConfigureAwait(true);
            var ids = StationIds(overview);
            var known = ids.Count == 0 ? [] : await _priceService.GetKnownStationsAsync(ids).ConfigureAwait(true);
            if (version != _loadVersion)
            {
                return;
            }

            _settings = settings;
            _overview = overview;
            _stations = known.ToDictionary(station => station.Id);
            _loaded = true;
            Rebuild();
            OnPropertyChanged(nameof(ShowEmptyHint));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Nur der Typ wird protokolliert: Ausnahmetexte könnten Pfade enthalten.
            _logger.LogWarning("Die Favoritengruppen konnten nicht geladen werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = FavoritesTexts.LoadFailed;
        }
    }

    /// <summary>
    /// Aktualisiert die Preise der Favoriten (und lädt „In der Nähe“ neu). Ein laufender Abruf wird abgebrochen.
    /// Der Standort wird nur ermittelt, wenn die Standortnutzung ihn erlaubt und ein Bedarf besteht (Favoriten-Tankstellen oder „In der Nähe“).
    /// </summary>
    /// <param name="force">Ruft unabhängig von der Frische der Preise ab.</param>
    /// <returns>Ein Task, der nach dem Abruf abgeschlossen ist.</returns>
    public async Task RefreshAsync(bool force)
    {
        CancelRefresh();
        var cancellation = new CancellationTokenSource();
        _refreshCancellation = cancellation;
        var token = cancellation.Token;
        SetBusy(true);
        try
        {
            var settings = _settings ?? await _settingsService.LoadAsync(token).ConfigureAwait(true);
            _settings = settings;
            var ids = StationIds(_overview);

            LocationResult? location = null;
            if (settings.GpsUsage.AllowsLocation() && (ids.Count > 0 || settings.ShowNearby))
            {
                location = await GetLocationAsync(settings.GpsUsage, token).ConfigureAwait(true);
            }

            token.ThrowIfCancellationRequested();
            _position = location?.Position;

            if (ids.Count > 0)
            {
                var result = await _priceService.RefreshStationsAsync(ids, force, token).ConfigureAwait(true);
                token.ThrowIfCancellationRequested();
                _lastRefresh = result;
                var merged = new Dictionary<string, StationInfo>(_stations);
                foreach (var station in result.Stations)
                {
                    merged[station.Id] = station;
                }

                _stations = merged;
                StatusMessage = NullIfEmpty(SearchTexts.GetFailureMessage(result.Failure, result.Stations.Count > 0));
            }
            else
            {
                _lastRefresh = null;
            }

            token.ThrowIfCancellationRequested();
            Rebuild();
            await Nearby.LoadAsync(settings, location, token).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            // Ein neuer Abruf oder das Verlassen der Seite hat diesen Abruf abgebrochen.
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Die Preise der Favoriten konnten nicht aktualisiert werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = FavoritesTexts.LoadFailed;
        }
        finally
        {
            if (ReferenceEquals(_refreshCancellation, cancellation))
            {
                _refreshCancellation = null;
                SetBusy(false);
            }

            UpdateOfflineState();
            cancellation.Dispose();
        }
    }

    private static string? NullIfEmpty(string text)
    {
        return text.Length == 0 ? null : text;
    }

    private static List<string> StationIds(IReadOnlyList<FavoriteGroupOverview> overview)
    {
        return overview.SelectMany(group => group.Entries).Select(entry => entry.StationId).Distinct().ToList();
    }

    private async Task LoadAndRefreshAsync()
    {
        await LoadAsync().ConfigureAwait(true);
        await RefreshAsync(force: false).ConfigureAwait(true);
    }

    private async Task<LocationResult> GetLocationAsync(GpsUsage gpsUsage, CancellationToken cancellationToken)
    {
        try
        {
            return await _locationService.GetCurrentLocationAsync(gpsUsage, cancellationToken).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Der Standort wird nie protokolliert; nur der Typ des Fehlers.
            _logger.LogWarning("Der Standort konnte nicht ermittelt werden ({ExceptionType}).", ex.GetType().Name);
            return LocationResult.Failure(LocationStatus.Unavailable);
        }
    }

    private void Rebuild()
    {
        var settings = _settings ?? AppSettings.CreateDefault();
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var built = HomeOverviewBuilder.Build(_overview, _stations, settings.FuelTypes, _position, now);
        Groups = built.Select(CreateGroup).ToList();
    }

    private HomeGroupViewModel CreateGroup(HomeGroup group)
    {
        var stations = group.Stations.Select(station => new HomeStationViewModel(station, "Favorites.Station", OpenStation)).ToList();
        return new HomeGroupViewModel(group, stations, !_collapsed.Contains(group.Id), OpenGroup, OnExpandedChanged);
    }

    private void OnExpandedChanged(HomeGroupViewModel group)
    {
        if (group.IsExpanded)
        {
            _collapsed.Remove(group.GroupId);
        }
        else
        {
            _collapsed.Add(group.GroupId);
        }
    }

    private void OnFavoritesChanged()
    {
        if (_loaded)
        {
            LastTask = Task.WhenAll(LastTask, LoadAsync());
        }
    }

    private void OnConnectionChanged(object? sender, bool online)
    {
        RunOnUiThread(UpdateOfflineState);
    }

    private void OnConnectionRestored(object? sender, EventArgs e)
    {
        RunOnUiThread(HandleConnectionRestored);
    }

    private void RunOnUiThread(Action action)
    {
        // Das Ereignis kommt vom Threadpool; die Reaktion läuft im Kontext, in dem das ViewModel erzeugt wurde (UI-Thread der App).
        if (_context is null || SynchronizationContext.Current == _context)
        {
            action();
        }
        else
        {
            _context.Post(_ => action(), null);
        }
    }

    private void HandleConnectionRestored()
    {
        if (!_subscribed)
        {
            return;
        }

        // Nach Wiederverbindung wird nur aktualisiert, wenn zuletzt kein Abruf möglich war oder Preise veraltet sind (ab 60 Minuten).
        UpdateOfflineState();
        var needsRefresh = _lastRefresh?.Source == PriceDataSource.OfflineFallback
            || Groups.Any(group => group.HasStalePrice)
            || Nearby.IsOfflineResult;
        if (needsRefresh)
        {
            LastTask = RefreshAsync(force: false);
        }
    }

    private void UpdateOfflineState()
    {
        IsOffline = !_connection.IsOnline || _lastRefresh?.Source == PriceDataSource.OfflineFallback || Nearby.IsOfflineResult;
        OnPropertyChanged(nameof(CanRefresh));
    }

    private void RefreshRequested()
    {
        if (CanRefresh)
        {
            LastTask = RefreshAsync(force: true);
        }
    }

    private void SetBusy(bool busy)
    {
        IsBusy = busy;
        OnPropertyChanged(nameof(CanRefresh));
    }

    private void CancelRefresh()
    {
        _refreshCancellation?.Cancel();
        _refreshCancellation = null;
    }

    private void OpenGroup(HomeGroupViewModel group)
    {
        LastTask = _navigator.OpenGroupAsync(group.GroupId);
    }

    private void OpenStation(HomeStationViewModel station)
    {
        // Kachel und Schaltfläche können dasselbe Antippen melden; geöffnet wird nur einmal.
        if (_isOpeningStation)
        {
            return;
        }

        _isOpeningStation = true;
        LastTask = OpenStationAsync(station);
    }

    private async Task OpenStationAsync(HomeStationViewModel station)
    {
        try
        {
            await _stationNavigator.OpenDetailAsync(station.Item).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Die Detailansicht konnte nicht geöffnet werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = DetailTexts.NotAvailable;
        }
        finally
        {
            _isOpeningStation = false;
        }
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

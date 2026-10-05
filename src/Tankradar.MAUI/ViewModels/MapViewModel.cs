using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services;
using Tankradar.MAUI.Services.Location;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.MAUI.Services.Search;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// ViewModel für den Bereich „Karte" (Suche): Umkreissuche am aktuellen Standort mit filterbarer und sortierbarer Ergebnisliste.
/// Der Standort wird nur auf Anforderung der Suche abgefragt und nie gespeichert oder protokolliert.
/// </summary>
public class MapViewModel : BaseViewModel
{
    private static readonly IReadOnlyList<StationListItem> NoStations = [];

    private readonly ISettingsService _settingsService;
    private readonly ILocationService _locationService;
    private readonly IFuelPriceService _priceService;
    private readonly IConnectionMonitor _connection;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MapViewModel> _logger;
    private readonly List<ChoiceOptionViewModel<ResultSortOrder>> _sortOptions;
    private AppSettings? _settings;
    private Task<AppSettings?>? _settingsLoad;
    private ResultSortOrder? _loadedDefaultSort;
    private StationSearchResult? _lastResult;
    private CancellationTokenSource? _searchCancellation;
    private bool _subscribed;
    private string _radiusText = SearchRadius.Default.ToString(System.Globalization.CultureInfo.InvariantCulture);
    private IReadOnlyList<StationListItem> _stations = NoStations;
    private IReadOnlyList<FuelFilterOptionViewModel> _fuelFilterOptions = [];
    private string? _statusMessage;
    private bool _isOffline;
    private string? _sourceNote;

    /// <summary>
    /// Erstellt das ViewModel mit dem Seitentitel „Karte".
    /// </summary>
    /// <param name="settingsService">Dienst zum Laden der Einstellungen.</param>
    /// <param name="locationService">Dienst zur Standortermittlung.</param>
    /// <param name="priceService">Preisdienst für die Umkreissuche.</param>
    /// <param name="connection">Die Verbindungserkennung (Offline-Hinweis).</param>
    /// <param name="timeProvider">Die Zeitquelle für Altersangaben.</param>
    /// <param name="logger">Logger (protokolliert nie Koordinaten).</param>
    public MapViewModel(
        ISettingsService settingsService,
        ILocationService locationService,
        IFuelPriceService priceService,
        IConnectionMonitor connection,
        TimeProvider timeProvider,
        ILogger<MapViewModel> logger)
    {
        _settingsService = settingsService;
        _locationService = locationService;
        _priceService = priceService;
        _connection = connection;
        _timeProvider = timeProvider;
        _logger = logger;
        Title = "Karte";

        _sortOptions = [];
        foreach (var value in Enum.GetValues<ResultSortOrder>())
        {
            _sortOptions.Add(new ChoiceOptionViewModel<ResultSortOrder>(value, SettingsTexts.GetLabel(value), OnSortSelected));
        }

        SortOptions = _sortOptions;
        SelectSort(AppSettings.CreateDefault().ResultSortOrder);
        FuelFilterOptions = CreateFilterOptions([]);
        SearchCommand = new Command(() => LastSearchTask = SearchAsync());
        LastSearchTask = Task.CompletedTask;
        LastSettingsTask = Task.CompletedTask;
        _isOffline = !_connection.IsOnline;
    }

    /// <summary>
    /// Die Eingabe des Suchradius in Kilometern (Standard 5).
    /// </summary>
    public string RadiusText
    {
        get => _radiusText;
        set => SetProperty(ref _radiusText, value);
    }

    /// <summary>
    /// Die aufbereitete Ergebnisliste; wird bei jeder Änderung vollständig ersetzt.
    /// </summary>
    public IReadOnlyList<StationListItem> Stations
    {
        get => _stations;
        private set
        {
            if (SetProperty(ref _stations, value))
            {
                OnPropertyChanged(nameof(HasResults));
                OnPropertyChanged(nameof(ShowEmptyState));
            }
        }
    }

    /// <summary>
    /// Die Optionen des Spritsortenfilters („Alle“ und die in den Einstellungen gewählten Sorten).
    /// </summary>
    public IReadOnlyList<FuelFilterOptionViewModel> FuelFilterOptions
    {
        get => _fuelFilterOptions;
        private set => SetProperty(ref _fuelFilterOptions, value);
    }

    /// <summary>
    /// Die Optionen der Sortierung.
    /// </summary>
    public IReadOnlyList<ChoiceOptionViewModel<ResultSortOrder>> SortOptions { get; }

    /// <summary>
    /// Befehl zum Auslösen der Suche.
    /// </summary>
    public ICommand SearchCommand { get; }

    /// <summary>
    /// Die zuletzt gestartete Suche (für Tests und Abwarten).
    /// </summary>
    public Task LastSearchTask { get; private set; }

    /// <summary>
    /// Der zuletzt gestartete Ladevorgang der Einstellungen (für Tests und Abwarten).
    /// </summary>
    public Task LastSettingsTask { get; private set; }

    /// <summary>
    /// Ein Hinweis oder eine Fehlermeldung zur Suche; <see langword="null"/>, wenn keine anliegt.
    /// </summary>
    public string? StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatusMessage));
                OnPropertyChanged(nameof(ShowEmptyState));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob eine <see cref="StatusMessage"/> anliegt.
    /// </summary>
    public bool HasStatusMessage => StatusMessage is { Length: > 0 };

    /// <summary>
    /// Gibt an, ob der Offline-Hinweis in der Kopfzeile angezeigt wird (keine Verbindung oder zuletzt bekannte Preise).
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
    /// Der Hinweis zur Datenherkunft (zuletzt bekannte Preise); <see langword="null"/>, wenn keiner nötig ist.
    /// </summary>
    public string? SourceNote
    {
        get => _sourceNote;
        private set
        {
            if (SetProperty(ref _sourceNote, value))
            {
                OnPropertyChanged(nameof(HasSourceNote));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob eine <see cref="SourceNote"/> anliegt.
    /// </summary>
    public bool HasSourceNote => SourceNote is { Length: > 0 };

    /// <summary>
    /// Gibt an, ob die Ergebnisliste Tankstellen enthält.
    /// </summary>
    public bool HasResults => Stations.Count > 0;

    /// <summary>
    /// Gibt an, ob der Leerzustand „Keine Tankstellen im Umkreis gefunden“ angezeigt wird (nur nach einer Suche ohne Fehlerhinweis).
    /// </summary>
    public bool ShowEmptyState => _lastResult is not null && Stations.Count == 0 && !HasStatusMessage;

    /// <summary>
    /// Lädt die Einstellungen und meldet sich beim Verbindungsmonitor an; ein Standort wird dabei nicht abgefragt.
    /// </summary>
    public override void OnAppearing()
    {
        if (!_subscribed)
        {
            _connection.ConnectionChanged += OnConnectionChanged;
            _subscribed = true;
        }

        UpdateOfflineState();
        LastSettingsTask = LoadSettingsAsync();
    }

    /// <summary>
    /// Meldet sich vom Verbindungsmonitor ab und bricht eine laufende Suche ab.
    /// </summary>
    public override void OnDisappearing()
    {
        if (_subscribed)
        {
            _connection.ConnectionChanged -= OnConnectionChanged;
            _subscribed = false;
        }

        CancelSearch();
        IsBusy = false;
    }

    /// <summary>
    /// Führt die Suche aus: Radius prüfen, Standort gemäß Einstellung ermitteln, Preisdienst abfragen, Liste aufbereiten.
    /// </summary>
    /// <returns>Ein Task, der nach der Suche abgeschlossen ist.</returns>
    public async Task SearchAsync()
    {
        CancelSearch();
        if (!SearchRadius.TryParse(RadiusText, out var radiusKm))
        {
            IsBusy = false;
            StatusMessage = SearchTexts.RadiusInvalid;
            return;
        }

        var cancellation = new CancellationTokenSource();
        _searchCancellation = cancellation;
        IsBusy = true;
        try
        {
            StatusMessage = null;
            await RunSearchAsync(radiusKm, cancellation.Token).ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // Eine neuere Suche oder das Verlassen der Seite hat diese abgelöst.
        }
        catch (Exception ex) when (IsCurrent(cancellation))
        {
            // Nur der Typ wird protokolliert: Ausnahmetexte könnten die Anfrage samt Position enthalten.
            _logger.LogWarning("Die Suche ist fehlgeschlagen ({ExceptionType}).", ex.GetType().Name);
            ClearResult();
            StatusMessage = SearchTexts.SearchFailed;
        }
        finally
        {
            if (IsCurrent(cancellation))
            {
                _searchCancellation = null;
                IsBusy = false;
                cancellation.Dispose();
            }
        }
    }

    /// <summary>
    /// Bereitet das zuletzt gelieferte Ergebnis mit dem gewählten Filter und der gewählten Sortierung neu auf (kein Standort- und kein API-Aufruf).
    /// </summary>
    public void ApplyFilterAndSort()
    {
        if (_lastResult is null || _settings is null)
        {
            Stations = NoStations;
            return;
        }

        var filter = FuelFilterOptions.FirstOrDefault(option => option.IsSelected)?.FuelType;
        var sort = _sortOptions.FirstOrDefault(option => option.IsSelected)?.Value ?? ResultSortOrder.Price;
        Stations = StationResultBuilder.Build(
            _lastResult.Stations,
            _settings.FuelTypes,
            filter,
            sort,
            _timeProvider.GetUtcNow().UtcDateTime);
    }

    private async Task RunSearchAsync(int radiusKm, CancellationToken token)
    {
        var settings = _settings ?? await LoadSettingsAsync().ConfigureAwait(true);
        token.ThrowIfCancellationRequested();
        if (settings is null)
        {
            ClearResult();
            StatusMessage = SettingsTexts.LoadFailed;
            return;
        }

        var location = await _locationService.GetCurrentLocationAsync(settings.GpsUsage, token).ConfigureAwait(true);
        token.ThrowIfCancellationRequested();
        if (location.Status != LocationStatus.Available || location.Position is null)
        {
            ClearResult();
            StatusMessage = SearchTexts.GetLocationMessage(location.Status == LocationStatus.Available ? LocationStatus.Unavailable : location.Status);
            return;
        }

        var fuelTypes = settings.FuelTypes.Where(selection => selection.IsSelected).Select(selection => selection.FuelType).ToList();
        var query = new StationSearchQuery(location.Position.Latitude, location.Position.Longitude, radiusKm, fuelTypes);
        var result = await _priceService.SearchNearbyAsync(query, token).ConfigureAwait(true);
        token.ThrowIfCancellationRequested();
        ApplyResult(result);
    }

    private void ApplyResult(StationSearchResult result)
    {
        _lastResult = result;
        ApplyFilterAndSort();
        StatusMessage = NullIfEmpty(SearchTexts.GetFailureMessage(result.Failure, result.Stations.Count > 0));
        SourceNote = result.Source == PriceDataSource.OfflineFallback && result.Stations.Count > 0 ? SearchTexts.OfflineFallbackNote : null;
        UpdateOfflineState();
        OnPropertyChanged(nameof(ShowEmptyState));
    }

    private bool IsCurrent(CancellationTokenSource cancellation)
    {
        return ReferenceEquals(_searchCancellation, cancellation);
    }

    private void CancelSearch()
    {
        var running = _searchCancellation;
        _searchCancellation = null;
        if (running is null)
        {
            return;
        }

        running.Cancel();
        running.Dispose();
    }

    private Task<AppSettings?> LoadSettingsAsync()
    {
        if (_settingsLoad is { IsCompleted: false } running)
        {
            return running;
        }

        return _settingsLoad = LoadSettingsCoreAsync();
    }

    private async Task<AppSettings?> LoadSettingsCoreAsync()
    {
        try
        {
            var settings = await _settingsService.LoadAsync().ConfigureAwait(true);
            _settings = settings;
            if (StatusMessage == SettingsTexts.LoadFailed)
            {
                StatusMessage = null;
            }

            if (_loadedDefaultSort != settings.ResultSortOrder)
            {
                _loadedDefaultSort = settings.ResultSortOrder;
                SelectSort(settings.ResultSortOrder);
            }

            var previousFilter = FuelFilterOptions.FirstOrDefault(option => option.IsSelected)?.FuelType;
            FuelFilterOptions = CreateFilterOptions(
                settings.FuelTypes.Where(selection => selection.IsSelected).Select(selection => selection.FuelType).ToList(),
                previousFilter);
            if (_lastResult is not null)
            {
                ApplyFilterAndSort();
            }

            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError("Die Einstellungen konnten nicht geladen werden ({ExceptionType}).", ex.GetType().Name);
            _settings = null;
            StatusMessage = SettingsTexts.LoadFailed;
            return null;
        }
    }

    private List<FuelFilterOptionViewModel> CreateFilterOptions(IReadOnlyList<FuelType> selectedFuelTypes, FuelType? selectedFilter = null)
    {
        var options = new List<FuelFilterOptionViewModel> { new(null, OnFilterSelected) };
        options.AddRange(selectedFuelTypes.Select(fuelType => new FuelFilterOptionViewModel(fuelType, OnFilterSelected)));
        var current = options.FirstOrDefault(option => option.FuelType == selectedFilter) ?? options[0];
        current.SetSelectedSilently(true);
        return options;
    }

    private void SelectSort(ResultSortOrder sortOrder)
    {
        foreach (var option in _sortOptions)
        {
            option.SetSelectedSilently(option.Value == sortOrder);
        }
    }

    private void OnFilterSelected(FuelFilterOptionViewModel selected)
    {
        foreach (var option in FuelFilterOptions.Where(option => !ReferenceEquals(option, selected)))
        {
            option.SetSelectedSilently(false);
        }

        ApplyFilterAndSort();
    }

    private void OnSortSelected(ChoiceOptionViewModel<ResultSortOrder> selected)
    {
        foreach (var option in _sortOptions.Where(option => !ReferenceEquals(option, selected)))
        {
            option.SetSelectedSilently(false);
        }

        ApplyFilterAndSort();
    }

    private void OnConnectionChanged(object? sender, bool online)
    {
        UpdateOfflineState();
    }

    private void UpdateOfflineState()
    {
        IsOffline = !_connection.IsOnline || _lastResult?.Source == PriceDataSource.OfflineFallback;
    }

    private void ClearResult()
    {
        _lastResult = null;
        SourceNote = null;
        Stations = NoStations;
        UpdateOfflineState();
        OnPropertyChanged(nameof(ShowEmptyState));
    }

    private static string? NullIfEmpty(string value)
    {
        return value.Length == 0 ? null : value;
    }
}

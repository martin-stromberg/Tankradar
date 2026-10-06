using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services;
using Tankradar.MAUI.Services.Geocoding;
using Tankradar.MAUI.Services.Location;
using Tankradar.MAUI.Services.Navigation;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.MAUI.Services.Search;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// ViewModel für den Bereich „Karte" (Suche): Umkreissuche am aktuellen Standort oder rund um eine eingegebene Adresse mit filterbarer und sortierbarer Ergebnisliste.
/// Der Standort bzw. die aufgelöste Adresse wird nur auf Anforderung der Suche ermittelt und nie gespeichert oder protokolliert; die Adresseingabe wird nicht gespeichert.
/// </summary>
public class MapViewModel : BaseViewModel
{
    /// <summary>
    /// Anzahl der Tankstellen, die auf einmal in der Liste dargestellt werden (weitere über „Weitere anzeigen“).
    /// </summary>
    public const int PageSize = 25;

    private static readonly IReadOnlyList<StationListItem> NoStations = [];

    private readonly ISettingsService _settingsService;
    private readonly ILocationService _locationService;
    private readonly IGeocodingService _geocodingService;
    private readonly IFuelPriceService _priceService;
    private readonly IConnectionMonitor _connection;
    private readonly TimeProvider _timeProvider;
    private readonly IStationNavigator _navigator;
    private readonly ILogger<MapViewModel> _logger;
    private readonly List<ChoiceOptionViewModel<ResultSortOrder>> _sortOptions;
    private AppSettings? _settings;
    private Task<AppSettings?>? _settingsLoad;
    private ResultSortOrder? _loadedDefaultSort;
    private StationSearchResult? _lastResult;
    private CancellationTokenSource? _searchCancellation;
    private bool _subscribed;
    private bool _isOpeningStation;
    private int _radiusKm = SearchRadius.Default;
    private SearchMode _searchMode = SearchMode.CurrentLocation;
    private string _addressText = string.Empty;
    private string? _resolvedPlace;
    private readonly List<ChoiceOptionViewModel<SearchMode>> _modeOptions;
    private readonly List<RadiusOptionViewModel> _radiusOptions;
    private IReadOnlyList<StationListItem> _allStations = NoStations;
    private int _visibleCount = PageSize;
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
    /// <param name="geocodingService">Dienst zur Umwandlung einer Adresse in eine Position.</param>
    /// <param name="priceService">Preisdienst für die Umkreissuche.</param>
    /// <param name="connection">Die Verbindungserkennung (Offline-Hinweis).</param>
    /// <param name="timeProvider">Die Zeitquelle für Altersangaben.</param>
    /// <param name="navigator">Die Navigation zur Detailansicht einer Tankstelle.</param>
    /// <param name="logger">Logger (protokolliert nie Koordinaten).</param>
    public MapViewModel(
        ISettingsService settingsService,
        ILocationService locationService,
        IGeocodingService geocodingService,
        IFuelPriceService priceService,
        IConnectionMonitor connection,
        TimeProvider timeProvider,
        IStationNavigator navigator,
        ILogger<MapViewModel> logger)
    {
        _settingsService = settingsService;
        _locationService = locationService;
        _geocodingService = geocodingService;
        _priceService = priceService;
        _connection = connection;
        _timeProvider = timeProvider;
        _navigator = navigator;
        _logger = logger;
        Title = "Karte";

        _sortOptions = [];
        foreach (var value in Enum.GetValues<ResultSortOrder>())
        {
            _sortOptions.Add(new ChoiceOptionViewModel<ResultSortOrder>(value, SettingsTexts.GetLabel(value), OnSortSelected));
        }

        SortOptions = _sortOptions;
        _modeOptions = [];
        foreach (var value in Enum.GetValues<SearchMode>())
        {
            _modeOptions.Add(new ChoiceOptionViewModel<SearchMode>(value, SearchTexts.GetModeLabel(value), OnModeSelected));
        }

        ModeOptions = _modeOptions;
        SyncModeSelection();
        ClearAddressCommand = new Command(ClearAddress);
        SelectSort(AppSettings.CreateDefault().ResultSortOrder);
        _radiusOptions = SearchRadius.Steps.Select(step => new RadiusOptionViewModel(step, OnRadiusSelected)).ToList();
        RadiusOptions = _radiusOptions;
        SyncRadiusSelection();
        ShowMoreCommand = new Command(ShowMore);
        OpenStationCommand = new Command<StationListItem>(OpenStation);
        FuelFilterOptions = CreateFilterOptions([]);
        SearchCommand = new Command(() => LastSearchTask = SearchAsync());
        LastSearchTask = Task.CompletedTask;
        LastSettingsTask = Task.CompletedTask;
        _isOffline = !_connection.IsOnline;
    }

    /// <summary>
    /// Die wählbaren Sucharten („Aktueller Standort“, „Adresse, Ort oder PLZ“).
    /// </summary>
    public IReadOnlyList<ChoiceOptionViewModel<SearchMode>> ModeOptions { get; }

    /// <summary>
    /// Gibt an, ob rund um eine eingegebene Adresse gesucht wird (Eingabefeld und Quellenangabe sichtbar).
    /// </summary>
    public bool IsAddressMode => _searchMode == SearchMode.Address;

    /// <summary>
    /// Die Eingabe im Adressfeld; sie wird nur im Arbeitsspeicher gehalten und nie gespeichert.
    /// </summary>
    public string AddressText
    {
        get => _addressText;
        set
        {
            if (SetProperty(ref _addressText, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(HasAddressText));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob das Adressfeld Text enthält (Schaltfläche zum Löschen sichtbar).
    /// </summary>
    public bool HasAddressText => _addressText.Length > 0;

    /// <summary>
    /// Befehl zum Löschen der Adresseingabe.
    /// </summary>
    public ICommand ClearAddressCommand { get; }

    /// <summary>
    /// Die Quellenangabe für die Geodaten (im Adressmodus anzuzeigen).
    /// </summary>
    public string AttributionText => SearchTexts.OsmAttribution;

    /// <summary>
    /// Der Hinweis auf den Ort, rund um den gesucht wurde („Suche rund um: …“); <see langword="null"/>, wenn nicht nach Adresse gesucht wurde.
    /// </summary>
    public string? ResolvedPlace
    {
        get => _resolvedPlace;
        private set
        {
            if (SetProperty(ref _resolvedPlace, value))
            {
                OnPropertyChanged(nameof(HasResolvedPlace));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob ein <see cref="ResolvedPlace"/> anliegt.
    /// </summary>
    public bool HasResolvedPlace => ResolvedPlace is { Length: > 0 };

    /// <summary>
    /// Der gewählte Suchradius in Kilometern (Standard 5); die Oberfläche setzt ihn über die Radiusstufen (Chips).
    /// </summary>
    public int RadiusKm
    {
        get => _radiusKm;
        set
        {
            if (SetProperty(ref _radiusKm, value))
            {
                SyncRadiusSelection();
            }
        }
    }

    /// <summary>
    /// Die wählbaren Radiusstufen (Chips) innerhalb von 1 bis 25 km.
    /// </summary>
    public IReadOnlyList<RadiusOptionViewModel> RadiusOptions { get; }

    /// <summary>
    /// Befehl zum Anzeigen weiterer Tankstellen der Ergebnisliste.
    /// </summary>
    public ICommand ShowMoreCommand { get; }

    /// <summary>
    /// Befehl zum Öffnen der Detailansicht einer Tankstelle aus der Ergebnisliste (Parameter: die Tankstelle).
    /// </summary>
    public ICommand OpenStationCommand { get; }

    /// <summary>
    /// Die Gesamtzahl der Tankstellen des aktuellen Ergebnisses nach Filter (angezeigt werden davon höchstens die ersten Seiten).
    /// </summary>
    public int TotalStationCount => _allStations.Count;

    /// <summary>
    /// Gibt an, ob weitere Tankstellen zum Nachladen vorhanden sind.
    /// </summary>
    public bool HasMore => _allStations.Count > _stations.Count;

    /// <summary>
    /// Die Beschriftung der Schaltfläche „Weitere anzeigen“.
    /// </summary>
    public string ShowMoreText
    {
        get { return SearchTexts.FormatShowMore(_allStations.Count - _stations.Count); }
    }

    /// <summary>
    /// Die dargestellten Tankstellen der aufbereiteten Ergebnisliste (höchstens die ersten <see cref="PageSize"/>, je „Weitere anzeigen“ mehr); wird bei jeder Änderung vollständig ersetzt.
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
        if (!SearchRadius.IsValid(_radiusKm))
        {
            IsBusy = false;
            StatusMessage = SearchTexts.RadiusInvalid;
            return;
        }

        // Die Eingabe wird geprüft, bevor irgendetwas an den externen Dienst geht.
        var addressError = _searchMode == SearchMode.Address ? AddressInput.Validate(_addressText, out _) : AddressInputError.None;
        if (addressError != AddressInputError.None)
        {
            IsBusy = false;
            ClearResult();
            StatusMessage = SearchTexts.GetAddressInputMessage(addressError);
            return;
        }

        var cancellation = new CancellationTokenSource();
        _searchCancellation = cancellation;
        IsBusy = true;
        try
        {
            StatusMessage = null;
            await RunSearchAsync(_radiusKm, cancellation.Token).ConfigureAwait(true);
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
        ApplyFilterAndSort(resetPaging: true);
    }

    /// <summary>
    /// Zeigt die nächste Seite der Ergebnisliste an.
    /// </summary>
    public void ShowMore()
    {
        _visibleCount += PageSize;
        PublishVisible();
    }

    private void OpenStation(StationListItem? station)
    {
        // Karte und Schaltfläche der Zeile können dasselbe Antippen melden; geöffnet wird nur einmal.
        if (station is null || _isOpeningStation)
        {
            return;
        }

        _isOpeningStation = true;
        _ = OpenStationAsync(station);
    }

    private async Task OpenStationAsync(StationListItem station)
    {
        try
        {
            await _navigator.OpenDetailAsync(station).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Die Detailansicht konnte nicht geöffnet werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = DetailTexts.NotAvailable;
        }
        finally
        {
            _isOpeningStation = false;
        }
    }

    private void ApplyFilterAndSort(bool resetPaging)
    {
        if (resetPaging)
        {
            _visibleCount = PageSize;
        }

        if (_lastResult is null || _settings is null)
        {
            _allStations = NoStations;
            PublishVisible();
            return;
        }

        var filter = FuelFilterOptions.FirstOrDefault(option => option.IsSelected)?.FuelType;
        var sort = _sortOptions.FirstOrDefault(option => option.IsSelected)?.Value ?? ResultSortOrder.Price;
        _allStations = StationResultBuilder.Build(
            _lastResult.Stations,
            _settings.FuelTypes,
            filter,
            sort,
            _timeProvider.GetUtcNow().UtcDateTime);
        PublishVisible();
    }

    private void PublishVisible()
    {
        Stations = _allStations.Count <= _visibleCount ? _allStations : _allStations.Take(_visibleCount).ToList();

        // Unabhängig davon, ob sich die dargestellte Liste geändert hat: Gesamtzahl und Rest hängen auch von der gefilterten Gesamtliste ab.
        OnPropertyChanged(nameof(TotalStationCount));
        OnPropertyChanged(nameof(HasMore));
        OnPropertyChanged(nameof(ShowMoreText));
    }

    private void SyncRadiusSelection()
    {
        foreach (var option in _radiusOptions)
        {
            option.SetSelectedSilently(option.RadiusKm == _radiusKm);
        }
    }

    private void OnRadiusSelected(RadiusOptionViewModel selected)
    {
        RadiusKm = selected.RadiusKm;
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

        var position = _searchMode == SearchMode.Address
            ? await ResolveAddressAsync(token).ConfigureAwait(true)
            : await ResolveCurrentLocationAsync(settings, token).ConfigureAwait(true);
        token.ThrowIfCancellationRequested();
        if (position is null)
        {
            return;
        }

        var fuelTypes = settings.FuelTypes.Where(selection => selection.IsSelected).Select(selection => selection.FuelType).ToList();
        var query = new StationSearchQuery(position.Latitude, position.Longitude, radiusKm, fuelTypes);
        var result = await _priceService.SearchNearbyAsync(query, token).ConfigureAwait(true);
        token.ThrowIfCancellationRequested();
        ApplyResult(result);
    }

    private async Task<GeoPosition?> ResolveCurrentLocationAsync(AppSettings settings, CancellationToken token)
    {
        var location = await _locationService.GetCurrentLocationAsync(settings.GpsUsage, token).ConfigureAwait(true);
        token.ThrowIfCancellationRequested();
        if (location.Status != LocationStatus.Available || location.Position is null)
        {
            ClearResult();
            StatusMessage = SearchTexts.GetLocationMessage(location.Status == LocationStatus.Available ? LocationStatus.Unavailable : location.Status);
            return null;
        }

        return location.Position;
    }

    private async Task<GeoPosition?> ResolveAddressAsync(CancellationToken token)
    {
        if (!_connection.IsOnline)
        {
            ClearResult();
            StatusMessage = SearchTexts.AddressOffline;
            return null;
        }

        var result = await _geocodingService.ResolveAsync(_addressText, token).ConfigureAwait(true);
        token.ThrowIfCancellationRequested();
        if (result.Status != GeocodingStatus.Found || result.Position is null)
        {
            ClearResult();
            StatusMessage = SearchTexts.GetGeocodingMessage(result.Status == GeocodingStatus.Found ? GeocodingStatus.InvalidResponse : result.Status, _connection.IsOnline);
            return null;
        }

        // Nur der Anzeigename des Ortes wird für die Oberfläche gehalten; die Position verlässt diese Methode nur als Rückgabewert.
        AddressInput.Validate(_addressText, out var normalized);
        ResolvedPlace = SearchTexts.FormatResolvedPlace(result.PlaceName ?? normalized);
        return result.Position;
    }

    private void OnModeSelected(ChoiceOptionViewModel<SearchMode> selected)
    {
        if (_searchMode == selected.Value)
        {
            return;
        }

        CancelSearch();
        IsBusy = false;
        _searchMode = selected.Value;
        SyncModeSelection();
        OnPropertyChanged(nameof(IsAddressMode));
        ClearResult();
        StatusMessage = null;
    }

    private void SyncModeSelection()
    {
        foreach (var option in _modeOptions)
        {
            option.SetSelectedSilently(option.Value == _searchMode);
        }
    }

    private void ClearAddress()
    {
        AddressText = string.Empty;
        StatusMessage = null;
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
                ApplyFilterAndSort(resetPaging: false);
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
        ResolvedPlace = null;
        _allStations = NoStations;
        _visibleCount = PageSize;
        Stations = NoStations;
        UpdateOfflineState();
        OnPropertyChanged(nameof(ShowEmptyState));
    }

    private static string? NullIfEmpty(string value)
    {
        return value.Length == 0 ? null : value;
    }
}

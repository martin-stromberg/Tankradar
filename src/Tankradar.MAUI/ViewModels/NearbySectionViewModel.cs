using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Favorites;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Navigation;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.MAUI.Services.Search;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Der Bereich „In der Nähe“ der Startseite: Tankstellen im festen Umkreis um den Standort, nach Entfernung, filterbar nach Spritsorte, rein informativ (ohne Favoritenaktion).
/// Sichtbar nur, wenn der Bereich eingeschaltet ist und die Standortnutzung nicht „Nie“ ist. Der Standort wird nicht hier ermittelt, sondern von der Startseite übergeben,
/// nur im Arbeitsspeicher gehalten und nie gespeichert oder protokolliert.
/// </summary>
public class NearbySectionViewModel : BaseViewModel
{
    /// <summary>
    /// Höchstzahl der angezeigten Einträge.
    /// </summary>
    public const int MaxItems = 10;

    private static readonly IReadOnlyList<HomeStationViewModel> NoEntries = [];

    private readonly IFuelPriceService _priceService;
    private readonly IStationNavigator _navigator;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<NearbySectionViewModel> _logger;
    private IReadOnlyList<StationInfo> _stations = [];
    private IReadOnlyList<FuelTypeSelection> _fuelTypes = [];
    private IReadOnlyList<HomeStationViewModel> _entries = NoEntries;
    private IReadOnlyList<FuelFilterOptionViewModel> _filterOptions = [];
    private bool _isVisible;
    private bool _loaded;
    private bool _isOpeningStation;
    private string? _statusMessage;
    private PriceDataSource? _source;

    /// <summary>
    /// Erstellt den Bereich.
    /// </summary>
    /// <param name="priceService">Der Preisdienst für die Umkreissuche.</param>
    /// <param name="navigator">Öffnet die Detailansicht einer Tankstelle.</param>
    /// <param name="timeProvider">Die Zeitquelle für Altersangaben.</param>
    /// <param name="logger">Logger (protokolliert nie Koordinaten).</param>
    public NearbySectionViewModel(IFuelPriceService priceService, IStationNavigator navigator, TimeProvider timeProvider, ILogger<NearbySectionViewModel> logger)
    {
        _priceService = priceService;
        _navigator = navigator;
        _timeProvider = timeProvider;
        _logger = logger;
        Title = FavoritesTexts.NearbyHeading;
        LastOpenTask = Task.CompletedTask;
    }

    /// <summary>
    /// Der zuletzt gestartete Öffnungsvorgang (für Tests und Abwarten).
    /// </summary>
    public Task LastOpenTask { get; private set; }

    /// <summary>
    /// Die Überschrift des Bereichs.
    /// </summary>
    public string Heading => FavoritesTexts.NearbyHeading;

    /// <summary>
    /// Die Erläuterung des Bereichs.
    /// </summary>
    public string Description => FavoritesTexts.NearbyDescription;

    /// <summary>
    /// Gibt an, ob der Bereich angezeigt wird (eingeschaltet und Standortnutzung erlaubt).
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        private set => SetProperty(ref _isVisible, value);
    }

    /// <summary>
    /// Die Tankstellen nach Entfernung, höchstens <see cref="MaxItems"/>.
    /// </summary>
    public IReadOnlyList<HomeStationViewModel> Entries
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
    /// Gibt an, ob Einträge angezeigt werden.
    /// </summary>
    public bool HasEntries => Entries.Count > 0;

    /// <summary>
    /// Die Filterchips „Alle“ und je gewählte Sorte.
    /// </summary>
    public IReadOnlyList<FuelFilterOptionViewModel> FilterOptions
    {
        get => _filterOptions;
        private set => SetProperty(ref _filterOptions, value);
    }

    /// <summary>
    /// Eine Meldung (z. B. Standort nicht verfügbar); <see langword="null"/>, wenn keine anliegt.
    /// </summary>
    public string? StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatusMessage));
                OnPropertyChanged(nameof(ShowEmptyHint));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob eine <see cref="StatusMessage"/> anliegt.
    /// </summary>
    public bool HasStatusMessage => StatusMessage is { Length: > 0 };

    /// <summary>
    /// Gibt an, ob der Hinweis „Keine Tankstellen in der Nähe“ angezeigt wird.
    /// </summary>
    public bool ShowEmptyHint => IsVisible && _loaded && !HasStatusMessage && Entries.Count == 0;

    /// <summary>
    /// Der Hinweis „Keine Tankstellen in der Nähe“.
    /// </summary>
    public string EmptyHintText => FavoritesTexts.NearbyEmpty;

    /// <summary>
    /// Gibt an, ob das letzte Ergebnis ein Offline-Stand (zuletzt bekannte Preise) war.
    /// </summary>
    public bool IsOfflineResult => _source == PriceDataSource.OfflineFallback;

    /// <summary>
    /// Lädt den Bereich: blendet ihn aus, wenn er nicht sichtbar sein darf, zeigt ohne Standort die passende Meldung, sonst die Tankstellen in der Nähe.
    /// </summary>
    /// <param name="settings">Die Einstellungen (Sichtbarkeit, Sorten).</param>
    /// <param name="location">Das Ergebnis der Standortermittlung; <see langword="null"/>, wenn nicht ermittelt wurde.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Ein Task, der nach dem Laden abgeschlossen ist.</returns>
    public async Task LoadAsync(AppSettings settings, LocationResult? location, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!settings.ShowNearby || !settings.GpsUsage.AllowsLocation())
        {
            Hide();
            return;
        }

        IsVisible = true;
        _fuelTypes = settings.FuelTypes;
        var selected = settings.FuelTypes.Where(selection => selection.IsSelected).Select(selection => selection.FuelType).ToList();
        FilterOptions = CreateFilterOptions(selected);

        if (location?.Position is not { } position)
        {
            _stations = [];
            _source = null;
            Entries = NoEntries;
            StatusMessage = SearchTexts.GetLocationMessage(location is null || location.Status == LocationStatus.Available ? LocationStatus.Unavailable : location.Status);
            _loaded = true;
            OnPropertyChanged(nameof(IsOfflineResult));
            OnPropertyChanged(nameof(ShowEmptyHint));
            return;
        }

        if (selected.Count == 0)
        {
            _stations = [];
            _source = null;
            StatusMessage = null;
            Entries = NoEntries;
            _loaded = true;
            OnPropertyChanged(nameof(IsOfflineResult));
            return;
        }

        try
        {
            var query = new StationSearchQuery(position.Latitude, position.Longitude, SearchRadius.Default, selected);
            var result = await _priceService.SearchNearbyAsync(query, cancellationToken).ConfigureAwait(true);
            cancellationToken.ThrowIfCancellationRequested();
            _stations = result.Stations;
            _source = result.Source;
            StatusMessage = NullIfEmpty(SearchTexts.GetFailureMessage(result.Failure, result.Stations.Count > 0));
            ApplyFilter();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Der Bereich „In der Nähe“ konnte nicht geladen werden ({ExceptionType}).", ex.GetType().Name);
            _stations = [];
            _source = null;
            Entries = NoEntries;
            StatusMessage = SearchTexts.SearchFailed;
        }

        _loaded = true;
        OnPropertyChanged(nameof(IsOfflineResult));
        OnPropertyChanged(nameof(ShowEmptyHint));
    }

    private static string? NullIfEmpty(string text)
    {
        return text.Length == 0 ? null : text;
    }

    private void Hide()
    {
        IsVisible = false;
        _loaded = false;
        _stations = [];
        _source = null;
        Entries = NoEntries;
        StatusMessage = null;
        FilterOptions = [];
        OnPropertyChanged(nameof(IsOfflineResult));
        OnPropertyChanged(nameof(ShowEmptyHint));
    }

    private List<FuelFilterOptionViewModel> CreateFilterOptions(IReadOnlyList<FuelType> selectedFuelTypes)
    {
        var previous = FilterOptions.FirstOrDefault(option => option.IsSelected)?.FuelType;
        var options = new List<FuelFilterOptionViewModel> { new(null, OnFilterSelected) };
        options.AddRange(selectedFuelTypes.Select(fuelType => new FuelFilterOptionViewModel(fuelType, OnFilterSelected)));
        var current = options.FirstOrDefault(option => option.FuelType == previous) ?? options[0];
        current.SetSelectedSilently(true);
        return options;
    }

    private void OnFilterSelected(FuelFilterOptionViewModel selected)
    {
        foreach (var option in FilterOptions.Where(option => !ReferenceEquals(option, selected)))
        {
            option.SetSelectedSilently(false);
        }

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filter = FilterOptions.FirstOrDefault(option => option.IsSelected)?.FuelType;
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var items = StationResultBuilder.Build(_stations, _fuelTypes, filter, ResultSortOrder.Distance, now).Take(MaxItems);
        Entries = items.Select(item => new HomeStationViewModel(new HomeStation(item, null, FavoritePriority.None), "Nearby.Station", OpenStation)).ToList();
    }

    private void OpenStation(HomeStationViewModel station)
    {
        // Kachel und Schaltfläche können dasselbe Antippen melden; geöffnet wird nur einmal.
        if (_isOpeningStation)
        {
            return;
        }

        _isOpeningStation = true;
        LastOpenTask = OpenStationAsync(station);
    }

    private async Task OpenStationAsync(HomeStationViewModel station)
    {
        try
        {
            await _navigator.OpenDetailAsync(station.Item).ConfigureAwait(true);
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
}

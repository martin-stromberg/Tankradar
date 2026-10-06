using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services;
using Tankradar.MAUI.Services.Navigation;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.MAUI.Services.Search;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// ViewModel der Tankstellen-Detailansicht: zeigt zunächst die Angaben aus der Ergebnisliste, ruft dann die Details ab (oder verwendet den lokalen Stand),
/// zeigt ohne Verbindung die zuletzt bekannten Daten mit Altersangabe und Offline-Hinweis und aktualisiert veraltete Preise automatisch, sobald die Verbindung wiederhergestellt wird.
/// </summary>
public class StationDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly ISettingsService _settingsService;
    private readonly IFuelPriceService _priceService;
    private readonly IConnectionMonitor _connection;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<StationDetailViewModel> _logger;
    private StationListItem? _origin;
    private StationInfo? _station;
    private StationDetailResult? _lastResult;
    private AppSettings? _settings;
    private CancellationTokenSource? _loadCancellation;
    private StationDetailItem? _detail;
    private string? _statusMessage;
    private string? _sourceNote;
    private bool _isOffline;
    private bool _subscribed;

    /// <summary>
    /// Erstellt das ViewModel mit dem Seitentitel „Stationsdetails“.
    /// </summary>
    /// <param name="settingsService">Dienst zum Laden der Einstellungen (aktivierte Spritsorten).</param>
    /// <param name="priceService">Preisdienst für die Detailabfrage.</param>
    /// <param name="connection">Die Verbindungserkennung (Offline-Hinweis, automatische Aktualisierung).</param>
    /// <param name="timeProvider">Die Zeitquelle für Altersangaben.</param>
    /// <param name="logger">Logger (protokolliert nie Koordinaten).</param>
    /// <param name="favorites">Die Favoritengruppen der Tankstelle (Karte „Favoritengruppen“).</param>
    public StationDetailViewModel(
        ISettingsService settingsService,
        IFuelPriceService priceService,
        IConnectionMonitor connection,
        TimeProvider timeProvider,
        ILogger<StationDetailViewModel> logger,
        StationFavoritesViewModel favorites)
    {
        Favorites = favorites;
        _settingsService = settingsService;
        _priceService = priceService;
        _connection = connection;
        _timeProvider = timeProvider;
        _logger = logger;
        Title = DetailTexts.PageTitle;
        _isOffline = !_connection.IsOnline;
        RefreshCommand = new Command(() => LastLoadTask = LoadAsync());
        LastLoadTask = Task.CompletedTask;
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IsBusy))
            {
                OnPropertyChanged(nameof(CanRefresh));
            }
        };
    }

    /// <summary>
    /// Die Favoritengruppen der Tankstelle (Zuordnen und Entfernen).
    /// </summary>
    public StationFavoritesViewModel Favorites { get; }

    /// <summary>
    /// Die aufbereiteten Angaben der Tankstelle; <see langword="null"/>, solange nichts vorliegt.
    /// </summary>
    public StationDetailItem? Detail
    {
        get => _detail;
        private set
        {
            if (SetProperty(ref _detail, value))
            {
                OnPropertyChanged(nameof(HasDetail));
                OnPropertyChanged(nameof(ShowNoPrices));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob Angaben zur Tankstelle vorliegen.
    /// </summary>
    public bool HasDetail => Detail is not null;

    /// <summary>
    /// Gibt an, ob der Hinweis „keine Preise für die aktivierten Sorten“ angezeigt wird.
    /// </summary>
    public bool ShowNoPrices => Detail is { HasPrices: false };

    /// <summary>
    /// Befehl zum Aktualisieren der Preise.
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// Gibt an, ob „Preise aktualisieren“ bedienbar ist: nicht während eines Abrufs und nur mit Verbindung (ohne Verbindung bleibt die Schaltfläche deaktiviert; der Offline-Hinweis erklärt, warum).
    /// </summary>
    public bool CanRefresh => !IsBusy && _connection.IsOnline;

    /// <summary>
    /// Der zuletzt gestartete Ladevorgang (für Tests und Abwarten).
    /// </summary>
    public Task LastLoadTask { get; private set; }

    /// <summary>
    /// Ein Hinweis oder eine Fehlermeldung zum Laden; <see langword="null"/>, wenn keine anliegt.
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
    /// Der Hinweis zur Datenherkunft (zuletzt bekannte Daten); <see langword="null"/>, wenn keiner nötig ist.
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
    /// Gibt an, ob der Offline-Hinweis in der Kopfzeile angezeigt wird (keine Verbindung oder zuletzt bekannte Daten).
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
    /// Übernimmt die Tankstelle aus der Ergebnisliste (Navigationsparameter) und zeigt deren Angaben sofort an.
    /// </summary>
    /// <param name="query">Die Navigationsparameter; erwartet wird <see cref="ShellStationNavigator.StationParameter"/>.</param>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue(ShellStationNavigator.StationParameter, out var value) && value is StationListItem origin)
        {
            Show(origin);
        }
    }

    /// <summary>
    /// Zeigt die Angaben der Tankstelle aus der Ergebnisliste als Anfangsanzeige.
    /// </summary>
    /// <param name="origin">Die Tankstelle aus der Ergebnisliste.</param>
    public void Show(StationListItem origin)
    {
        ArgumentNullException.ThrowIfNull(origin);
        _origin = origin;
        _station = null;
        _lastResult = null;
        _ = Favorites.SetStationAsync(origin.Id);
        Detail = new StationDetailItem(
            origin.Id,
            origin.Name,
            string.Empty,
            origin.AddressText,
            origin.DistanceText,
            origin.OpeningStatusText,
            origin.PriceLines,
            origin.HasUnconfirmedPrice,
            origin.IsAutomatedStation,
            [],
            string.Empty);
    }

    /// <summary>
    /// Meldet sich beim Verbindungsmonitor an und ruft die Details der Tankstelle ab.
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
        Favorites.OnAppearing();
        LastLoadTask = LoadAsync();
    }

    /// <summary>
    /// Meldet sich vom Verbindungsmonitor ab und bricht einen laufenden Abruf ab.
    /// </summary>
    public override void OnDisappearing()
    {
        if (_subscribed)
        {
            _connection.ConnectionChanged -= OnConnectionChanged;
            _connection.ConnectionRestored -= OnConnectionRestored;
            _subscribed = false;
        }

        CancelLoad();
        IsBusy = false;
    }

    /// <summary>
    /// Ruft die Details der Tankstelle ab (bzw. verwendet den lokalen Stand) und bereitet sie auf.
    /// </summary>
    /// <returns>Ein Task, der nach dem Laden abgeschlossen ist.</returns>
    public async Task LoadAsync()
    {
        if (_origin is null)
        {
            return;
        }

        CancelLoad();
        var cancellation = new CancellationTokenSource();
        _loadCancellation = cancellation;
        var token = cancellation.Token;
        IsBusy = true;
        try
        {
            var settings = _settings ?? await LoadSettingsAsync().ConfigureAwait(true);
            token.ThrowIfCancellationRequested();
            if (settings is null)
            {
                StatusMessage = SettingsTexts.LoadFailed;
                return;
            }

            var result = await _priceService.GetStationDetailAsync(_origin.Id, token).ConfigureAwait(true);
            token.ThrowIfCancellationRequested();
            Apply(result, settings);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested || !IsCurrent(cancellation))
        {
            // Ein neuerer Abruf oder das Verlassen der Seite hat diesen abgelöst.
        }
        catch (Exception ex) when (IsCurrent(cancellation))
        {
            // Nur der Typ wird protokolliert: Ausnahmetexte könnten die Anfrage samt Schlüssel enthalten.
            _logger.LogWarning("Die Tankstellendetails konnten nicht geladen werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = DetailTexts.NotAvailable;
        }
        finally
        {
            if (IsCurrent(cancellation))
            {
                _loadCancellation = null;
                IsBusy = false;
                cancellation.Dispose();
            }
        }
    }

    private void Apply(StationDetailResult result, AppSettings settings)
    {
        _lastResult = result;
        if (result.Station is { } station)
        {
            _station = station;
            RebuildDetail(settings);
        }

        var hasData = result.Station is not null || Detail is not null;
        StatusMessage = result.Station is null && result.Failure == PriceFailure.None
            ? DetailTexts.NotAvailable
            : NullIfEmpty(SearchTexts.GetFailureMessage(result.Failure, hasData));
        SourceNote = result.Source == PriceDataSource.OfflineFallback && hasData ? DetailTexts.LastKnownNote : null;
        UpdateOfflineState();
    }

    private void RebuildDetail(AppSettings settings)
    {
        if (_station is null)
        {
            return;
        }

        Detail = StationDetailBuilder.Build(_station, settings.FuelTypes, _origin?.DistanceKm, _timeProvider.GetUtcNow().UtcDateTime);
    }

    private async Task<AppSettings?> LoadSettingsAsync()
    {
        try
        {
            _settings = await _settingsService.LoadAsync().ConfigureAwait(true);
            return _settings;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError("Die Einstellungen konnten nicht geladen werden ({ExceptionType}).", ex.GetType().Name);
            return null;
        }
    }

    private void OnConnectionChanged(object? sender, bool online)
    {
        UpdateOfflineState();
    }

    private void OnConnectionRestored(object? sender, EventArgs e)
    {
        // Nach Wiederverbindung werden nur veraltete Angaben (Offline-Stand oder Preise ab 60 Minuten) neu abgefragt.
        UpdateOfflineState();
        var hasStalePrice = Detail is { HasStalePrice: true };
        if (_origin is not null && (_lastResult?.Source == PriceDataSource.OfflineFallback || hasStalePrice))
        {
            LastLoadTask = LoadAsync();
        }
    }

    private void UpdateOfflineState()
    {
        IsOffline = !_connection.IsOnline || _lastResult?.Source == PriceDataSource.OfflineFallback;
        OnPropertyChanged(nameof(CanRefresh));
    }

    private bool IsCurrent(CancellationTokenSource cancellation)
    {
        return ReferenceEquals(_loadCancellation, cancellation);
    }

    private void CancelLoad()
    {
        var running = _loadCancellation;
        _loadCancellation = null;
        if (running is null)
        {
            return;
        }

        running.Cancel();
        running.Dispose();
    }

    private static string? NullIfEmpty(string value)
    {
        return value.Length == 0 ? null : value;
    }
}

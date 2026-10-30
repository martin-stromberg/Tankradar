using System.ComponentModel;
using System.Windows.Input;
using Tankradar.MAUI.Models.Map;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Map;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Nachladen des sichtbaren Kartenausschnitts: Die Schaltfläche „Tankstellen in diesem Bereich laden“ erscheint in den Sucharten Standort und Adresse, sobald der Anwender
/// den Ausschnitt gegenüber der letzten Suche verschoben oder gezoomt hat. Abgefragt wird erst beim Betätigen, höchstens einmal je <see cref="AreaReloadMinInterval"/>.
/// </summary>
public partial class MapViewModel
{
    /// <summary>
    /// Mindestabstand zwischen zwei Abfragen des sichtbaren Kartenausschnitts (Nutzungsbedingungen der Preisquelle: eine Abfrage je Minute).
    /// </summary>
    public static readonly TimeSpan AreaReloadMinInterval = TimeSpan.FromSeconds(60);

    private Command _reloadVisibleAreaCommand = null!;
    private bool _showAreaReloadButton;
    private string? _areaReloadHint;
    private bool _isAreaReloading;
    private bool _isAreaTooLarge;
    private MapFitMode _mapFitMode = MapFitMode.FitToResult;
    private MapViewport? _currentViewport;
    private MapViewport? _searchedViewport;
    private DateTimeOffset? _lastAreaReloadAt;
    private ITimer? _areaReloadTimer;
    private SynchronizationContext? _areaReloadContext;

    /// <summary>
    /// Gibt an, ob die Schaltfläche „Tankstellen in diesem Bereich laden“ erscheint: nur in den Sucharten Standort und Adresse, nachdem der Anwender den Ausschnitt
    /// gegenüber der letzten Suche verschoben oder gezoomt hat. Abgefragt wird erst beim Betätigen der Schaltfläche.
    /// </summary>
    public bool ShowAreaReloadButton
    {
        get => _showAreaReloadButton;
        private set => SetProperty(ref _showAreaReloadButton, value);
    }

    /// <summary>
    /// Der Hinweis zur Schaltfläche „Tankstellen in diesem Bereich laden“ (Ausschnitt zu groß oder Wartezeit bis zur nächsten Abfrage); <see langword="null"/>, wenn keiner anliegt.
    /// </summary>
    public string? AreaReloadHint
    {
        get => _areaReloadHint;
        private set
        {
            if (SetProperty(ref _areaReloadHint, value))
            {
                OnPropertyChanged(nameof(HasAreaReloadHint));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob ein <see cref="AreaReloadHint"/> anliegt.
    /// </summary>
    public bool HasAreaReloadHint => AreaReloadHint is { Length: > 0 };

    /// <summary>
    /// Gibt an, ob gerade die Tankstellen des sichtbaren Ausschnitts geladen werden.
    /// </summary>
    public bool IsAreaReloading
    {
        get => _isAreaReloading;
        private set => SetProperty(ref _isAreaReloading, value);
    }

    /// <summary>
    /// Legt fest, ob die Karte nach einem neuen Ergebnis den Ausschnitt einpasst (neue Suche) oder beibehält (Nachladen des sichtbaren Bereichs).
    /// </summary>
    public MapFitMode MapFitMode
    {
        get => _mapFitMode;
        private set => SetProperty(ref _mapFitMode, value);
    }

    /// <summary>
    /// Befehl zum Laden der Tankstellen des sichtbaren Kartenausschnitts (eine Umkreisabfrage um die Mitte, höchstens eine je <see cref="AreaReloadMinInterval"/>).
    /// </summary>
    public ICommand ReloadVisibleAreaCommand => _reloadVisibleAreaCommand;

    private bool IsAreaReloadMode
    {
        get { return _searchMode is SearchMode.CurrentLocation or SearchMode.Address; }
    }

    private bool CanReloadVisibleArea
    {
        get { return ShowAreaReloadButton && !_isAreaTooLarge && !IsBusy && !IsAreaReloadBlocked; }
    }

    private bool IsAreaReloadBlocked
    {
        get { return GetAreaReloadRemaining() > TimeSpan.Zero; }
    }

    /// <summary>
    /// Übernimmt den Kartenausschnitt nach einer Bewegung durch den Anwender: In den Sucharten Standort und Adresse erscheint die Schaltfläche
    /// „Tankstellen in diesem Bereich laden“, sobald Mitte oder Zoomstufe von der letzten Suche abweichen. Es wird dabei nichts abgefragt.
    /// </summary>
    /// <param name="viewport">Der aktuelle Kartenausschnitt.</param>
    public void OnViewportChanged(MapViewport viewport)
    {
        ArgumentNullException.ThrowIfNull(viewport);
        _currentViewport = viewport;
        UpdateAreaReloadState();
    }

    /// <summary>
    /// Übernimmt den Ausschnitt, auf den die Karte ein neues Suchergebnis eingepasst hat, als Ausschnitt der letzten Suche: Erst eine Abweichung davon
    /// (Verschieben oder Zoomen) blendet die Schaltfläche „Tankstellen in diesem Bereich laden“ ein; „Zurücksetzen“ kehrt zu diesem Ausschnitt zurück.
    /// </summary>
    /// <param name="viewport">Der eingepasste Kartenausschnitt.</param>
    public void OnViewportFitted(MapViewport viewport)
    {
        ArgumentNullException.ThrowIfNull(viewport);
        _searchedViewport = viewport;
        _currentViewport = viewport;
        UpdateAreaReloadState();
    }

    private void InitializeAreaReload()
    {
        _reloadVisibleAreaCommand = new Command(ReloadVisibleArea, () => CanReloadVisibleArea);
        PropertyChanged += OnAreaReloadPropertyChanged;
    }

    private void OnAreaReloadPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Während einer Suche oder eines Nachladens ist der Befehl nicht ausführbar.
        if (e.PropertyName == nameof(IsBusy))
        {
            _reloadVisibleAreaCommand.ChangeCanExecute();
        }
    }

    private void ResetAreaReload()
    {
        _searchedViewport = null;
        ShowAreaReloadButton = false;
        AreaReloadHint = null;
        _isAreaTooLarge = false;
        _reloadVisibleAreaCommand.ChangeCanExecute();
    }

    private TimeSpan GetAreaReloadRemaining()
    {
        if (_lastAreaReloadAt is not { } last)
        {
            return TimeSpan.Zero;
        }

        var remaining = last + AreaReloadMinInterval - _timeProvider.GetUtcNow();
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private void UpdateAreaReloadState()
    {
        var show = false;
        var tooLarge = false;
        if (_currentViewport is { } viewport && IsAreaReloadMode && !IsSameView(_searchedViewport, viewport))
        {
            show = true;
            tooLarge = MapAreaQuery.IsTooLarge(viewport.GetBounds());
        }

        ShowAreaReloadButton = show;
        _isAreaTooLarge = tooLarge;
        UpdateAreaReloadHint();
    }

    private void UpdateAreaReloadHint()
    {
        var remaining = ShowAreaReloadButton && !_isAreaTooLarge ? GetAreaReloadRemaining() : TimeSpan.Zero;
        var remainingSeconds = (int)Math.Ceiling(remaining.TotalSeconds);
        if (_isAreaTooLarge)
        {
            AreaReloadHint = MapTexts.ReloadAreaTooLarge;
        }
        else
        {
            AreaReloadHint = remainingSeconds > 0 ? MapTexts.ReloadAreaWait(remainingSeconds) : null;
        }

        _reloadVisibleAreaCommand.ChangeCanExecute();
    }

    private void StartAreaReloadTimer()
    {
        // Der Zeitgeber aktualisiert die Restzeit im Hinweis und gibt den Befehl nach Ablauf der Sperre wieder frei.
        _areaReloadTimer?.Dispose();
        _areaReloadContext = SynchronizationContext.Current;
        var tick = TimeSpan.FromSeconds(1);
        _areaReloadTimer = _timeProvider.CreateTimer(_ => OnAreaReloadTimerTick(), null, tick, tick);
    }

    private void StopAreaReloadTimer()
    {
        // Nach Ablauf der Sperre oder auf der unsichtbaren Seite wird der Hinweis nicht aktualisiert; die Sperre selbst hängt nur am Zeitpunkt der letzten Abfrage.
        _areaReloadTimer?.Dispose();
        _areaReloadTimer = null;
    }

    private void ResumeAreaReloadTimer()
    {
        // Beim erneuten Erscheinen gibt der Hinweis die aktuelle Restzeit wieder; läuft die Sperre noch, tickt der Zeitgeber weiter bis zu ihrem Ablauf.
        if (IsAreaReloadBlocked && _areaReloadTimer is null)
        {
            StartAreaReloadTimer();
        }

        UpdateAreaReloadHint();
    }

    private void OnAreaReloadTimerTick()
    {
        if (_areaReloadContext is null || SynchronizationContext.Current == _areaReloadContext)
        {
            RefreshAreaReloadBlock();
        }
        else
        {
            _areaReloadContext.Post(_ => RefreshAreaReloadBlock(), null);
        }
    }

    private void RefreshAreaReloadBlock()
    {
        if (!IsAreaReloadBlocked)
        {
            StopAreaReloadTimer();
        }

        UpdateAreaReloadHint();
    }

    private static bool IsSameView(MapViewport? searched, MapViewport current)
    {
        const double Tolerance = 1e-7;
        return searched is not null
            && searched.Zoom == current.Zoom
            && Math.Abs(searched.CenterLatitude - current.CenterLatitude) < Tolerance
            && Math.Abs(searched.CenterLongitude - current.CenterLongitude) < Tolerance;
    }

    private void ReloadVisibleArea()
    {
        if (!CanReloadVisibleArea || _currentViewport is null)
        {
            return;
        }

        LastSearchTask = RunAreaReloadAsync();
    }

    private async Task RunAreaReloadAsync()
    {
        CancelSearch();
        IsAreaReloading = true;
        await RunGuardedSearchAsync(cancellation => ReloadVisibleAreaAsync(cancellation.Token), LogSearchFailure, () => IsAreaReloading = false).ConfigureAwait(true);
    }

    private async Task ReloadVisibleAreaAsync(CancellationToken token)
    {
        var viewport = _currentViewport;
        var settings = await GetSettingsOrReportAsync(clearResult: false, token).ConfigureAwait(true);
        if (settings is null || viewport is null)
        {
            return;
        }

        if (MapAreaQuery.From(viewport.GetBounds(), SelectedFuelTypes(settings)) is not { } query)
        {
            return;
        }

        _lastAreaReloadAt = _timeProvider.GetUtcNow();
        StartAreaReloadTimer();
        var result = await _priceService.SearchNearbyAsync(query, token).ConfigureAwait(true);
        token.ThrowIfCancellationRequested();

        // Liste, Markierungen und Zähler werden ersetzt; die Markierung der Suchposition bleibt die der letzten Suche.
        _searchedViewport = viewport;
        ApplyResult(result, _origin, MapFitMode.KeepViewport);

        // Hat der Anwender den Ausschnitt während der Abfrage weiter bewegt, erscheint die Schaltfläche wieder (mit Hinweis auf die Wartezeit).
        UpdateAreaReloadState();
    }
}

using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Windows.Input;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Map;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Geocoding;
using Tankradar.MAUI.Services.Routing;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Routenmodus des Kartenbereichs: Start (aktueller Standort oder Adresse), Ziel, Abstand zur Route, Ablauf der Routensuche und Ergebnis.
/// Eingaben, Positionen und Route leben ausschließlich im Arbeitsspeicher und werden nie gespeichert oder protokolliert.
/// </summary>
public partial class MapViewModel
{
    private const int MapRouteMaxPoints = 1500;

    private readonly IRouteSearchService _routeSearchService;
    private readonly List<ChoiceOptionViewModel<RouteStartKind>> _routeStartOptions = [];
    private readonly List<RadiusOptionViewModel> _routeDistanceOptions = [];
    private RouteStartKind _routeStartKind = RouteStartKind.CurrentLocation;
    private string _startInput = string.Empty;
    private string _destinationInput = string.Empty;
    private int _routeDistanceKm = RouteDistance.Default;
    private RouteSearchResult? _routeResult;
    private MapRoute? _mapRoute;
    private string? _routeSummary;
    private string? _routeProgressText;

    /// <summary>
    /// Gibt an, ob nach einer Route gesucht wird (Start, Ziel und Abstand sichtbar).
    /// </summary>
    public bool IsRouteMode
    {
        get { return _searchMode == SearchMode.Route; }
    }

    /// <summary>
    /// Gibt an, ob die Radiuswahl der Umkreissuche sichtbar ist (nicht bei der Routensuche).
    /// </summary>
    public bool IsRadiusVisible
    {
        get { return _searchMode != SearchMode.Route; }
    }

    /// <summary>
    /// Die Quellenangabe für Routen und Geodaten (im Routenmodus anzuzeigen).
    /// </summary>
    public string RoutingAttributionText
    {
        get { return SearchTexts.RoutingAttribution; }
    }

    /// <summary>
    /// Der Text des Leerzustands: bei der Routensuche mit Hinweis auf einen größeren Abstand.
    /// </summary>
    public string EmptyStateText
    {
        get { return IsRouteMode ? SearchTexts.RouteEmptyState : SearchTexts.EmptyState; }
    }

    /// <summary>
    /// Die wählbaren Startarten („Aktueller Standort“, „Adresse, Ort oder PLZ“).
    /// </summary>
    public IReadOnlyList<ChoiceOptionViewModel<RouteStartKind>> RouteStartOptions
    {
        get { return _routeStartOptions; }
    }

    /// <summary>
    /// Die gewählte Startart.
    /// </summary>
    public RouteStartKind RouteStartKind
    {
        get { return _routeStartKind; }
    }

    /// <summary>
    /// Gibt an, ob der Start als Adresse eingegeben wird (Eingabefeld sichtbar).
    /// </summary>
    public bool IsRouteStartAddress
    {
        get { return _routeStartKind == RouteStartKind.Address; }
    }

    /// <summary>
    /// Die Eingabe im Startfeld; sie wird nur im Arbeitsspeicher gehalten und nie gespeichert.
    /// </summary>
    public string StartInput
    {
        get => _startInput;
        set
        {
            if (SetProperty(ref _startInput, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(HasStartInput));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob das Startfeld Text enthält (Schaltfläche zum Löschen sichtbar).
    /// </summary>
    public bool HasStartInput
    {
        get { return _startInput.Length > 0; }
    }

    /// <summary>
    /// Die Eingabe im Zielfeld; sie wird nur im Arbeitsspeicher gehalten und nie gespeichert.
    /// </summary>
    public string DestinationInput
    {
        get => _destinationInput;
        set
        {
            if (SetProperty(ref _destinationInput, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(HasDestinationInput));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob das Zielfeld Text enthält (Schaltfläche zum Löschen sichtbar).
    /// </summary>
    public bool HasDestinationInput
    {
        get { return _destinationInput.Length > 0; }
    }

    /// <summary>
    /// Befehl zum Löschen der Starteingabe.
    /// </summary>
    public ICommand ClearStartCommand { get; }

    /// <summary>
    /// Befehl zum Löschen der Zieleingabe.
    /// </summary>
    public ICommand ClearDestinationCommand { get; }

    /// <summary>
    /// Der gewählte Abstand zur Route in Kilometern (Standard 2).
    /// </summary>
    public int RouteDistanceKm
    {
        get => _routeDistanceKm;
        set
        {
            if (SetProperty(ref _routeDistanceKm, value))
            {
                SyncRouteDistanceSelection();
            }
        }
    }

    /// <summary>
    /// Die wählbaren Abstandsstufen zur Route (Chips): 1, 2, 5 und 10 km.
    /// </summary>
    public IReadOnlyList<RadiusOptionViewModel> RouteDistanceOptions
    {
        get { return _routeDistanceOptions; }
    }

    /// <summary>
    /// Die Zusammenfassung der letzten Routensuche („Von A nach B, 123 km, N Tankstellen …“); <see langword="null"/> ohne Routenergebnis.
    /// </summary>
    public string? RouteSummary
    {
        get => _routeSummary;
        private set
        {
            if (SetProperty(ref _routeSummary, value))
            {
                OnPropertyChanged(nameof(HasRouteSummary));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob eine <see cref="RouteSummary"/> anliegt.
    /// </summary>
    public bool HasRouteSummary
    {
        get { return RouteSummary is { Length: > 0 }; }
    }

    /// <summary>
    /// Die auf der Karte eingezeichnete Route; <see langword="null"/> ohne Routenergebnis. Sie wird nur im Arbeitsspeicher gehalten.
    /// </summary>
    public MapRoute? MapRoute
    {
        get => _mapRoute;
        private set
        {
            if (SetProperty(ref _mapRoute, value))
            {
                OnPropertyChanged(nameof(HasMapRoute));
                OnPropertyChanged(nameof(ShowMapHint));
                OnPropertyChanged(nameof(MapHintText));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob eine Route auf der Karte eingezeichnet ist.
    /// </summary>
    public bool HasMapRoute
    {
        get { return _mapRoute is not null; }
    }

    /// <summary>
    /// Der Fortschritt der laufenden Routensuche; <see langword="null"/>, wenn keine läuft.
    /// </summary>
    public string? RouteProgressText
    {
        get => _routeProgressText;
        private set
        {
            if (SetProperty(ref _routeProgressText, value))
            {
                OnPropertyChanged(nameof(HasRouteProgress));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob ein <see cref="RouteProgressText"/> anliegt.
    /// </summary>
    public bool HasRouteProgress
    {
        get { return RouteProgressText is { Length: > 0 }; }
    }

    private string RouteMapHintText
    {
        get
        {
            var result = _routeResult;
            return result is null
                ? MapTexts.NoResults
                : SearchTexts.FormatRouteMapHint(GetStartName(result), GetDestinationName(result), result.Stations.Count);
        }
    }

    /// <summary>
    /// Wählt die Startart der Routensuche.
    /// </summary>
    /// <param name="kind">Die Startart.</param>
    public void SelectStartKind(RouteStartKind kind)
    {
        if (_routeStartKind == kind)
        {
            SyncStartKindSelection();
            return;
        }

        _routeStartKind = kind;
        SyncStartKindSelection();
        OnPropertyChanged(nameof(RouteStartKind));
        OnPropertyChanged(nameof(IsRouteStartAddress));
        StatusMessage = null;
    }

    /// <summary>
    /// Löscht die Starteingabe.
    /// </summary>
    public void ClearStart()
    {
        StartInput = string.Empty;
        StatusMessage = null;
    }

    /// <summary>
    /// Löscht die Zieleingabe.
    /// </summary>
    public void ClearDestination()
    {
        DestinationInput = string.Empty;
        StatusMessage = null;
    }

    private void InitializeRouteOptions()
    {
        foreach (var value in Enum.GetValues<RouteStartKind>())
        {
            var label = value == RouteStartKind.Address ? SearchTexts.RouteStartAddress : SearchTexts.RouteStartCurrentLocation;
            _routeStartOptions.Add(new ChoiceOptionViewModel<RouteStartKind>(value, label, option => SelectStartKind(option.Value)));
        }

        SyncStartKindSelection();
        foreach (var step in RouteDistance.Options)
        {
            _routeDistanceOptions.Add(new RadiusOptionViewModel(step, option => RouteDistanceKm = option.RadiusKm));
        }

        SyncRouteDistanceSelection();
    }

    private void SyncStartKindSelection()
    {
        foreach (var option in _routeStartOptions)
        {
            option.SetSelectedSilently(option.Value == _routeStartKind);
        }
    }

    private void SyncRouteDistanceSelection()
    {
        foreach (var option in _routeDistanceOptions)
        {
            option.SetSelectedSilently(option.RadiusKm == _routeDistanceKm);
        }
    }

    private async Task SearchRouteAsync()
    {
        // Alle Eingaben werden geprüft, bevor irgendetwas an einen externen Dienst geht.
        if (!RouteDistance.IsValid(_routeDistanceKm))
        {
            IsBusy = false;
            StatusMessage = SearchTexts.RouteDistanceInvalid;
            return;
        }

        var startError = _routeStartKind == RouteStartKind.Address ? AddressInput.Validate(_startInput, out _) : AddressInputError.None;
        if (startError != AddressInputError.None)
        {
            IsBusy = false;
            ClearResult();
            StatusMessage = SearchTexts.GetRouteInputMessage(isStart: true, startError);
            return;
        }

        var destinationError = AddressInput.Validate(_destinationInput, out _);
        if (destinationError != AddressInputError.None)
        {
            IsBusy = false;
            ClearResult();
            StatusMessage = SearchTexts.GetRouteInputMessage(isStart: false, destinationError);
            return;
        }

        if (!_connection.IsOnline)
        {
            IsBusy = false;
            ClearResult();
            StatusMessage = SearchTexts.RouteOffline;
            return;
        }

        await RunGuardedSearchAsync(ExecuteRouteSearchAsync, LogRouteSearchFailure, () => RouteProgressText = null).ConfigureAwait(true);
    }

    private void LogRouteSearchFailure(Exception ex)
    {
        _logger.LogWarning("Die Routensuche ist fehlgeschlagen ({ExceptionType}).", ex.GetType().Name);
    }

    private async Task ExecuteRouteSearchAsync(CancellationTokenSource cancellation)
    {
        var token = cancellation.Token;
        var settings = _settings ?? await LoadSettingsAsync().ConfigureAwait(true);
        token.ThrowIfCancellationRequested();
        if (settings is null)
        {
            ClearResult();
            StatusMessage = SettingsTexts.LoadFailed;
            return;
        }

        GeoPosition? startPosition = null;
        if (_routeStartKind == RouteStartKind.CurrentLocation)
        {
            startPosition = await ResolveCurrentLocationAsync(settings, token).ConfigureAwait(true);
            token.ThrowIfCancellationRequested();
            if (startPosition is null)
            {
                return;
            }
        }

        var fuelTypes = settings.FuelTypes.Where(selection => selection.IsSelected).Select(selection => selection.FuelType).ToList();
        var request = new RouteSearchRequest(startPosition, startPosition is null ? _startInput : null, _destinationInput, _routeDistanceKm, fuelTypes);
        var progress = new ContextProgress(this, cancellation);
        var result = await _routeSearchService.SearchAsync(request, progress, token).ConfigureAwait(true);
        token.ThrowIfCancellationRequested();
        ApplyRouteResult(result, startPosition is not null);
    }

    private void ApplyRouteResult(RouteSearchResult result, bool startIsCurrentLocation)
    {
        if (result.Status != RouteSearchStatus.Success || result.Route is null || result.StartPosition is null || result.DestinationPosition is null)
        {
            ClearResult();
            StatusMessage = NullIfEmpty(SearchTexts.GetRouteMessage(result)) ?? SearchTexts.SearchFailed;
            return;
        }

        ClearRouteState();
        _routeResult = result;
        _lastResult = new StationSearchResult(result.Stations.Select(station => station.Station).ToList(), PriceDataSource.Live, PriceFailure.None);
        Origin = new MapOrigin(startIsCurrentLocation ? MapOriginKind.CurrentLocation : MapOriginKind.SearchedPlace, result.StartPosition.Latitude, result.StartPosition.Longitude);
        MapRoute = new MapRoute(result.Route.Simplify(Math.Max(0.005, result.Route.LengthKm / 5000), MapRouteMaxPoints));
        MapResultVersion++;
        RouteSummary = SearchTexts.FormatRouteSummary(GetStartName(result), GetDestinationName(result), result.LengthKm, result.Stations.Count, _routeDistanceKm);
        ApplyFilterAndSort();
        StatusMessage = null;
        SourceNote = null;
        UpdateOfflineState();
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(MapHintText));
    }

    private string GetStartName(RouteSearchResult result)
    {
        if (_routeStartKind == RouteStartKind.CurrentLocation)
        {
            return SearchTexts.RouteStartCurrentLocation;
        }

        return NonEmpty(result.StartName) ?? NormalizedOrEmpty(_startInput);
    }

    private string GetDestinationName(RouteSearchResult result)
    {
        return NonEmpty(result.DestinationName) ?? NormalizedOrEmpty(_destinationInput);
    }

    private static string? NonEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string NormalizedOrEmpty(string input)
    {
        AddressInput.Validate(input, out var normalized);
        return normalized;
    }

    private void ClearRouteState()
    {
        _routeResult = null;
        MapRoute = null;
        RouteSummary = null;
        RouteProgressText = null;
        OnPropertyChanged(nameof(MapHintText));
    }

    private void ReportRouteProgress(CancellationTokenSource cancellation, RouteSearchProgress progress)
    {
        if (IsCurrent(cancellation))
        {
            RouteProgressText = SearchTexts.FormatRouteProgress(progress);
        }
    }

    /// <summary>
    /// Meldet den Fortschritt im Kontext der Oberfläche, von dem aus die Suche gestartet wurde, und ignoriert Meldungen abgelöster Suchen.
    /// </summary>
    private sealed class ContextProgress : IProgress<RouteSearchProgress>
    {
        private readonly MapViewModel _owner;
        private readonly CancellationTokenSource _cancellation;
        private readonly SynchronizationContext? _context = SynchronizationContext.Current;

        public ContextProgress(MapViewModel owner, CancellationTokenSource cancellation)
        {
            _owner = owner;
            _cancellation = cancellation;
        }

        public void Report(RouteSearchProgress value)
        {
            if (_context is null)
            {
                _owner.ReportRouteProgress(_cancellation, value);
                return;
            }

            _context.Post(_ => _owner.ReportRouteProgress(_cancellation, value), null);
        }
    }
}

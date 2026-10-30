using System.Windows.Input;
using Tankradar.MAUI.Models.Map;
using Tankradar.MAUI.Resources;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Map;

namespace Tankradar.MAUI.Views.Controls;

/// <summary>
/// Plattformübergreifende Karte auf Basis von OpenStreetMap-Kacheln: gleiche Darstellung unter iOS und Windows (Kacheln als Bilder, Markierungen als Schaltflächen),
/// dadurch auch über UI Automation prüfbar. Die Karte lässt sich per Zoom-Schaltflächen und Kneifgeste zoomen sowie per Wischgeste und Schaltflächen verschieben.
/// Sie zeigt die Tankstellenmarkierungen mit Preis (Farbe nach Preisniveau) und die markierte Suchposition; die Quellenangabe legt die Seite über die Karte.
/// Bewegt der Anwender den Ausschnitt, meldet die Karte das über <see cref="ViewportChanged"/>.
/// </summary>
public partial class StationMapView : ContentView
{
    /// <summary>
    /// Bindbare Eigenschaft <see cref="Markers"/>.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public static readonly BindableProperty MarkersProperty;

    /// <summary>
    /// Bindbare Eigenschaft <see cref="Origin"/>.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public static readonly BindableProperty OriginProperty;

    /// <summary>
    /// Bindbare Eigenschaft <see cref="Route"/>.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public static readonly BindableProperty RouteProperty;

    /// <summary>
    /// Bindbare Eigenschaft <see cref="ResultVersion"/>.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public static readonly BindableProperty ResultVersionProperty;

    /// <summary>
    /// Bindbare Eigenschaft <see cref="MarkerCommand"/>.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public static readonly BindableProperty MarkerCommandProperty;

    /// <summary>
    /// Bindbare Eigenschaft <see cref="MapFitMode"/>.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public static readonly BindableProperty MapFitModeProperty;

    static StationMapView()
    {
        MarkersProperty = BindableProperty.Create(
        nameof(Markers),
        typeof(IReadOnlyList<MapMarker>),
        typeof(StationMapView),
        propertyChanged: OnMarkersChanged);
        OriginProperty = BindableProperty.Create(
        nameof(Origin),
        typeof(MapOrigin),
        typeof(StationMapView),
        propertyChanged: OnMarkersChanged);
        RouteProperty = BindableProperty.Create(
        nameof(Route),
        typeof(MapRoute),
        typeof(StationMapView),
        propertyChanged: OnMarkersChanged);
        ResultVersionProperty = BindableProperty.Create(
        nameof(ResultVersion),
        typeof(int),
        typeof(StationMapView),
        propertyChanged: OnResultVersionChanged);
        MarkerCommandProperty = BindableProperty.Create(
        nameof(MarkerCommand),
        typeof(ICommand),
        typeof(StationMapView));
        MapFitModeProperty = BindableProperty.Create(
        nameof(MapFitMode),
        typeof(MapFitMode),
        typeof(StationMapView),
        MapFitMode.FitToResult);
    }

    private const double MarkerHeight = 44;
    private const double MarkerPriceWidth = 88;
    private const double PanFraction = 0.4;
    private const double FitPadding = 64;
    private static readonly TimeSpan TileDelay = TimeSpan.FromMilliseconds(150);

    private readonly Dictionary<TileKey, Image> _tiles = [];
    private readonly Dictionary<TileKey, CancellationTokenSource> _tileLoads = [];
    private readonly Dictionary<string, Button> _markerButtons = [];
    private MapViewport? _viewport;
    private Border? _originMarker;
    private Border? _destinationMarker;
    private readonly MapRouteDrawable _routeDrawable = new();
    private bool _fitPending;
    private double _lastPanX;
    private double _lastPanY;
    private double _pinchScale = 1;

    /// <summary>
    /// Erstellt die Karte.
    /// </summary>
    public StationMapView()
    {
        InitializeComponent();
        RouteLayer.Drawable = _routeDrawable;
        Surface.SizeChanged += (_, _) => OnSurfaceSizeChanged();
        Surface.HandlerChanged += (_, _) => AttachPointerInput();
        Surface.Unloaded += (_, _) => DetachPointerInput();
    }

    /// <summary>
    /// Bindet unter Windows das Ziehen mit der Maus und das Mausrad an die Karte (die Gestenerkenner reagieren dort nur auf Berührung und Stift). Auf anderen Plattformen ohne Wirkung.
    /// </summary>
    partial void AttachPointerInput();

    /// <summary>
    /// Löst unter Windows die Zeigerereignisse beim Entladen der Karte. Auf anderen Plattformen ohne Wirkung.
    /// </summary>
    partial void DetachPointerInput();

    /// <summary>
    /// Die Markierungen der Tankstellen.
    /// </summary>
    public IReadOnlyList<MapMarker>? Markers
    {
        get => (IReadOnlyList<MapMarker>?)GetValue(MarkersProperty);
        set => SetValue(MarkersProperty, value);
    }

    /// <summary>
    /// Die markierte Suchposition (eigener Standort oder gesuchte Position).
    /// </summary>
    public MapOrigin? Origin
    {
        get => (MapOrigin?)GetValue(OriginProperty);
        set => SetValue(OriginProperty, value);
    }

    /// <summary>
    /// Die eingezeichnete Route einer Routensuche; <see langword="null"/> ohne Route.
    /// </summary>
    public MapRoute? Route
    {
        get => (MapRoute?)GetValue(RouteProperty);
        set => SetValue(RouteProperty, value);
    }

    /// <summary>
    /// Zähler der Ergebnisse; ändert er sich, wird der Ausschnitt bei <see cref="MapFitMode.FitToResult"/> neu auf alle Markierungen eingepasst.
    /// </summary>
    public int ResultVersion
    {
        get => (int)GetValue(ResultVersionProperty);
        set => SetValue(ResultVersionProperty, value);
    }

    /// <summary>
    /// Befehl, der beim Antippen einer Markierung mit der Tankstelle (<see cref="Models.Search.StationListItem"/>) als Parameter ausgeführt wird.
    /// </summary>
    public ICommand? MarkerCommand
    {
        get => (ICommand?)GetValue(MarkerCommandProperty);
        set => SetValue(MarkerCommandProperty, value);
    }

    /// <summary>
    /// Legt fest, ob ein neues Ergebnis (<see cref="ResultVersion"/>) den Ausschnitt einpasst (<see cref="MapFitMode.FitToResult"/>) oder den Ausschnitt des Anwenders beibehält
    /// (<see cref="MapFitMode.KeepViewport"/>, Nachladen des sichtbaren Bereichs).
    /// </summary>
    public MapFitMode MapFitMode
    {
        get => (MapFitMode)GetValue(MapFitModeProperty);
        set => SetValue(MapFitModeProperty, value);
    }

    /// <summary>
    /// Wird ausgelöst, wenn der Anwender den Ausschnitt verändert (Ziehen, Wischen, Kneifen, Mausrad, Zoom- und Verschiebeschaltflächen, Zurücksetzen);
    /// das Einpassen nach einem neuen Ergebnis löst es nicht aus.
    /// </summary>
    public event EventHandler<ViewportChangedEventArgs>? ViewportChanged;

    /// <summary>
    /// Wird ausgelöst, nachdem die Karte den Ausschnitt auf ein neues Ergebnis eingepasst hat (<see cref="MapFitMode.FitToResult"/>);
    /// der gemeldete Ausschnitt ist der Ausschnitt der Suche, gegenüber dem spätere Bewegungen des Anwenders verglichen werden.
    /// </summary>
    public event EventHandler<ViewportChangedEventArgs>? ViewportFitted;

    /// <summary>
    /// Die Quelle der Kartenkacheln; ohne Quelle bleibt der Hintergrund leer, die Markierungen bleiben benutzbar.
    /// </summary>
    public ITileSource? TileSource { get; set; }

    private static void OnMarkersChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((StationMapView)bindable).RefreshMarkers();
    }

    private static void OnResultVersionChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((StationMapView)bindable).OnResultChanged();
    }

    private void OnSurfaceSizeChanged()
    {
        if (Surface.Width <= 0 || Surface.Height <= 0)
        {
            return;
        }

        if (_viewport is null || _fitPending)
        {
            FitToResult();
            return;
        }

        _viewport = _viewport.Resize(Surface.Width, Surface.Height);
        Render();
    }

    private void RequestFit()
    {
        // Die Bindungen aktualisieren Markierungen, Suchposition und Zähler nacheinander; eingepasst wird erst danach.
        _fitPending = true;
        Dispatcher.Dispatch(() =>
        {
            if (_fitPending && Surface.Width > 0 && Surface.Height > 0)
            {
                FitToResult();
            }
        });
    }

    private void FitToResult()
    {
        FitToContent();
        if (_viewport is { } viewport)
        {
            ViewportFitted?.Invoke(this, new ViewportChangedEventArgs(viewport));
        }
    }

    private void FitToContent()
    {
        _fitPending = false;
        var points = new List<(double, double)>();
        foreach (var marker in Markers ?? [])
        {
            points.Add((marker.Latitude, marker.Longitude));
        }

        if (Origin is { } origin)
        {
            points.Add((origin.Latitude, origin.Longitude));
        }

        if (Route is { } route)
        {
            // Die Begrenzungsbox der Route: Start, Ziel und alle Punkte dazwischen liegen darin.
            points.Add((route.Bounds.MinLatitude, route.Bounds.MinLongitude));
            points.Add((route.Bounds.MaxLatitude, route.Bounds.MaxLongitude));
        }

        _viewport = MapViewport.Fit(points, Surface.Width, Surface.Height, FitPadding);
        Render();
    }

    private void RefreshMarkers()
    {
        ClearMarkerVisuals();
        if (_viewport is not null)
        {
            Render();
        }
    }

    private void OnResultChanged()
    {
        // Nur neue Suchen passen den Ausschnitt an; beim Nachladen des sichtbaren Bereichs bleibt er erhalten.
        if (MapFitMode == MapFitMode.FitToResult)
        {
            RequestFit();
            return;
        }

        RefreshMarkers();
    }

    private void RaiseViewportChanged()
    {
        if (_viewport is { } viewport)
        {
            ViewportChanged?.Invoke(this, new ViewportChangedEventArgs(viewport));
        }
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == IsVisibleProperty.PropertyName)
        {
            if (IsVisible)
            {
                Render();
            }
            else
            {
                // Unsichtbare Karte: Markierungen und Kacheln nicht im Oberflächenbaum (und in der Bedienhilfe) halten.
                ClearVisuals();
            }
        }
    }

    private void ClearVisuals()
    {
        ClearMarkerVisuals();
        foreach (var load in _tileLoads.Values)
        {
            load.Cancel();
        }

        _tileLoads.Clear();
        foreach (var image in _tiles.Values)
        {
            TileLayer.Remove(image);
        }

        _tiles.Clear();
    }

    private void ClearMarkerVisuals()
    {
        foreach (var button in _markerButtons.Values)
        {
            MarkerLayer.Remove(button);
        }

        _markerButtons.Clear();
        if (_originMarker is not null)
        {
            MarkerLayer.Remove(_originMarker);
            _originMarker = null;
        }

        if (_destinationMarker is not null)
        {
            MarkerLayer.Remove(_destinationMarker);
            _destinationMarker = null;
        }
    }

    private void Render()
    {
        if (!IsVisible || _viewport is not { } viewport)
        {
            return;
        }

        RenderTiles(viewport);
        RenderRoute(viewport);
        RenderMarkers(viewport);
        ZoomLabel.Text = MapTexts.FormatZoom(viewport.Zoom);
    }

    private void RenderTiles(MapViewport viewport)
    {
        var placed = viewport.VisibleTiles();
        var wanted = placed.Select(tile => tile.Key).ToHashSet();
        foreach (var key in _tiles.Keys.Where(key => !wanted.Contains(key)).ToList())
        {
            if (_tileLoads.Remove(key, out var load))
            {
                load.Cancel();
                load.Dispose();
            }

            TileLayer.Remove(_tiles[key]);
            _tiles.Remove(key);
        }

        // Kacheln desselben Schlüssels können an mehreren Stellen erscheinen (Weltkarte wiederholt sich bei kleinen Zoomstufen); dann wird nur die erste gezeigt.
        foreach (var tile in placed.GroupBy(tile => tile.Key).Select(group => group.First()))
        {
            if (!_tiles.TryGetValue(tile.Key, out var image))
            {
                image = new Image { Aspect = Aspect.Fill, InputTransparent = true };
                _tiles[tile.Key] = image;
                TileLayer.Add(image);
                StartTileLoad(tile.Key, image);
            }

            AbsoluteLayout.SetLayoutBounds(image, new Rect(tile.Left, tile.Top, MapViewport.TileSize, MapViewport.TileSize));
            AbsoluteLayout.SetLayoutFlags(image, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
        }
    }

    private void RenderRoute(MapViewport viewport)
    {
        if (Route is not { } route)
        {
            _routeDrawable.Lines = [];
            RouteLayer.IsVisible = false;
            return;
        }

        _routeDrawable.Lines = MapRouteProjector.Project(viewport, route.Points);
        RouteLayer.IsVisible = true;
        RouteLayer.Invalidate();
    }

    private void StartTileLoad(TileKey key, Image image)
    {
        var source = TileSource;
        if (source is null)
        {
            return;
        }

        var cancellation = new CancellationTokenSource();
        _tileLoads[key] = cancellation;
        _ = LoadTileAsync(source, key, image, cancellation);
    }

    private async Task LoadTileAsync(ITileSource source, TileKey key, Image image, CancellationTokenSource cancellation)
    {
        try
        {
            // Kurze Verzögerung: Kacheln, die beim schnellen Verschieben oder Zoomen sofort wieder verschwinden, werden gar nicht erst abgerufen.
            await Task.Delay(TileDelay, cancellation.Token).ConfigureAwait(true);
            var token = cancellation.Token;

            // Datei- und Netzzugriffe der Kachelquelle laufen nicht auf dem UI-Thread (kein Ruckeln beim Verschieben).
            var data = await Task.Run(() => source.GetTileAsync(key, token), token).ConfigureAwait(true);
            if (cancellation.IsCancellationRequested)
            {
                return;
            }

            if (data is not null)
            {
                image.Source = ImageSource.FromStream(() => new MemoryStream(data));
            }
            else if (_tiles.TryGetValue(key, out var current) && ReferenceEquals(current, image))
            {
                // Nicht verfügbar: Die Kachel wird beim nächsten Aufbau erneut angefragt (die Quelle hält das Wartefenster nach Fehlern ein).
                TileLayer.Remove(image);
                _tiles.Remove(key);
            }
        }
        catch (OperationCanceledException)
        {
            // Die Kachel ist nicht mehr sichtbar.
        }
        finally
        {
            if (_tileLoads.TryGetValue(key, out var registered) && ReferenceEquals(registered, cancellation))
            {
                _tileLoads.Remove(key);
            }

            cancellation.Dispose();
        }
    }

    private void RenderMarkers(MapViewport viewport)
    {
        var markers = Markers ?? [];
        var selection = MapMarkerSelection.Select(markers, viewport);
        var seen = new HashSet<string>();
        foreach (var marker in selection.Rendered)
        {
            var (x, y) = viewport.ToScreen(marker.Latitude, marker.Longitude);
            var id = marker.Station.Id;
            seen.Add(id);
            if (!_markerButtons.TryGetValue(id, out var button))
            {
                button = CreateMarkerButton(marker);
                _markerButtons[id] = button;
                MarkerLayer.Add(button);
            }

            var width = marker.HasPrice ? MarkerPriceWidth : MarkerHeight;
            AbsoluteLayout.SetLayoutBounds(button, new Rect(x - (width / 2), y - MarkerHeight, width, MarkerHeight));
            AbsoluteLayout.SetLayoutFlags(button, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
        }

        foreach (var id in _markerButtons.Keys.Where(id => !seen.Contains(id)).ToList())
        {
            MarkerLayer.Remove(_markerButtons[id]);
            _markerButtons.Remove(id);
        }

        RenderOrigin(viewport);
        RenderDestination(viewport);
        CountLabel.Text = selection.IsTruncated
            ? MapTexts.FormatStationCountLimited(selection.InViewport, markers.Count, selection.Rendered.Count)
            : MapTexts.FormatStationCount(selection.InViewport, markers.Count);
    }

    private Button CreateMarkerButton(MapMarker marker)
    {
        var button = new Button
        {
            Text = marker.HasPrice ? marker.PriceText : FirstLetter(marker.Station.Name),
            BackgroundColor = MapPalette.For(marker.Level),
            TextColor = Colors.White,
            BorderColor = Colors.White,
            BorderWidth = 2,
            CornerRadius = 14,
            Padding = new Thickness(6, 0),
            FontFamily = "InterSemibold",
            FontSize = 14,
            MinimumHeightRequest = MarkerHeight,
            AutomationId = "Map.Marker." + marker.Station.Id,
        };
        SemanticProperties.SetDescription(button, marker.Description);
        SemanticProperties.SetHint(button, MapTexts.GetLevelLabel(marker.Level));
        button.Clicked += (_, _) => MarkerCommand?.Execute(marker.Station);
        return button;
    }

    private void RenderOrigin(MapViewport viewport)
    {
        if (Origin is not { } origin)
        {
            return;
        }

        var (x, y) = viewport.ToScreen(origin.Latitude, origin.Longitude);
        var inside = viewport.Contains(origin.Latitude, origin.Longitude);
        if (!inside)
        {
            if (_originMarker is not null)
            {
                MarkerLayer.Remove(_originMarker);
                _originMarker = null;
            }

            return;
        }

        if (_originMarker is null)
        {
            _originMarker = new Border
            {
                BackgroundColor = Color.FromArgb("#005C55"),
                Stroke = Colors.White,
                StrokeThickness = 3,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 11 },
                InputTransparent = true,
                AutomationId = "Map.Origin",
            };
            SemanticProperties.SetDescription(_originMarker, MapTexts.GetOriginLabel(origin.Kind));
            MarkerLayer.Add(_originMarker);
        }

        AbsoluteLayout.SetLayoutBounds(_originMarker, new Rect(x - 11, y - 11, 22, 22));
        AbsoluteLayout.SetLayoutFlags(_originMarker, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
    }

    private void RenderDestination(MapViewport viewport)
    {
        if (Route is not { } route)
        {
            return;
        }

        var destination = route.Destination;
        if (!viewport.Contains(destination.Latitude, destination.Longitude))
        {
            if (_destinationMarker is not null)
            {
                MarkerLayer.Remove(_destinationMarker);
                _destinationMarker = null;
            }

            return;
        }

        var (x, y) = viewport.ToScreen(destination.Latitude, destination.Longitude);
        if (_destinationMarker is null)
        {
            _destinationMarker = new Border
            {
                BackgroundColor = Color.FromArgb("#1E293B"),
                Stroke = Colors.White,
                StrokeThickness = 3,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 4 },
                InputTransparent = true,
                AutomationId = "Map.Destination",
            };
            SemanticProperties.SetDescription(_destinationMarker, MapTexts.RouteDestination);
            MarkerLayer.Add(_destinationMarker);
        }

        AbsoluteLayout.SetLayoutBounds(_destinationMarker, new Rect(x - 11, y - 11, 22, 22));
        AbsoluteLayout.SetLayoutFlags(_destinationMarker, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
    }

    private static string FirstLetter(string name)
    {
        return name.Length > 0 ? char.ToUpperInvariant(name[0]).ToString() : "·";
    }

    private void Zoom(int delta)
    {
        if (_viewport is { } viewport)
        {
            _viewport = viewport.ZoomBy(delta);
            Render();
            RaiseViewportChanged();
        }
    }

    private void Pan(double fractionX, double fractionY)
    {
        if (_viewport is { } viewport)
        {
            // Der Ausschnitt wandert in die Richtung, die Karte also entgegengesetzt.
            _viewport = viewport.PanByPixels(-fractionX * viewport.Width, -fractionY * viewport.Height);
            Render();
            RaiseViewportChanged();
        }
    }

    private void PanByPixels(double deltaX, double deltaY)
    {
        if (_viewport is { } viewport)
        {
            _viewport = viewport.PanByPixels(deltaX, deltaY);
            Render();
            RaiseViewportChanged();
        }
    }

    private void OnZoomInClicked(object? sender, EventArgs e)
    {
        Zoom(1);
    }

    private void OnZoomOutClicked(object? sender, EventArgs e)
    {
        Zoom(-1);
    }

    private void OnRecenterClicked(object? sender, EventArgs e)
    {
        FitToContent();
        RaiseViewportChanged();
    }

    private void OnPanNorthClicked(object? sender, EventArgs e)
    {
        Pan(0, -PanFraction);
    }

    private void OnPanSouthClicked(object? sender, EventArgs e)
    {
        Pan(0, PanFraction);
    }

    private void OnPanEastClicked(object? sender, EventArgs e)
    {
        Pan(PanFraction, 0);
    }

    private void OnPanWestClicked(object? sender, EventArgs e)
    {
        Pan(-PanFraction, 0);
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _lastPanX = 0;
                _lastPanY = 0;
                break;
            case GestureStatus.Running when _viewport is { } viewport:
                _viewport = viewport.PanByPixels(e.TotalX - _lastPanX, e.TotalY - _lastPanY);
                _lastPanX = e.TotalX;
                _lastPanY = e.TotalY;
                Render();
                RaiseViewportChanged();
                break;
            default:
                break;
        }
    }

    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        switch (e.Status)
        {
            case GestureStatus.Started:
                _pinchScale = 1;
                break;
            case GestureStatus.Running:
                _pinchScale = e.Scale;
                break;
            case GestureStatus.Completed:
                // Ganzzahlige Zoomstufen: eine deutliche Kneifgeste ändert die Stufe um eins.
                if (_pinchScale > 1.3)
                {
                    Zoom(1);
                }
                else if (_pinchScale < 0.77)
                {
                    Zoom(-1);
                }

                _pinchScale = 1;
                break;
            default:
                break;
        }
    }
}

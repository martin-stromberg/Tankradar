using System.ComponentModel;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.ViewModels;

namespace Tankradar.MAUI.Views.Controls;

/// <summary>
/// Trefferliste der Kartenansicht als am unteren Rand angedocktes Panel: eingeklappt zeigt es die Trefferanzahl, ausgeklappt Filter, Sortierung und die Liste
/// mit seitenweiser Anzeige. Auf- und Zuklappen über die Schaltfläche am Kopf (Bedienung ohne Geste), durch Antippen des Kopfes oder per Ziehgeste.
/// Der Bindungskontext ist das <see cref="MapViewModel"/> der Seite; ausgeklappt nimmt das Panel höchstens 60 % der Seitenhöhe ein. Der Wechsel zwischen
/// ein- und ausgeklapptem Zustand wird als Höhenänderung animiert (das Panel gleitet über <c>TranslateToAsync</c> nach oben bzw. unten).
/// </summary>
public partial class StationBottomSheet : ContentView
{
    private const double MaxHeightFraction = 0.6;
    private static readonly TimeSpan SnapDuration = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan ToggleDuration = TimeSpan.FromMilliseconds(200);

    // Spätestens nach dieser Zeit gilt der Zielzustand, auch wenn die Animation nicht läuft (z. B. ohne sichtbares Fenster).
    private static readonly TimeSpan ToggleTimeout = ToggleDuration + TimeSpan.FromMilliseconds(300);

    private MapViewModel? _viewModel;
    private VisualElement? _host;
    private double _panY;
    private int _toggleVersion;

    /// <summary>
    /// Erstellt das Panel.
    /// </summary>
    public StationBottomSheet()
    {
        InitializeComponent();
        Header.SizeChanged += (_, _) => UpdateMaximumHeight();
        StatusArea.SizeChanged += (_, _) => UpdateMaximumHeight();
    }

    /// <inheritdoc />
    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = BindingContext as MapViewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            ListArea.IsVisible = _viewModel.IsBottomSheetExpanded;
        }
    }

    /// <inheritdoc />
    protected override void OnParentSet()
    {
        base.OnParentSet();
        if (_host is not null)
        {
            _host.SizeChanged -= OnHostSizeChanged;
        }

        _host = Parent as VisualElement;
        if (_host is not null)
        {
            _host.SizeChanged += OnHostSizeChanged;
        }

        UpdateMaximumHeight();
    }

    private double HostHeight
    {
        get { return _host?.Height ?? 0; }
    }

    private void OnHostSizeChanged(object? sender, EventArgs e)
    {
        UpdateMaximumHeight();
    }

    private void UpdateMaximumHeight()
    {
        if (HostHeight <= 0)
        {
            return;
        }

        // Kopf und Statuszeilen bleiben sichtbar; die Liste darunter scrollt innerhalb der Höhengrenze (Kartenfläche und Quellenangabe bleiben oberhalb frei).
        ListArea.MaximumHeightRequest = Math.Max(0, (HostHeight * MaxHeightFraction) - Header.Height - StatusArea.Height);
    }

    private void OnHeaderPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (_viewModel is not { } viewModel)
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panY = 0;
                break;
            case GestureStatus.Running:
                _panY = e.TotalY;
                if (viewModel.IsBottomSheetExpanded && _panY > 0)
                {
                    // Ausgeklappt folgt das Panel dem Finger nach unten (begrenzt auf den Weg bis zum eingeklappten Zustand).
                    Sheet.TranslationY = Math.Min(_panY, Travel);
                }

                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                var threshold = Travel / 2;
                var expand = !viewModel.IsBottomSheetExpanded && _panY < -threshold;
                var collapse = viewModel.IsBottomSheetExpanded && _panY > threshold;
                _panY = 0;
                if (expand || collapse)
                {
                    // Die Animation des Zustandswechsels übernimmt das Zurücksetzen der Verschiebung.
                    viewModel.ToggleBottomSheetCommand.Execute(null);
                }
                else
                {
                    _ = Sheet.TranslateToAsync(0, 0, (uint)SnapDuration.TotalMilliseconds);
                }

                break;
            default:
                break;
        }
    }

    private double Travel
    {
        get { return Math.Max((HostHeight * MaxHeightFraction) - Header.Height, 1); }
    }

    private void OnStationTapped(object? sender, EventArgs e)
    {
        // Eintrag und Schaltfläche der Zeile tragen die Tankstelle als Bindungskontext; geöffnet wird die Detailansicht wie über die Markierung.
        if (sender is BindableObject { BindingContext: StationListItem station } && _viewModel is { } viewModel)
        {
            viewModel.OpenStationCommand.Execute(station);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MapViewModel.IsBottomSheetExpanded) && _viewModel is { } viewModel)
        {
            _ = AnimateExpandedAsync(viewModel.IsBottomSheetExpanded);
        }
    }

    private async Task AnimateExpandedAsync(bool expanded)
    {
        var version = ++_toggleVersion;
        Sheet.CancelAnimations();
        if (!IsLoaded || Sheet.Width <= 0)
        {
            ListArea.IsVisible = expanded;
            Sheet.TranslationY = 0;
            return;
        }

        if (expanded)
        {
            // Die Liste wird sofort eingeblendet; das um ihre Höhe nach unten verschobene Panel gleitet anschließend an seine Endposition.
            ListArea.IsVisible = true;
            Sheet.TranslationY = MeasureListHeight();
            await RunToggleAnimationAsync(0).ConfigureAwait(true);
        }
        else
        {
            // Das Panel gleitet um die Höhe der Liste nach unten; erst danach wird die Liste ausgeblendet und die Verschiebung aufgehoben.
            await RunToggleAnimationAsync(ListArea.Height).ConfigureAwait(true);
        }

        if (version != _toggleVersion)
        {
            return;
        }

        Sheet.CancelAnimations();
        ListArea.IsVisible = expanded;
        Sheet.TranslationY = 0;
    }

    private Task RunToggleAnimationAsync(double targetY)
    {
        var animation = Sheet.TranslateToAsync(0, Math.Max(targetY, 0), (uint)ToggleDuration.TotalMilliseconds, Easing.CubicOut);
        return Task.WhenAny(animation, Task.Delay(ToggleTimeout));
    }

    private double MeasureListHeight()
    {
        var height = ListArea.Measure(Sheet.Width, double.PositiveInfinity).Height;
        if (ListArea.MaximumHeightRequest > 0)
        {
            height = Math.Min(height, ListArea.MaximumHeightRequest);
        }

        return double.IsFinite(height) ? Math.Max(height, 0) : 0;
    }
}

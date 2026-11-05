using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Views.Charts;

/// <summary>
/// Steuerelement für die Diagramme der Auswertung: zeichnet <see cref="Data"/> als Balkendiagramm und zeichnet bei Datenänderung
/// sowie beim Wechsel zwischen hellem und dunklem Erscheinungsbild neu. Die Anmeldung beim Ereignis des Erscheinungsbilds
/// hält die View nicht am Leben (schwache Verweise) und endet, sobald die View ihren Handler verliert.
/// </summary>
public sealed class ChartGraphicsView : GraphicsView
{
    /// <summary>
    /// Bindbare Eigenschaft <see cref="Data"/>.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public static readonly BindableProperty DataProperty;

    private ThemeSubscription? _subscription;

    static ChartGraphicsView()
    {
        DataProperty = BindableProperty.Create(
        nameof(Data),
        typeof(ChartData),
        typeof(ChartGraphicsView),
        propertyChanged: OnDataChanged);
    }

    /// <summary>
    /// Die darzustellenden Daten.
    /// </summary>
    public ChartData? Data
    {
        get { return (ChartData?)GetValue(DataProperty); }
        set { SetValue(DataProperty, value); }
    }

    /// <inheritdoc />
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        _subscription?.Dispose();
        _subscription = null;
        if (Handler is not null && Application.Current is { } application)
        {
            _subscription = new ThemeSubscription(this, application);
        }
    }

    /// <inheritdoc />
    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        _subscription?.Dispose();
        _subscription = null;
        base.OnHandlerChanging(args!);
    }

    private static void OnDataChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ChartGraphicsView)bindable;
        view.Drawable = newValue is ChartData data ? new BarChartDrawable(data) : null!;
        view.Invalidate();
    }

    /// <summary>
    /// Meldet eine View schwach beim Ereignis des Erscheinungsbilds an; meldet sich selbst ab, wenn die View nicht mehr existiert.
    /// </summary>
    private sealed class ThemeSubscription : IDisposable
    {
        private readonly WeakReference<ChartGraphicsView> _view;
        private readonly Application _application;
        private bool _disposed;

        public ThemeSubscription(ChartGraphicsView view, Application application)
        {
            _view = new WeakReference<ChartGraphicsView>(view);
            _application = application;
            _application.RequestedThemeChanged += OnRequestedThemeChanged;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _application.RequestedThemeChanged -= OnRequestedThemeChanged;
        }

        private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
        {
            if (_view.TryGetTarget(out var view))
            {
                view.Invalidate();
            }
            else
            {
                Dispose();
            }
        }
    }
}

using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Views.Charts;

/// <summary>
/// Steuerelement für die Diagramme der Auswertung: wählt nach <see cref="ChartData.Kind"/> die passende Zeichnung und zeichnet bei Datenänderung neu.
/// </summary>
public sealed class ChartGraphicsView : GraphicsView
{
    /// <summary>
    /// Bindbare Eigenschaft <see cref="Data"/>.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public static readonly BindableProperty DataProperty;

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
        if (Application.Current is not { } application)
        {
            return;
        }

        application.RequestedThemeChanged -= OnRequestedThemeChanged;
        if (Handler is not null)
        {
            application.RequestedThemeChanged += OnRequestedThemeChanged;
        }
    }

    /// <inheritdoc />
    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        if (args?.NewHandler is null && Application.Current is { } application)
        {
            application.RequestedThemeChanged -= OnRequestedThemeChanged;
        }

        base.OnHandlerChanging(args!);
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        Invalidate();
    }

    private static void OnDataChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ChartGraphicsView)bindable;
        view.Drawable = newValue switch
        {
            ChartData { Kind: ChartKind.Bar } data => new BarChartDrawable(data),
            ChartData data => new LineChartDrawable(data),
            _ => null!,
        };
        view.Invalidate();
    }
}

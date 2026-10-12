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

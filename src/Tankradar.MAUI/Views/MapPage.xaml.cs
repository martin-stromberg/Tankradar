using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Map;
using Tankradar.MAUI.ViewModels;

namespace Tankradar.MAUI.Views;

/// <summary>
/// Seite für den Bereich „Karte" (Suche): Umkreissuche am aktuellen Standort oder rund um eine Adresse mit Ergebnisliste und alternativer Kartenansicht. Meldet das Ausblenden der Seite an das <see cref="MapViewModel"/>.
/// </summary>
public partial class MapPage : TankradarContentPage
{
    /// <summary>
    /// Erstellt die Seite und setzt das per Dependency Injection bereitgestellte <see cref="MapViewModel"/> als Bindungskontext.
    /// </summary>
    /// <param name="viewModel">Das per Dependency Injection bereitgestellte ViewModel der Seite.</param>
    /// <param name="tileSource">Die Quelle der Kartenkacheln für die Kartenansicht.</param>
    public MapPage(MapViewModel viewModel, ITileSource tileSource)
    {
        InitializeComponent();
        ResultMap.TileSource = tileSource;
        BindingContext = viewModel;
    }

    private void OnStationTapped(object? sender, EventArgs e)
    {
        // Karte und Schaltfläche der Ergebniszeile tragen die Tankstelle als Bindungskontext.
        if (sender is BindableObject { BindingContext: StationListItem station } && BindingContext is MapViewModel viewModel)
        {
            viewModel.OpenStationCommand.Execute(station);
        }
    }
}

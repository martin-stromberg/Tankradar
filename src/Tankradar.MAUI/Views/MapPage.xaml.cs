using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Map;
using Tankradar.MAUI.ViewModels;
using Tankradar.MAUI.Views.Controls;

namespace Tankradar.MAUI.Views;

/// <summary>
/// Seite für den Bereich „Karte" (Suche): Vollflächenkarte mit Suchdialog (Standort, Adresse, Route), Trefferliste am unteren Rand und Nachladen des sichtbaren Ausschnitts.
/// Reicht Bewegungen und das Einpassen der Karte an das <see cref="MapViewModel"/> weiter und meldet das Ausblenden der Seite.
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

    /// <inheritdoc />
    protected override bool OnBackButtonPressed()
    {
        // Die Rückwärtstaste schließt zuerst den Suchdialog; die Eingaben bleiben erhalten.
        if (BindingContext is MapViewModel { IsSearchOverlayOpen: true } viewModel)
        {
            viewModel.CloseSearchOverlayCommand.Execute(null);
            return true;
        }

        return base.OnBackButtonPressed();
    }

    private void OnMapViewportChanged(object? sender, ViewportChangedEventArgs e)
    {
        if (BindingContext is MapViewModel viewModel)
        {
            viewModel.OnViewportChanged(e.Viewport);
        }
    }

    private void OnMapViewportFitted(object? sender, ViewportChangedEventArgs e)
    {
        if (BindingContext is MapViewModel viewModel)
        {
            viewModel.OnViewportFitted(e.Viewport);
        }
    }

    private async void OnAttributionClicked(object? sender, EventArgs e)
    {
        try
        {
            await Launcher.Default.OpenAsync(new Uri(MapTexts.AttributionUrl));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Ohne Browser bleibt die Quellenangabe als Text sichtbar; mehr ist nicht zu tun.
        }
    }
}

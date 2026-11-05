using Tankradar.MAUI.ViewModels;

namespace Tankradar.MAUI.Views;

/// <summary>
/// Detailansicht einer Tankstelle (Name, Adresse, Entfernung, Preise mit Alter, Hinweise, Öffnungszeiten), geöffnet aus der Ergebnisliste der Suche.
/// </summary>
public partial class StationDetailPage : TankradarContentPage
{
    /// <summary>
    /// Erstellt die Seite und setzt das per Dependency Injection bereitgestellte <see cref="StationDetailViewModel"/> als Bindungskontext.
    /// </summary>
    /// <param name="viewModel">Das per Dependency Injection bereitgestellte ViewModel der Seite.</param>
    public StationDetailPage(StationDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

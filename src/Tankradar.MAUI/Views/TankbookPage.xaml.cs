using Tankradar.MAUI.ViewModels;

namespace Tankradar.MAUI.Views;

/// <summary>
/// Fahrzeugverwaltung des Tankbuchs (aus dem Tab „Tankbuch“ geöffnet): Fahrzeugliste, Formular zum Anlegen und Bearbeiten sowie Rückfrage zum Löschen als Bereiche in der Seite.
/// </summary>
public partial class TankbookPage : TankradarContentPage
{
    /// <summary>
    /// Erstellt die Seite und setzt das per Dependency Injection bereitgestellte <see cref="TankbookViewModel"/> als Bindungskontext.
    /// </summary>
    /// <param name="viewModel">Das per Dependency Injection bereitgestellte ViewModel der Seite.</param>
    public TankbookPage(TankbookViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

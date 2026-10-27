using Tankradar.MAUI.ViewModels;

namespace Tankradar.MAUI.Views;

/// <summary>
/// Seite des Tabs „Tankbuch“: Tank- und Ladevorgänge des gewählten Fahrzeugs mit Fahrzeugwechsel im Kopf; Liste, Formular und Rückfrage zum Löschen als Bereiche in der Seite.
/// </summary>
public partial class TankbookEntriesPage : TankradarContentPage
{
    /// <summary>
    /// Erstellt die Seite und setzt das per Dependency Injection bereitgestellte <see cref="TankbookEntriesViewModel"/> als Bindungskontext.
    /// </summary>
    /// <param name="viewModel">Das per Dependency Injection bereitgestellte ViewModel der Seite.</param>
    public TankbookEntriesPage(TankbookEntriesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

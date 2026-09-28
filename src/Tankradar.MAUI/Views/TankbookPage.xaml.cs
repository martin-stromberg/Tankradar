using Tankradar.MAUI.ViewModels;

namespace Tankradar.MAUI.Views;

/// <summary>
/// Seite für den Bereich „Tankbuch". Aktuell ohne Inhalt außer Platzhaltertext.
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

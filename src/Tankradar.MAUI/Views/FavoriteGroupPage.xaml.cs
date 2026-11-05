using Tankradar.MAUI.ViewModels;

namespace Tankradar.MAUI.Views;

/// <summary>
/// Gruppenansicht einer Favoritengruppe (Name, Beschreibung, Tankstellen nach Priorität mit Notiz), geöffnet aus der Gruppenübersicht im Bereich „Favoriten“.
/// </summary>
public partial class FavoriteGroupPage : TankradarContentPage
{
    /// <summary>
    /// Erstellt die Seite und setzt das per Dependency Injection bereitgestellte <see cref="FavoriteGroupViewModel"/> als Bindungskontext.
    /// </summary>
    /// <param name="viewModel">Das per Dependency Injection bereitgestellte ViewModel der Seite.</param>
    public FavoriteGroupPage(FavoriteGroupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

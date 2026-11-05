using Tankradar.MAUI.ViewModels;

namespace Tankradar.MAUI.Views;

/// <summary>
/// Startseite („Favoriten“): Favoritengruppen mit Tankstellenkacheln (Preise, Entfernung, Hinweise), Offline-Hinweis, Aktualisierung, optionaler Bereich „In der Nähe“ und Anlegen einer neuen Gruppe; Antippen einer Tankstelle öffnet die Detailansicht, einer Gruppe die Gruppenansicht.
/// </summary>
public partial class FavoritesPage : TankradarContentPage
{
    /// <summary>
    /// Erstellt die Seite und setzt das per Dependency Injection bereitgestellte <see cref="FavoritesViewModel"/> als Bindungskontext.
    /// </summary>
    /// <param name="viewModel">Das per Dependency Injection bereitgestellte ViewModel der Seite.</param>
    public FavoritesPage(FavoritesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

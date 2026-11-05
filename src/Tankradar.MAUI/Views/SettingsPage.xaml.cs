using Tankradar.MAUI.ViewModels;

namespace Tankradar.MAUI.Views;

/// <summary>
/// Seite für den Bereich „Optionen" (Einstellungen). Zeigt die Einstellungen an und speichert Änderungen sofort.
/// </summary>
public partial class SettingsPage : TankradarContentPage
{
    /// <summary>
    /// Erstellt die Seite und setzt das per Dependency Injection bereitgestellte <see cref="SettingsViewModel"/> als Bindungskontext.
    /// </summary>
    /// <param name="viewModel">Das per Dependency Injection bereitgestellte ViewModel der Seite.</param>
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

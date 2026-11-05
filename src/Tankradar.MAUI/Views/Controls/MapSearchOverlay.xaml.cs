using Tankradar.MAUI.ViewModels;

namespace Tankradar.MAUI.Views.Controls;

/// <summary>
/// Suchdialog über der Karte: Suchart (Route nur bei aktivierter Routensuche), Adresseingabe, Start, Ziel und Abstand der Routensuche, Radius und Suchen.
/// Die Abdunklung fängt Eingaben für Karte und Trefferliste ab, solange der Dialog offen ist. Der Bindungskontext ist das <see cref="MapViewModel"/> der Seite;
/// die Eingaben leben dort und bleiben beim Schließen erhalten.
/// </summary>
public partial class MapSearchOverlay : ContentView
{
    /// <summary>
    /// Erstellt den Suchdialog.
    /// </summary>
    public MapSearchOverlay()
    {
        InitializeComponent();
    }
}

namespace Tankradar.MAUI.Resources;

/// <summary>
/// Code-Behind der gemeinsamen Stile und Abstände der Kartenansicht (Chip-Schaltflächen, Auswahl-Chips, Abstände der schwebenden Elemente über der Karte).
/// Die Klasse ist bewusst <c>internal</c>, damit der XAML-Source-Generator keinen undokumentierten öffentlichen Typ erzeugt (CS1591).
/// </summary>
internal partial class MapStyles : ResourceDictionary
{
    /// <summary>
    /// Initialisiert die Stile und lädt die XAML-Ressourcen.
    /// </summary>
    public MapStyles()
    {
        InitializeComponent();
    }
}

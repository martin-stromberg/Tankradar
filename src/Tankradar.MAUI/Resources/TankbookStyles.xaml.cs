namespace Tankradar.MAUI.Resources;

/// <summary>
/// Code-Behind der gemeinsamen Stile der Tankbuch-Seiten (Karten, Schaltflächen, Chips, Eingabefelder mit Einheit, Feldbeschriftungen und -fehler).
/// Die Klasse ist bewusst <c>internal</c>, damit der XAML-Source-Generator keinen undokumentierten öffentlichen Typ erzeugt (CS1591).
/// </summary>
internal partial class TankbookStyles : ResourceDictionary
{
    /// <summary>
    /// Initialisiert die Stile und lädt die XAML-Ressourcen.
    /// </summary>
    public TankbookStyles()
    {
        InitializeComponent();
    }
}

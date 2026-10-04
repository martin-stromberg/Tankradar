namespace Tankradar.MAUI.Resources;

/// <summary>
/// Code-Behind des Design-Systems (Farben, Schriften, Stile). Die Klasse ist bewusst <c>internal</c>,
/// damit der XAML-Source-Generator keinen undokumentierten öffentlichen Typ erzeugt (CS1591).
/// </summary>
internal partial class DesignSystem : ResourceDictionary
{
    /// <summary>
    /// Initialisiert das Design-System und lädt die XAML-Ressourcen.
    /// </summary>
    public DesignSystem()
    {
        InitializeComponent();
    }
}

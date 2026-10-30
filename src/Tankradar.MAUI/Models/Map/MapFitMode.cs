namespace Tankradar.MAUI.Models.Map;

/// <summary>
/// Legt fest, ob die Karte den Ausschnitt nach einem neuen Ergebnis auf die Markierungen einpasst.
/// </summary>
public enum MapFitMode
{
    /// <summary>
    /// Neue Suche: Der Ausschnitt wird auf Markierungen, Suchposition und Route eingepasst.
    /// </summary>
    FitToResult,

    /// <summary>
    /// Nachladen des sichtbaren Bereichs: Der Ausschnitt des Anwenders bleibt erhalten.
    /// </summary>
    KeepViewport,
}

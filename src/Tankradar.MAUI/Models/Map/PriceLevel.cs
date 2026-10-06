namespace Tankradar.MAUI.Models.Map;

/// <summary>
/// Preisniveau einer Tankstelle innerhalb der aktuellen Ergebnismenge (bestimmt die Farbe der Markierung auf der Karte).
/// </summary>
public enum PriceLevel
{
    /// <summary>
    /// Günstigster Preis der Ergebnismenge (Grün).
    /// </summary>
    Cheapest,

    /// <summary>
    /// Alle übrigen Preise (Teal).
    /// </summary>
    Normal,

    /// <summary>
    /// Preis im oberen Drittel der Spanne zwischen niedrigstem und höchstem Preis (Rot).
    /// </summary>
    Expensive,

    /// <summary>
    /// Geschlossene Tankstelle (Grau).
    /// </summary>
    Closed,
}

namespace Tankradar.MAUI.Resources;

/// <summary>
/// Berechnet die Schriftgröße der großen Zahl einer Auswertungskachel nach der Länge des Textes, damit die Zahl auch bei schmalen Kacheln
/// (360 bis 375 pt Fensterbreite) nicht mitten im Wert umbricht.
/// </summary>
public static class StatisticsTileFont
{
    /// <summary>
    /// Standardschriftgröße (Stil <c>price-hero</c>) für kurze Zahlen.
    /// </summary>
    public const double DefaultSize = 28;

    /// <summary>
    /// Kleinste Schriftgröße für lange Zahlen.
    /// </summary>
    public const double MinimumSize = 18;

    /// <summary>
    /// Ermittelt die Schriftgröße für einen Text.
    /// </summary>
    /// <param name="text">Der Zahlentext, z. B. „9.989,00“.</param>
    /// <returns>Bis 6 Zeichen 28 pt, bei 7 Zeichen 24 pt, bei 8 Zeichen 21 pt, ab 9 Zeichen 18 pt.</returns>
    public static double SizeFor(string? text)
    {
        var length = text?.Length ?? 0;
        return length switch
        {
            <= 6 => DefaultSize,
            7 => 24,
            8 => 21,
            _ => MinimumSize,
        };
    }
}

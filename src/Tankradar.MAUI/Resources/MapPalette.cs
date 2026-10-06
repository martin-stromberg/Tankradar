using Tankradar.MAUI.Models.Map;

namespace Tankradar.MAUI.Resources;

/// <summary>
/// Die Farben der Preisniveaus (Markierungen und Legende der Karte). Weißer Text hat auf allen Farben mindestens 4,5:1 Kontrast.
/// </summary>
public static class MapPalette
{
    /// <summary>
    /// Grün: günstigster Preis.
    /// </summary>
    /// <returns>Die Farbe.</returns>
    public static readonly Color Cheapest = Color.FromArgb("#006C49");

    /// <summary>
    /// Teal: alle übrigen Preise.
    /// </summary>
    /// <returns>Die Farbe.</returns>
    public static readonly Color Normal = Color.FromArgb("#0F766E");

    /// <summary>
    /// Rot: Preise im oberen Drittel der Preisspanne.
    /// </summary>
    /// <returns>Die Farbe.</returns>
    public static readonly Color Expensive = Color.FromArgb("#BA1A1A");

    /// <summary>
    /// Grau: geschlossene Tankstellen.
    /// </summary>
    /// <returns>Die Farbe.</returns>
    public static readonly Color Closed = Color.FromArgb("#64748B");

    /// <summary>
    /// Liefert die Farbe eines Preisniveaus.
    /// </summary>
    /// <param name="level">Das Preisniveau.</param>
    /// <returns>Die Farbe.</returns>
    public static Color For(PriceLevel level)
    {
        return level switch
        {
            PriceLevel.Cheapest => Cheapest,
            PriceLevel.Expensive => Expensive,
            PriceLevel.Closed => Closed,
            _ => Normal,
        };
    }
}

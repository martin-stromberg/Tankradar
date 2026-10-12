namespace Tankradar.MAUI.Resources;

/// <summary>
/// Die Farben der Diagramme in hellem und dunklem Erscheinungsbild; abgeleitet aus den Werten von <c>DesignSystem.xaml</c>
/// (Teal <c>#0F766E</c>, Text und Rahmen). Linie bzw. Balken haben gegen die Kartenfläche mindestens 3:1 Kontrast.
/// </summary>
public static class ChartPalette
{
    /// <summary>
    /// Liefert die Farbe der Linie bzw. der Balken.
    /// </summary>
    /// <param name="isDark">Gibt an, ob das dunkle Erscheinungsbild gilt.</param>
    /// <returns>Die Farbe.</returns>
    public static Color Series(bool isDark)
    {
        return isDark ? Color.FromArgb("#2DD4BF") : Color.FromArgb("#0F766E");
    }

    /// <summary>
    /// Liefert die Farbe der Achsenlinie.
    /// </summary>
    /// <param name="isDark">Gibt an, ob das dunkle Erscheinungsbild gilt.</param>
    /// <returns>Die Farbe.</returns>
    public static Color Axis(bool isDark)
    {
        return isDark ? Color.FromArgb("#94A3B8") : Color.FromArgb("#64748B");
    }

    /// <summary>
    /// Liefert die Farbe der Werte und Beschriftungen.
    /// </summary>
    /// <param name="isDark">Gibt an, ob das dunkle Erscheinungsbild gilt.</param>
    /// <returns>Die Farbe.</returns>
    public static Color Text(bool isDark)
    {
        return isDark ? Color.FromArgb("#EEF0FF") : Color.FromArgb("#0F172A");
    }
}

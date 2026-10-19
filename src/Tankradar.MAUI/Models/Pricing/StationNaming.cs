namespace Tankradar.MAUI.Models.Pricing;

/// <summary>
/// Regeln für den angezeigten Namen einer Tankstelle. Bei manchen Tankstellen liefert die Quelle als Namen den Inhaber; deshalb steht die Marke vorn
/// und der Stammdatenname klein darunter. Ein eigener Anzeigename des Anwenders hat überall Vorrang.
/// </summary>
public static class StationNaming
{
    /// <summary>
    /// Die höchste Länge eines eigenen Anzeigenamens in Zeichen.
    /// </summary>
    public const int MaxCustomNameLength = 60;

    /// <summary>
    /// Liefert den prominent angezeigten Namen: der eigene Anzeigename, sonst die Marke, sonst der Name der Quelle.
    /// </summary>
    /// <param name="customName">Der eigene Anzeigename; <see langword="null"/> oder leer, wenn keiner vergeben ist.</param>
    /// <param name="brand">Die Marke; <see langword="null"/> oder leer, wenn die Quelle keine liefert.</param>
    /// <param name="name">Der Name der Quelle.</param>
    /// <returns>Der Anzeigename.</returns>
    public static string GetPrimary(string? customName, string? brand, string? name)
    {
        var custom = Clean(customName);
        if (custom.Length > 0)
        {
            return custom;
        }

        var mark = Clean(brand);
        return mark.Length > 0 ? mark : Clean(name);
    }

    /// <summary>
    /// Liefert den klein darunter angezeigten Stammdatennamen; leer, wenn er dem Anzeigenamen entspricht (Groß-/Kleinschreibung unbeachtet).
    /// </summary>
    /// <param name="customName">Der eigene Anzeigename.</param>
    /// <param name="brand">Die Marke.</param>
    /// <param name="name">Der Name der Quelle.</param>
    /// <returns>Der Stammdatenname oder ein leerer Text.</returns>
    public static string GetSecondary(string? customName, string? brand, string? name)
    {
        var primary = GetPrimary(customName, brand, name);
        var source = Clean(name);
        return string.Equals(primary, source, StringComparison.OrdinalIgnoreCase) ? string.Empty : source;
    }

    private static string Clean(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }
}

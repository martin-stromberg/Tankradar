namespace Tankradar.MAUI.Models;

/// <summary>
/// Gibt an, wann die App den Standort des Geräts verwenden darf. Die Namen der Werte sind ein Persistenzvertrag.
/// </summary>
public enum GpsUsage
{
    /// <summary>
    /// Der Standort wird immer verwendet.
    /// </summary>
    Always,

    /// <summary>
    /// Der Standort wird nur verwendet, solange die App genutzt wird.
    /// </summary>
    WhileInUse,

    /// <summary>
    /// Der Standort wird nie verwendet.
    /// </summary>
    Never,
}

/// <summary>
/// Erweiterungen zu <see cref="GpsUsage"/>.
/// </summary>
public static class GpsUsageExtensions
{
    /// <summary>
    /// Gibt an, ob die Einstellung eine Standortabfrage erlaubt (nur „Immer“ und „Nur bei Nutzung“; alles andere, auch undefinierte Werte, sperrt: Fail Secure).
    /// </summary>
    /// <param name="usage">Die Einstellung.</param>
    /// <returns><see langword="true"/>, wenn eine Abfrage zulässig ist.</returns>
    public static bool AllowsLocation(this GpsUsage usage)
    {
        return usage is GpsUsage.Always or GpsUsage.WhileInUse;
    }
}

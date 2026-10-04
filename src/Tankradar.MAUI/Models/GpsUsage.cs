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

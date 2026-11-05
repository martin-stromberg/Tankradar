namespace Tankradar.MAUI.Models.Pricing;

/// <summary>
/// Ergebnis des Vergebens oder Entfernens eines eigenen Anzeigenamens.
/// </summary>
public enum StationNameResult
{
    /// <summary>
    /// Der Name wurde gespeichert bzw. entfernt.
    /// </summary>
    Ok,

    /// <summary>
    /// Der Name ist länger als <see cref="StationNaming.MaxCustomNameLength"/> Zeichen.
    /// </summary>
    TooLong,

    /// <summary>
    /// Die Tankstelle ist lokal nicht bekannt.
    /// </summary>
    NotFound,
}

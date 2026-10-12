namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Ergebnis des Ladens einer Auswertung.
/// </summary>
public enum TankbookStatisticsStatus
{
    /// <summary>
    /// Die Auswertung liegt vor.
    /// </summary>
    Ok,

    /// <summary>
    /// Das Fahrzeug gibt es nicht (mehr).
    /// </summary>
    VehicleNotFound,
}

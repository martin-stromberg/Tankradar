namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Herkunft des angezeigten Durchschnittsverbrauchs einer Art.
/// </summary>
public enum TankbookAverageSource
{
    /// <summary>
    /// Es gibt keinen Durchschnittsverbrauch.
    /// </summary>
    None,

    /// <summary>
    /// Der Durchschnitt wurde aus mindestens einem berechneten Intervall ermittelt.
    /// </summary>
    Calculated,

    /// <summary>
    /// Ersatzwert: der Startverbrauch des Fahrzeugs, solange kein Intervall berechnet werden kann.
    /// </summary>
    StartConsumption,
}

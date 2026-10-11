namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Ergebnis einer Änderung an Fahrzeugen.
/// </summary>
public enum VehicleResult
{
    /// <summary>
    /// Die Änderung wurde gespeichert.
    /// </summary>
    Ok,

    /// <summary>
    /// Der Name ist leer oder zu lang.
    /// </summary>
    InvalidName,

    /// <summary>
    /// Ein Fahrzeug mit diesem Namen gibt es bereits (ohne Beachtung der Groß- und Kleinschreibung).
    /// </summary>
    DuplicateName,

    /// <summary>
    /// Die Kraftstoffart ist kein bekannter Wert.
    /// </summary>
    InvalidFuelKind,

    /// <summary>
    /// Der Start-Kilometerstand liegt außerhalb des erlaubten Bereichs.
    /// </summary>
    InvalidOdometer,

    /// <summary>
    /// Der Startverbrauch liegt außerhalb des erlaubten Bereichs oder hat zu viele Nachkommastellen.
    /// </summary>
    InvalidConsumption,

    /// <summary>
    /// Das Fahrzeug gibt es nicht (mehr).
    /// </summary>
    VehicleNotFound,
}

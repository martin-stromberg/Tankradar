namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Fehler eines einzelnen Felds im Fahrzeugformular.
/// </summary>
public enum VehicleFieldError
{
    /// <summary>
    /// Das Feld ist gültig.
    /// </summary>
    None,

    /// <summary>
    /// Ein Pflichtfeld ist leer.
    /// </summary>
    Required,

    /// <summary>
    /// Die Eingabe ist keine gültige Zahl.
    /// </summary>
    NotANumber,

    /// <summary>
    /// Die Zahl ist negativ.
    /// </summary>
    Negative,

    /// <summary>
    /// Die Zahl liegt außerhalb des erlaubten Bereichs.
    /// </summary>
    OutOfRange,

    /// <summary>
    /// Die Zahl hat zu viele Nachkommastellen.
    /// </summary>
    TooManyDecimals,

    /// <summary>
    /// Der Text ist zu lang.
    /// </summary>
    TooLong,
}

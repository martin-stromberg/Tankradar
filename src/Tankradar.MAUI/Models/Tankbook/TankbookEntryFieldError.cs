namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Fehler eines einzelnen Felds im Formular eines Tank- oder Ladevorgangs.
/// </summary>
public enum TankbookEntryFieldError
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

    /// <summary>
    /// Von Menge, Preis pro Einheit und Gesamtbetrag sind weniger als zwei Werte angegeben.
    /// </summary>
    MissingAmounts,

    /// <summary>
    /// Menge × Preis pro Einheit weicht vom Gesamtbetrag ab.
    /// </summary>
    AmountMismatch,

    /// <summary>
    /// Der Zeitpunkt liegt in der Zukunft.
    /// </summary>
    InFuture,
}

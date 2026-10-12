namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Ergebnis einer Änderung an Tank- und Ladevorgängen.
/// </summary>
public enum TankbookEntryResult
{
    /// <summary>
    /// Die Änderung wurde gespeichert.
    /// </summary>
    Ok,

    /// <summary>
    /// Das Fahrzeug gibt es nicht (mehr).
    /// </summary>
    VehicleNotFound,

    /// <summary>
    /// Den Vorgang gibt es nicht (mehr).
    /// </summary>
    EntryNotFound,

    /// <summary>
    /// Der Zeitpunkt fehlt oder liegt in der Zukunft.
    /// </summary>
    InvalidOccurredAt,

    /// <summary>
    /// Die Art des Vorgangs passt nicht zur Kraftstoffart des Fahrzeugs.
    /// </summary>
    KindNotAllowed,

    /// <summary>
    /// Die Menge liegt außerhalb des erlaubten Bereichs oder hat zu viele Nachkommastellen.
    /// </summary>
    InvalidQuantity,

    /// <summary>
    /// Der Preis pro Einheit liegt außerhalb des erlaubten Bereichs oder hat zu viele Nachkommastellen.
    /// </summary>
    InvalidUnitPrice,

    /// <summary>
    /// Der Gesamtbetrag liegt außerhalb des erlaubten Bereichs oder hat zu viele Nachkommastellen.
    /// </summary>
    InvalidTotalAmount,

    /// <summary>
    /// Menge × Preis pro Einheit weicht vom Gesamtbetrag ab.
    /// </summary>
    AmountMismatch,

    /// <summary>
    /// Der Kilometerstand liegt außerhalb des erlaubten Bereichs.
    /// </summary>
    InvalidOdometer,

    /// <summary>
    /// Der Kilometerstand liegt unter dem Start-Kilometerstand des Fahrzeugs.
    /// </summary>
    OdometerBelowVehicleStart,

    /// <summary>
    /// Der Kilometerstand liegt unter dem des chronologisch vorherigen Vorgangs.
    /// </summary>
    OdometerBelowPrevious,

    /// <summary>
    /// Der Kilometerstand liegt über dem des chronologisch nächsten Vorgangs.
    /// </summary>
    OdometerAboveNext,

    /// <summary>
    /// Der Name der Tankstelle bzw. des Ladeorts ist leer oder zu lang.
    /// </summary>
    InvalidStationName,

    /// <summary>
    /// Die Notiz ist zu lang.
    /// </summary>
    NoteTooLong,
}

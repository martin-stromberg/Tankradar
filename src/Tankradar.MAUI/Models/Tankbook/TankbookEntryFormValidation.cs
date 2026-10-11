namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Ergebnis der Prüfung des Formulars eines Tank- oder Ladevorgangs: je Feld ein Fehler und bei Erfolg die geprüfte Eingabe
/// mit dem gegebenenfalls berechneten dritten Wert.
/// </summary>
/// <param name="OccurredAtError">Der Fehler an Datum und Uhrzeit.</param>
/// <param name="StationError">Der Fehler an der Tankstelle bzw. dem Ladeort.</param>
/// <param name="QuantityError">Der Fehler an der Menge.</param>
/// <param name="UnitPriceError">Der Fehler am Preis pro Einheit.</param>
/// <param name="TotalAmountError">Der Fehler am Gesamtbetrag (auch fehlende Werte und Abweichungen der drei Werte).</param>
/// <param name="OdometerError">Der Fehler am Kilometerstand.</param>
/// <param name="NoteError">Der Fehler an der Notiz.</param>
/// <param name="ExpectedTotal">Der rechnerische Gesamtbetrag bei einer Abweichung, sonst <see langword="null"/>.</param>
/// <param name="Input">Die geprüfte Eingabe; <see langword="null"/>, wenn mindestens ein Feld fehlerhaft ist.</param>
/// <returns>Der Wert.</returns>
public sealed record TankbookEntryFormValidation(
    TankbookEntryFieldError OccurredAtError,
    TankbookEntryFieldError StationError,
    TankbookEntryFieldError QuantityError,
    TankbookEntryFieldError UnitPriceError,
    TankbookEntryFieldError TotalAmountError,
    TankbookEntryFieldError OdometerError,
    TankbookEntryFieldError NoteError,
    decimal? ExpectedTotal,
    TankbookEntryInput? Input)
{
    /// <summary>
    /// Gibt an, ob mindestens ein Feld fehlerhaft ist.
    /// </summary>
    public bool HasErrors
    {
        get
        {
            return OccurredAtError != TankbookEntryFieldError.None
                || StationError != TankbookEntryFieldError.None
                || QuantityError != TankbookEntryFieldError.None
                || UnitPriceError != TankbookEntryFieldError.None
                || TotalAmountError != TankbookEntryFieldError.None
                || OdometerError != TankbookEntryFieldError.None
                || NoteError != TankbookEntryFieldError.None;
        }
    }
}

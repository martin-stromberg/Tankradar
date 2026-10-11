namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Ergebnis der Prüfung des Fahrzeugformulars: je Feld ein Fehler und bei Erfolg die geprüfte Eingabe.
/// </summary>
/// <param name="NameError">Der Fehler am Namen.</param>
/// <param name="OdometerError">Der Fehler am Start-Kilometerstand.</param>
/// <param name="ConsumptionError">Der Fehler am Startverbrauch.</param>
/// <param name="Input">Die geprüfte Eingabe; <see langword="null"/>, wenn mindestens ein Feld fehlerhaft ist.</param>
/// <returns>Der Wert.</returns>
public sealed record VehicleFormValidation(
    VehicleFieldError NameError,
    VehicleFieldError OdometerError,
    VehicleFieldError ConsumptionError,
    VehicleInput? Input)
{
    /// <summary>
    /// Gibt an, ob mindestens ein Feld fehlerhaft ist.
    /// </summary>
    public bool HasErrors
    {
        get { return NameError != VehicleFieldError.None || OdometerError != VehicleFieldError.None || ConsumptionError != VehicleFieldError.None; }
    }
}

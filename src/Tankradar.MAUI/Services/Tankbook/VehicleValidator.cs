using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// Prüft Fahrzeugangaben: feldbezogen für das Formular und als Schutzprüfung des Dienstes.
/// </summary>
public static class VehicleValidator
{
    /// <summary>
    /// Prüft eine Eingabe vor dem Speichern (Schutzprüfung des Dienstes).
    /// </summary>
    /// <param name="input">Die Eingabe.</param>
    /// <returns><see cref="VehicleResult.Ok"/> bei gültiger Eingabe, sonst der erste gefundene Fehler.</returns>
    public static VehicleResult Validate(VehicleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var name = input.Name?.Trim() ?? string.Empty;
        if (name.Length == 0 || name.Length > VehicleLimits.MaxNameLength)
        {
            return VehicleResult.InvalidName;
        }

        if (!Enum.IsDefined(input.FuelKind))
        {
            return VehicleResult.InvalidFuelKind;
        }

        if (input.StartOdometerKm < 0 || input.StartOdometerKm > VehicleLimits.MaxOdometerKm)
        {
            return VehicleResult.InvalidOdometer;
        }

        if (input.StartConsumption is { } consumption
            && (consumption <= 0m || consumption >= VehicleLimits.MaxConsumptionExclusive || Normalize(consumption).Scale > VehicleLimits.MaxConsumptionDecimals))
        {
            return VehicleResult.InvalidConsumption;
        }

        return VehicleResult.Ok;
    }

    /// <summary>
    /// Prüft die Felder des Formulars; alle Feldfehler werden gleichzeitig ermittelt.
    /// </summary>
    /// <param name="name">Der eingegebene Name.</param>
    /// <param name="fuelKind">Die gewählte Kraftstoffart.</param>
    /// <param name="odometerText">Die Eingabe des Start-Kilometerstands.</param>
    /// <param name="consumptionText">Die Eingabe des optionalen Startverbrauchs.</param>
    /// <returns>Die Feldfehler und bei Erfolg die geprüfte Eingabe mit getrimmtem Namen.</returns>
    public static VehicleFormValidation ValidateForm(string name, VehicleFuelKind fuelKind, string odometerText, string consumptionText)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        var nameError = trimmedName.Length switch
        {
            0 => VehicleFieldError.Required,
            > VehicleLimits.MaxNameLength => VehicleFieldError.TooLong,
            _ => VehicleFieldError.None,
        };
        var odometerError = VehicleInputParser.ParseOdometer(odometerText, out var odometer);
        var consumptionError = VehicleInputParser.ParseConsumption(consumptionText, out var consumption);
        var hasErrors = nameError != VehicleFieldError.None || odometerError != VehicleFieldError.None || consumptionError != VehicleFieldError.None;
        var input = hasErrors ? null : new VehicleInput(trimmedName, fuelKind, odometer, consumption);
        return new VehicleFormValidation(nameError, odometerError, consumptionError, input);
    }

    /// <summary>
    /// Entfernt nachgestellte Nullen eines Dezimalwerts, damit seine Skala die tatsächlichen Nachkommastellen angibt.
    /// </summary>
    /// <param name="value">Der Wert.</param>
    /// <returns>Der gleiche Wert ohne nachgestellte Nullen.</returns>
    internal static decimal Normalize(decimal value)
    {
        return value / 1.000000000000000000000000000000000m;
    }
}

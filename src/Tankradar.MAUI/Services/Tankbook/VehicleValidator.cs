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

        if (input.StartConsumption is { } consumption && CheckConsumption(consumption) != VehicleFieldError.None)
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
        var validation = new VehicleFormValidation(nameError, odometerError, consumptionError, null);
        return validation.HasErrors ? validation : validation with { Input = new VehicleInput(trimmedName, fuelKind, odometer, consumption) };
    }

    /// <summary>
    /// Prüft den Wertebereich eines Startverbrauchs: nicht negativ, größer als 0 und kleiner als <see cref="VehicleLimits.MaxConsumptionExclusive"/>,
    /// höchstens <see cref="VehicleLimits.MaxConsumptionDecimals"/> Nachkommastellen (nachgestellte Nullen zählen nicht).
    /// </summary>
    /// <param name="consumption">Der Verbrauch.</param>
    /// <returns><see cref="VehicleFieldError.None"/> bei gültigem Wert, sonst der Fehler.</returns>
    public static VehicleFieldError CheckConsumption(decimal consumption)
    {
        if (consumption < 0m)
        {
            return VehicleFieldError.Negative;
        }

        if (consumption == 0m || consumption >= VehicleLimits.MaxConsumptionExclusive)
        {
            return VehicleFieldError.OutOfRange;
        }

        return Normalize(consumption).Scale > VehicleLimits.MaxConsumptionDecimals ? VehicleFieldError.TooManyDecimals : VehicleFieldError.None;
    }

    /// <summary>
    /// Liest den gespeicherten Namen einer Kraftstoffart (Persistenzvertrag <c>Vehicles.FuelKind</c>).
    /// </summary>
    /// <param name="value">Der gespeicherte Name.</param>
    /// <param name="fuelKind">Die Kraftstoffart; bei einem unbekannten Namen <see cref="VehicleFuelKind.Petrol"/>.</param>
    /// <returns><see langword="true"/>, wenn der Name eine bekannte Kraftstoffart ist.</returns>
    internal static bool TryParseFuelKind(string? value, out VehicleFuelKind fuelKind)
    {
        if (Enum.TryParse(value, out fuelKind) && Enum.IsDefined(fuelKind))
        {
            return true;
        }

        fuelKind = VehicleFuelKind.Petrol;
        return false;
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

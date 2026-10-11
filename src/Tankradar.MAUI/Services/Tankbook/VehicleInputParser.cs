using System.Globalization;
using System.Text.RegularExpressions;
using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// Erkennt die Zahleneingaben des Fahrzeugformulars in deutscher Schreibweise und prüft ihren Wertebereich.
/// </summary>
public static partial class VehicleInputParser
{
    /// <summary>
    /// Erkennt den Start-Kilometerstand: ganze Zahl aus Ziffern, optional mit Tausenderpunkten („84.350“); Leerzeichen am Rand werden ignoriert.
    /// </summary>
    /// <param name="text">Die Eingabe.</param>
    /// <param name="value">Der erkannte Kilometerstand; 0, wenn die Eingabe fehlerhaft ist.</param>
    /// <returns><see cref="VehicleFieldError.None"/> bei gültiger Eingabe, sonst der Fehler.</returns>
    public static VehicleFieldError ParseOdometer(string? text, out int value)
    {
        value = 0;
        var trimmed = text?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return VehicleFieldError.Required;
        }

        if (!OdometerPattern().IsMatch(trimmed))
        {
            return VehicleFieldError.NotANumber;
        }

        if (trimmed.StartsWith('-'))
        {
            return VehicleFieldError.Negative;
        }

        var digits = trimmed.Replace(".", string.Empty, StringComparison.Ordinal);
        if (!long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed > VehicleLimits.MaxOdometerKm)
        {
            return VehicleFieldError.OutOfRange;
        }

        value = (int)parsed;
        return VehicleFieldError.None;
    }

    /// <summary>
    /// Erkennt den optionalen Startverbrauch: Dezimalzahl mit Komma, ein einzelner Punkt gilt ebenfalls als Dezimaltrennzeichen („5.4“ = 5,4);
    /// Leerzeichen am Rand werden ignoriert. Eine leere Eingabe ist gültig und ergibt <see langword="null"/>.
    /// </summary>
    /// <param name="text">Die Eingabe.</param>
    /// <param name="value">Der erkannte Verbrauch ohne nachgestellte Nullen; <see langword="null"/> bei leerer oder fehlerhafter Eingabe.</param>
    /// <returns><see cref="VehicleFieldError.None"/> bei gültiger oder leerer Eingabe, sonst der Fehler.</returns>
    public static VehicleFieldError ParseConsumption(string? text, out decimal? value)
    {
        value = null;
        var trimmed = text?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return VehicleFieldError.None;
        }

        if (!ConsumptionPattern().IsMatch(trimmed)
            || !decimal.TryParse(trimmed.Replace(',', '.'), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed))
        {
            return VehicleFieldError.NotANumber;
        }

        if (parsed < 0m)
        {
            return VehicleFieldError.Negative;
        }

        if (parsed <= 0m || parsed >= VehicleLimits.MaxConsumptionExclusive)
        {
            return VehicleFieldError.OutOfRange;
        }

        var normalized = VehicleValidator.Normalize(parsed);
        if (normalized.Scale > VehicleLimits.MaxConsumptionDecimals)
        {
            return VehicleFieldError.TooManyDecimals;
        }

        value = normalized;
        return VehicleFieldError.None;
    }

    [GeneratedRegex(@"^-?(\d{1,3}(\.\d{3})+|\d+)$", RegexOptions.CultureInvariant)]
    private static partial Regex OdometerPattern();

    [GeneratedRegex(@"^-?(\d+([,.]\d+)?|[,.]\d+)$", RegexOptions.CultureInvariant)]
    private static partial Regex ConsumptionPattern();
}

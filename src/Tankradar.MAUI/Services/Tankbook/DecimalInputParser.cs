using System.Globalization;
using System.Text.RegularExpressions;
using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// Erkennt Dezimaleingaben (Menge, Preis pro Einheit, Gesamtbetrag) in deutscher Schreibweise und prüft ihren Wertebereich.
/// </summary>
public static partial class DecimalInputParser
{
    /// <summary>
    /// Erkennt eine optionale Dezimalzahl: Komma als Dezimaltrennzeichen, ein einzelner Punkt gilt ebenfalls als Dezimaltrennzeichen („1.859“ = 1,859);
    /// Leerzeichen am Rand werden ignoriert. Eine leere Eingabe ist gültig und ergibt <see langword="null"/>.
    /// </summary>
    /// <param name="text">Die Eingabe.</param>
    /// <param name="maxDecimals">Die Höchstzahl der Nachkommastellen (nachgestellte Nullen zählen nicht).</param>
    /// <param name="maxExclusive">Die Obergrenze (ausschließlich); der Wert muss außerdem größer als 0 sein.</param>
    /// <param name="value">Der erkannte Wert ohne nachgestellte Nullen; <see langword="null"/> bei leerer oder fehlerhafter Eingabe.</param>
    /// <returns><see cref="TankbookEntryFieldError.None"/> bei gültiger oder leerer Eingabe, sonst der Fehler.</returns>
    public static TankbookEntryFieldError Parse(string? text, int maxDecimals, decimal maxExclusive, out decimal? value)
    {
        value = null;
        var trimmed = text?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return TankbookEntryFieldError.None;
        }

        if (!DecimalPattern().IsMatch(trimmed)
            || !decimal.TryParse(trimmed.Replace(',', '.'), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed))
        {
            return TankbookEntryFieldError.NotANumber;
        }

        var error = Check(parsed, maxDecimals, maxExclusive);
        if (error == TankbookEntryFieldError.None)
        {
            value = VehicleValidator.Normalize(parsed);
        }

        return error;
    }

    /// <summary>
    /// Prüft den Wertebereich eines Dezimalwerts: nicht negativ, größer als 0, kleiner als <paramref name="maxExclusive"/> und höchstens
    /// <paramref name="maxDecimals"/> Nachkommastellen (nachgestellte Nullen zählen nicht).
    /// </summary>
    /// <param name="value">Der Wert.</param>
    /// <param name="maxDecimals">Die Höchstzahl der Nachkommastellen.</param>
    /// <param name="maxExclusive">Die Obergrenze (ausschließlich).</param>
    /// <returns><see cref="TankbookEntryFieldError.None"/> bei gültigem Wert, sonst der Fehler.</returns>
    public static TankbookEntryFieldError Check(decimal value, int maxDecimals, decimal maxExclusive)
    {
        if (value < 0m)
        {
            return TankbookEntryFieldError.Negative;
        }

        if (value == 0m || value >= maxExclusive)
        {
            return TankbookEntryFieldError.OutOfRange;
        }

        return VehicleValidator.Normalize(value).Scale > maxDecimals ? TankbookEntryFieldError.TooManyDecimals : TankbookEntryFieldError.None;
    }

    [GeneratedRegex(@"^-?(\d+([,.]\d+)?|[,.]\d+)$", RegexOptions.CultureInvariant)]
    private static partial Regex DecimalPattern();
}

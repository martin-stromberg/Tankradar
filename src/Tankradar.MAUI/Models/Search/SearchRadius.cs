using System.Globalization;

namespace Tankradar.MAUI.Models.Search;

/// <summary>
/// Der Suchradius der Umkreissuche in Kilometern (ganze Zahl von 1 bis 25).
/// </summary>
public static class SearchRadius
{
    /// <summary>
    /// Der kleinste zulässige Radius.
    /// </summary>
    public const int Min = 1;

    /// <summary>
    /// Der größte zulässige Radius (von der Preisquelle vorgegeben).
    /// </summary>
    public const int Max = 25;

    /// <summary>
    /// Der Standardradius.
    /// </summary>
    public const int Default = 5;

    /// <summary>
    /// Wandelt eine Eingabe in einen gültigen Radius um (nur ganze Ziffern, keine Dezimalzahlen).
    /// </summary>
    /// <param name="text">Die Eingabe.</param>
    /// <param name="radiusKm">Der Radius bei Erfolg, sonst 0.</param>
    /// <returns><see langword="true"/>, wenn die Eingabe eine ganze Zahl von 1 bis 25 ist.</returns>
    public static bool TryParse(string? text, out int radiusKm)
    {
        radiusKm = 0;
        var trimmed = text?.Trim();
        if (string.IsNullOrEmpty(trimmed) || !trimmed.All(char.IsAsciiDigit))
        {
            return false;
        }

        if (!int.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value is < Min or > Max)
        {
            return false;
        }

        radiusKm = value;
        return true;
    }
}

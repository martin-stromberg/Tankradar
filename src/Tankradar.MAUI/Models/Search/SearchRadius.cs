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
    /// Die in der Oberfläche wählbaren Radiusstufen in Kilometern (alle innerhalb von <see cref="Min"/> bis <see cref="Max"/>).
    /// </summary>
    public static readonly IReadOnlyList<int> Steps = [1, 2, 5, 10, 15, 25];

    /// <summary>
    /// Prüft, ob ein Radius im zulässigen Bereich liegt.
    /// </summary>
    /// <param name="radiusKm">Der Radius in Kilometern.</param>
    /// <returns><see langword="true"/>, wenn der Radius von 1 bis 25 km reicht.</returns>
    public static bool IsValid(int radiusKm)
    {
        return radiusKm is >= Min and <= Max;
    }

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

        if (!int.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || !IsValid(value))
        {
            return false;
        }

        radiusKm = value;
        return true;
    }
}

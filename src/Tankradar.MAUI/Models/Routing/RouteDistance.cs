using System.Globalization;

namespace Tankradar.MAUI.Models.Routing;

/// <summary>
/// Der Abstand zur Route in Kilometern, innerhalb dessen Tankstellen berücksichtigt werden (Stufen 1, 2, 5 und 10 km).
/// </summary>
public static class RouteDistance
{
    /// <summary>
    /// Der Standardabstand.
    /// </summary>
    public const int Default = 2;

    /// <summary>
    /// Die in der Oberfläche wählbaren Abstandsstufen in Kilometern.
    /// </summary>
    public static readonly IReadOnlyList<int> Options = [1, 2, 5, 10];

    /// <summary>
    /// Prüft, ob der Abstand einer der zulässigen Stufen entspricht.
    /// </summary>
    /// <param name="distanceKm">Der Abstand in Kilometern.</param>
    /// <returns><see langword="true"/>, wenn der Abstand 1, 2, 5 oder 10 km beträgt.</returns>
    public static bool IsValid(int distanceKm)
    {
        return Options.Contains(distanceKm);
    }

    /// <summary>
    /// Der Schlüssel einer Stufe für Automatisierungskennungen (invariante Kultur).
    /// </summary>
    /// <param name="distanceKm">Der Abstand in Kilometern.</param>
    /// <returns>Der Schlüssel, z. B. <c>2</c>.</returns>
    public static string GetKey(int distanceKm)
    {
        return distanceKm.ToString(CultureInfo.InvariantCulture);
    }
}

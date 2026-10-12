namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Grenzen der Angaben eines Fahrzeugs.
/// </summary>
public static class VehicleLimits
{
    /// <summary>
    /// Höchstzahl der Zeichen eines Fahrzeugnamens.
    /// </summary>
    public const int MaxNameLength = 40;

    /// <summary>
    /// Höchster erlaubter Start-Kilometerstand in Kilometern.
    /// </summary>
    public const int MaxOdometerKm = 9_999_999;

    /// <summary>
    /// Obergrenze (ausschließlich) des Startverbrauchs pro 100 km.
    /// </summary>
    public const decimal MaxConsumptionExclusive = 100m;

    /// <summary>
    /// Höchstzahl der Nachkommastellen des Startverbrauchs.
    /// </summary>
    public const int MaxConsumptionDecimals = 2;
}

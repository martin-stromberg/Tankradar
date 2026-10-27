using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// Bestimmt das im Tab „Tankbuch“ angezeigte Fahrzeug: das gemerkte, sofern es noch existiert; sonst das erste nach Name; ohne Fahrzeuge keines.
/// </summary>
public static class VehicleSelectionResolver
{
    /// <summary>
    /// Bestimmt das anzuzeigende Fahrzeug.
    /// </summary>
    /// <param name="sortedByName">Die Fahrzeuge nach Namen geordnet.</param>
    /// <param name="storedId">Die gemerkte Kennung; <see langword="null"/>, wenn keine gemerkt ist.</param>
    /// <returns>Das Fahrzeug oder <see langword="null"/>, wenn es keine Fahrzeuge gibt.</returns>
    public static Vehicle? Resolve(IReadOnlyList<Vehicle> sortedByName, long? storedId)
    {
        ArgumentNullException.ThrowIfNull(sortedByName);
        if (storedId is { } id && sortedByName.FirstOrDefault(vehicle => vehicle.Id == id) is { } stored)
        {
            return stored;
        }

        return sortedByName.Count > 0 ? sortedByName[0] : null;
    }
}

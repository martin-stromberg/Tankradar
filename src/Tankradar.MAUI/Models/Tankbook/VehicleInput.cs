namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Eingabe zum Anlegen oder Ändern eines Fahrzeugs.
/// </summary>
/// <param name="Name">Der Name des Fahrzeugs.</param>
/// <param name="FuelKind">Die Kraftstoffart.</param>
/// <param name="StartOdometerKm">Der Start-Kilometerstand in ganzen Kilometern.</param>
/// <param name="StartConsumption">Der optionale Startverbrauch pro 100 km; <see langword="null"/>, wenn keiner angegeben ist.</param>
/// <returns>Der Wert.</returns>
public sealed record VehicleInput(string Name, VehicleFuelKind FuelKind, int StartOdometerKm, decimal? StartConsumption);

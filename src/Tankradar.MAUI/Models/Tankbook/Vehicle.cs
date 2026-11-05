namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Ein Fahrzeug im Tankbuch.
/// </summary>
/// <param name="Id">Die Kennung des Fahrzeugs.</param>
/// <param name="Name">Der Name des Fahrzeugs.</param>
/// <param name="FuelKind">Die Kraftstoffart.</param>
/// <param name="StartOdometerKm">Der Start-Kilometerstand in ganzen Kilometern.</param>
/// <param name="StartConsumption">Der optionale Startverbrauch pro 100 km; <see langword="null"/>, wenn keiner hinterlegt ist.</param>
/// <returns>Der Wert.</returns>
public sealed record Vehicle(long Id, string Name, VehicleFuelKind FuelKind, int StartOdometerKm, decimal? StartConsumption);

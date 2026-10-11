namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Ergebnis des Anlegens eines Fahrzeugs.
/// </summary>
/// <param name="Result">Das Ergebnis der Änderung.</param>
/// <param name="Vehicle">Das angelegte Fahrzeug; <see langword="null"/>, wenn <paramref name="Result"/> nicht <see cref="VehicleResult.Ok"/> ist.</param>
/// <returns>Der Wert.</returns>
public sealed record VehicleSaveResult(VehicleResult Result, Vehicle? Vehicle);

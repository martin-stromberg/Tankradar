namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Ergebnis des Löschens eines Fahrzeugs.
/// </summary>
/// <param name="Result">Das Ergebnis der Änderung.</param>
/// <param name="EntryCount">Die beim Bestätigen ermittelte Zahl der Tank- und Ladevorgänge des Fahrzeugs.</param>
/// <returns>Der Wert.</returns>
public sealed record VehicleDeleteResult(VehicleResult Result, int EntryCount);

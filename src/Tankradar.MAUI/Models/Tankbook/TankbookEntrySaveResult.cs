namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Ergebnis des Anlegens oder Änderns eines Tank- oder Ladevorgangs.
/// </summary>
/// <param name="Result">Das Ergebnis der Änderung.</param>
/// <param name="Entry">Der gespeicherte Vorgang; <see langword="null"/>, wenn <paramref name="Result"/> nicht <see cref="TankbookEntryResult.Ok"/> ist.</param>
/// <param name="OdometerBoundKm">Der verletzte Grenzwert des Kilometerstands bei einem Kilometerfehler, sonst <see langword="null"/>.</param>
/// <returns>Der Wert.</returns>
public sealed record TankbookEntrySaveResult(TankbookEntryResult Result, TankbookEntry? Entry, int? OdometerBoundKm);

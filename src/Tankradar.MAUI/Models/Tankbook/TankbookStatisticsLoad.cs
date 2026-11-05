namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Ergebnis des Ladens einer Auswertung.
/// </summary>
/// <param name="Status">Der Status.</param>
/// <param name="Statistics">Die Auswertung; <see langword="null"/>, wenn das Fahrzeug nicht gefunden wurde.</param>
/// <returns>Der Wert.</returns>
public sealed record TankbookStatisticsLoad(TankbookStatisticsStatus Status, TankbookStatistics? Statistics);

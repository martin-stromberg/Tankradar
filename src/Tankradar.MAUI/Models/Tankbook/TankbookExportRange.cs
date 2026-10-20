namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Ein aufgelöster Zeitraum eines Exports (lokale Wanduhrzeit, beide Tage einschließlich).
/// </summary>
/// <param name="From">Der erste Tag ohne Uhrzeit; <see langword="null"/> ohne untere Grenze.</param>
/// <param name="To">Der letzte Tag ohne Uhrzeit; <see langword="null"/> ohne obere Grenze.</param>
/// <returns>Der Wert.</returns>
public sealed record TankbookExportRange(DateTime? From, DateTime? To);

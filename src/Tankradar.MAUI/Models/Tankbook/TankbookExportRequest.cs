namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Auftrag zur Erzeugung eines Exports.
/// </summary>
/// <param name="Format">Das Dateiformat.</param>
/// <param name="From">Der erste Tag des Zeitraums (einschließlich); <see langword="null"/> für keine untere Grenze.</param>
/// <param name="To">Der letzte Tag des Zeitraums (einschließlich); <see langword="null"/> für keine obere Grenze.</param>
/// <returns>Der Wert.</returns>
public sealed record TankbookExportRequest(TankbookExportFormat Format, DateTime? From, DateTime? To);

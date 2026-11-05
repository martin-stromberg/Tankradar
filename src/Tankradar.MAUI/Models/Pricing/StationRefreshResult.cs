namespace Tankradar.MAUI.Models.Pricing;

/// <summary>
/// Ergebnis der Preisaktualisierung für mehrere Tankstellen nach Kennungen.
/// </summary>
/// <param name="Stations">Die lokal bekannten Tankstellen in der Reihenfolge der Kennungen, mit den zuletzt bekannten Preisen.</param>
/// <param name="Source">Die Herkunft der Daten.</param>
/// <param name="Failure">Der Grund, warum nicht (vollständig) live abgerufen wurde; <see cref="PriceFailure.None"/> bei Live- und Cache-Daten.</param>
/// <returns>Der Wert.</returns>
public sealed record StationRefreshResult(IReadOnlyList<StationInfo> Stations, PriceDataSource Source, PriceFailure Failure);

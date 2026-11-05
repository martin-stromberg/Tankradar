namespace Tankradar.MAUI.Models.Pricing;

/// <summary>
/// Eine Umkreissuche. Die Position wird nur für die Anfrage verwendet und nie gespeichert.
/// </summary>
/// <param name="Latitude">Breitengrad der Suchposition.</param>
/// <param name="Longitude">Längengrad der Suchposition.</param>
/// <param name="RadiusKm">Suchradius in Kilometern.</param>
/// <param name="FuelTypes">Die gewünschten Spritsorten.</param>
/// <returns>Der Wert.</returns>
public sealed record StationSearchQuery(double Latitude, double Longitude, int RadiusKm, IReadOnlyList<FuelType> FuelTypes);

/// <summary>
/// Ergebnis einer Umkreissuche.
/// </summary>
/// <param name="Stations">Die gefundenen Tankstellen mit den Preisen der angefragten Sorten, nach Entfernung sortiert.</param>
/// <param name="Source">Die Herkunft der Daten.</param>
/// <param name="Failure">Der Grund, warum nicht live abgerufen wurde; <see cref="PriceFailure.None"/> bei Live- und Cache-Daten.</param>
/// <returns>Der Wert.</returns>
public sealed record StationSearchResult(IReadOnlyList<StationInfo> Stations, PriceDataSource Source, PriceFailure Failure);

/// <summary>
/// Ergebnis des Abrufs einer einzelnen Tankstelle.
/// </summary>
/// <param name="Station">Die Tankstelle; <see langword="null"/>, wenn sie weder abgerufen werden konnte noch lokal bekannt ist.</param>
/// <param name="Source">Die Herkunft der Daten.</param>
/// <param name="Failure">Der Grund, warum nicht live abgerufen wurde; <see cref="PriceFailure.None"/> bei Live- und Cache-Daten.</param>
/// <returns>Der Wert.</returns>
public sealed record StationDetailResult(StationInfo? Station, PriceDataSource Source, PriceFailure Failure);

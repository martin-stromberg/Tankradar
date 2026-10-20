using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Pricing;

namespace Tankradar.MAUI.Services.Routing;

/// <summary>
/// Das Ergebnis der Tankstellensuche entlang einer Route.
/// </summary>
/// <param name="Stations">Die Tankstellen im Abstandsband, nach Streckenposition sortiert.</param>
/// <param name="Failure">Der Grund eines Abbruchs; <see cref="PriceFailure.None"/> bei Erfolg.</param>
/// <returns>Der Wert.</returns>
public sealed record RouteStationSearchResult(IReadOnlyList<RouteStation> Stations, PriceFailure Failure);

/// <summary>
/// Sucht die Tankstellen entlang einer Route aus mehreren Umkreisabfragen.
/// </summary>
public interface IRouteStationSearchService
{
    /// <summary>
    /// Fragt die Abfragepunkte nacheinander ab, führt die Treffer zusammen und wählt die Tankstellen im Abstandsband aus.
    /// Bei einem Fehler einer Abfrage wird abgebrochen; es gibt keine Teilergebnisse.
    /// </summary>
    /// <param name="route">Die Route.</param>
    /// <param name="queryPoints">Die Abfragepunkte (siehe <see cref="RouteSampler"/>).</param>
    /// <param name="distanceKm">Der größte Abstand zur Route in Kilometern.</param>
    /// <param name="fuelTypes">Die gewünschten Spritsorten.</param>
    /// <param name="progress">Meldet den Fortschritt; kann <see langword="null"/> sein.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis.</returns>
    Task<RouteStationSearchResult> SearchAsync(
        RouteProfile route,
        IReadOnlyList<RouteQueryPoint> queryPoints,
        int distanceKm,
        IReadOnlyList<FuelType> fuelTypes,
        IProgress<RouteSearchProgress>? progress,
        CancellationToken cancellationToken);
}

/// <summary>
/// Fragt die Umkreisabfragen strikt nacheinander ab (die Drosselung übernimmt der Preisdienst), dedupliziert nach Kennung,
/// filtert nach dem Abstand zur Route und sortiert nach der Position entlang der Route.
/// </summary>
public sealed class RouteStationSearchService : IRouteStationSearchService
{
    private readonly IFuelPriceService _priceService;

    /// <summary>
    /// Erstellt den Dienst.
    /// </summary>
    /// <param name="priceService">Der Preisdienst für die einzelnen Umkreisabfragen.</param>
    public RouteStationSearchService(IFuelPriceService priceService)
    {
        _priceService = priceService;
    }

    /// <inheritdoc />
    public async Task<RouteStationSearchResult> SearchAsync(
        RouteProfile route,
        IReadOnlyList<RouteQueryPoint> queryPoints,
        int distanceKm,
        IReadOnlyList<FuelType> fuelTypes,
        IProgress<RouteSearchProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(queryPoints);
        ArgumentNullException.ThrowIfNull(fuelTypes);

        var found = new Dictionary<string, StationInfo>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < queryPoints.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new RouteSearchProgress(RouteSearchPhase.SearchingStations, index + 1, queryPoints.Count));
            var point = queryPoints[index];
            var result = await _priceService
                .SearchNearbyAsync(new StationSearchQuery(point.Latitude, point.Longitude, point.RadiusKm, fuelTypes), cancellationToken)
                .ConfigureAwait(false);
            if (result.Failure != PriceFailure.None)
            {
                return new RouteStationSearchResult([], result.Failure);
            }

            foreach (var station in result.Stations)
            {
                found.TryAdd(station.Id, station);
            }
        }

        var stations = new List<RouteStation>();
        foreach (var station in found.Values)
        {
            if (!GeoPosition.TryCreate(station.Latitude, station.Longitude, out _) || (station.Latitude == 0 && station.Longitude == 0))
            {
                continue;
            }

            var location = route.Locate(station.Latitude, station.Longitude, distanceKm);
            if (location.OffsetKm <= distanceKm)
            {
                stations.Add(new RouteStation(station, location.AlongKm, location.OffsetKm));
            }
        }

        stations.Sort((left, right) =>
        {
            var byAlong = left.AlongKm.CompareTo(right.AlongKm);
            return byAlong != 0 ? byAlong : string.CompareOrdinal(left.Station.Id, right.Station.Id);
        });
        return new RouteStationSearchResult(stations, PriceFailure.None);
    }
}

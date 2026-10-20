using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Geocoding;

namespace Tankradar.MAUI.Services.Routing;

/// <summary>
/// Führt die gesamte Routensuche aus: Orte auflösen, Route berechnen und Tankstellen entlang der Route suchen.
/// </summary>
public interface IRouteSearchService
{
    /// <summary>
    /// Führt die Routensuche aus. Ein unzulässiger Abstand löst eine <see cref="ArgumentOutOfRangeException"/> aus, bevor ein externer Dienst aufgerufen wird; sonstige Fehler werden als Status gemeldet.
    /// </summary>
    /// <param name="request">Der Auftrag.</param>
    /// <param name="progress">Meldet den Fortschritt; kann <see langword="null"/> sein.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis.</returns>
    Task<RouteSearchResult> SearchAsync(RouteSearchRequest request, IProgress<RouteSearchProgress>? progress, CancellationToken cancellationToken);
}

/// <summary>
/// Orchestriert Geokodierung, Routing und Tankstellensuche. Nach einem Fehler erfolgen keine weiteren externen Aufrufe.
/// Weder Orte noch Positionen noch die Route werden gespeichert oder protokolliert.
/// </summary>
public sealed class RouteSearchService : IRouteSearchService
{
    private readonly IGeocodingService _geocoding;
    private readonly IRoutingService _routing;
    private readonly IRouteStationSearchService _stationSearch;
    private readonly RouteSearchOptions _options;

    /// <summary>
    /// Erstellt den Dienst.
    /// </summary>
    /// <param name="geocoding">Die Ortsauflösung (Nominatim).</param>
    /// <param name="routing">Der Routingdienst.</param>
    /// <param name="stationSearch">Die Tankstellensuche entlang der Route.</param>
    /// <param name="options">Die Einstellungen (werden geprüft).</param>
    public RouteSearchService(IGeocodingService geocoding, IRoutingService routing, IRouteStationSearchService stationSearch, RouteSearchOptions options)
    {
        options.Validate();
        _geocoding = geocoding;
        _routing = routing;
        _stationSearch = stationSearch;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<RouteSearchResult> SearchAsync(RouteSearchRequest request, IProgress<RouteSearchProgress>? progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!RouteDistance.IsValid(request.DistanceKm))
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Der Abstand zur Route ist keine zulässige Stufe.");
        }

        if (!await _routing.IsAvailableAsync(cancellationToken).ConfigureAwait(false))
        {
            return RouteSearchResult.Fail(RouteSearchStatus.RoutingUnavailable);
        }

        progress?.Report(new RouteSearchProgress(RouteSearchPhase.ResolvingPlaces, 0, 0));
        var start = request.StartPosition;
        string? startName = null;
        if (start is null)
        {
            var resolvedStart = await _geocoding.ResolveAsync(request.StartText, cancellationToken).ConfigureAwait(false);
            if (resolvedStart.Status != GeocodingStatus.Found || resolvedStart.Position is null)
            {
                return resolvedStart.Status == GeocodingStatus.NotFound
                    ? RouteSearchResult.Fail(RouteSearchStatus.StartNotFound)
                    : RouteSearchResult.FailGeocoding(resolvedStart.Status);
            }

            start = resolvedStart.Position;
            startName = resolvedStart.PlaceName;
        }

        var resolvedDestination = await _geocoding.ResolveAsync(request.DestinationText, cancellationToken).ConfigureAwait(false);
        if (resolvedDestination.Status != GeocodingStatus.Found || resolvedDestination.Position is null)
        {
            return resolvedDestination.Status == GeocodingStatus.NotFound
                ? RouteSearchResult.Fail(RouteSearchStatus.DestinationNotFound)
                : RouteSearchResult.FailGeocoding(resolvedDestination.Status);
        }

        var destination = resolvedDestination.Position;
        if (RouteGeometry.HaversineKm(start.Latitude, start.Longitude, destination.Latitude, destination.Longitude) < _options.SamePlaceKm)
        {
            return RouteSearchResult.Fail(RouteSearchStatus.SamePlace);
        }

        progress?.Report(new RouteSearchProgress(RouteSearchPhase.CalculatingRoute, 0, 0));
        var routing = await _routing.GetRouteAsync(start, destination, cancellationToken).ConfigureAwait(false);
        if (routing.Route is not { } route)
        {
            return routing.Failure == RoutingFailure.RouteTooLong
                ? RouteSearchResult.Fail(RouteSearchStatus.RouteTooLong)
                : RouteSearchResult.FailRouting(routing.Failure);
        }

        if (!RouteSampler.TryCreateQueryPoints(route, request.DistanceKm, _options, out var queryPoints))
        {
            return RouteSearchResult.Fail(RouteSearchStatus.RouteTooLong);
        }

        var stations = await _stationSearch
            .SearchAsync(route, queryPoints, request.DistanceKm, request.FuelTypes, progress, cancellationToken)
            .ConfigureAwait(false);
        if (stations.Failure != PriceFailure.None)
        {
            return RouteSearchResult.FailPrice(stations.Failure);
        }

        return RouteSearchResult.Success(route, start, destination, startName, resolvedDestination.PlaceName, stations.Stations);
    }
}

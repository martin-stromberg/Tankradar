using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;

namespace Tankradar.MAUI.Services.Pricing;

/// <summary>
/// Liefert Kraftstoffpreise und Tankstellendaten: aus dem lokalen Cache, frisch von der API oder – wenn kein Abruf möglich ist – als zuletzt bekannte Werte.
/// </summary>
public interface IFuelPriceService
{
    /// <summary>
    /// Sucht Tankstellen im Umkreis. Fehler beim Abruf führen zum Rückfall auf die zuletzt bekannten Preise, nicht zu einer Ausnahme.
    /// </summary>
    /// <param name="query">Die Suchanfrage; sie wird vor dem Abruf validiert.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis mit Herkunftsangabe.</returns>
    /// <exception cref="ArgumentException">Die Anfrage ist ungültig.</exception>
    Task<StationSearchResult> SearchNearbyAsync(StationSearchQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ruft die Details einer Tankstelle ab. Fehler beim Abruf führen zum Rückfall auf die zuletzt bekannten Daten, nicht zu einer Ausnahme.
    /// </summary>
    /// <param name="stationId">Die Kennung der Tankstelle (UUID).</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis mit Herkunftsangabe.</returns>
    /// <exception cref="ArgumentException">Die Kennung ist ungültig.</exception>
    Task<StationDetailResult> GetStationDetailAsync(string stationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prüft auf Veranlassung des Anwenders, ob der Preisdienst erreichbar ist und den Schlüssel akzeptiert.
    /// </summary>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns><see cref="PriceFailure.None"/>, wenn der Dienst erreichbar ist, sonst der Grund.</returns>
    Task<PriceFailure> CheckAvailabilityAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Standardumsetzung von <see cref="IFuelPriceService"/>.
/// </summary>
public sealed class FuelPriceService : IFuelPriceService
{
    private const double ProbeLatitude = 52.5200;
    private const double ProbeLongitude = 13.4050;

    private readonly ITankerkoenigClient _client;
    private readonly IPriceRepository _repository;
    private readonly IConnectionMonitor _connection;
    private readonly PriceApiOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<FuelPriceService> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _searchCache = new();

    /// <summary>
    /// Erstellt den Dienst.
    /// </summary>
    /// <param name="client">Der API-Client.</param>
    /// <param name="repository">Der lokale Preis-Cache.</param>
    /// <param name="connection">Die Verbindungserkennung.</param>
    /// <param name="options">Die API-Einstellungen.</param>
    /// <param name="timeProvider">Die Zeitquelle.</param>
    /// <param name="logger">Logger.</param>
    public FuelPriceService(
        ITankerkoenigClient client,
        IPriceRepository repository,
        IConnectionMonitor connection,
        PriceApiOptions options,
        TimeProvider timeProvider,
        ILogger<FuelPriceService> logger)
    {
        _client = client;
        _repository = repository;
        _connection = connection;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<StationSearchResult> SearchNearbyAsync(StationSearchQuery query, CancellationToken cancellationToken = default)
    {
        Validate(query);

        var key = CacheKey(query);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (_searchCache.TryGetValue(key, out var cachedAt) && now - cachedAt < _options.CacheLifetime)
        {
            var cached = await _repository.FindNearbyAsync(query.Latitude, query.Longitude, query.RadiusKm, cancellationToken).ConfigureAwait(false);
            return new StationSearchResult(FilterByFuelTypes(cached, query.FuelTypes), PriceDataSource.Cache, PriceFailure.None);
        }

        if (!_connection.IsOnline)
        {
            return await SearchFallbackAsync(query, PriceFailure.Offline, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var stations = await _client.SearchAsync(query.Latitude, query.Longitude, query.RadiusKm, cancellationToken).ConfigureAwait(false);
            if (await TrySaveAsync(stations.ToList(), cancellationToken).ConfigureAwait(false))
            {
                _searchCache[key] = _timeProvider.GetUtcNow().UtcDateTime;
            }

            var sorted = stations.OrderBy(s => s.DistanceKm ?? double.MaxValue).ToList();
            return new StationSearchResult(FilterByFuelTypes(sorted, query.FuelTypes), PriceDataSource.Live, PriceFailure.None);
        }
        catch (PriceApiException ex)
        {
            _logger.LogWarning("Umkreissuche nicht möglich ({Failure}); es werden die zuletzt bekannten Preise geliefert.", ex.Failure);
            return await SearchFallbackAsync(query, ex.Failure, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<StationDetailResult> GetStationDetailAsync(string stationId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(stationId, out var parsed))
        {
            throw new ArgumentException("Die Tankstellenkennung muss eine UUID sein.", nameof(stationId));
        }

        var id = parsed.ToString("D");
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var known = await _repository.GetStationAsync(id, cancellationToken).ConfigureAwait(false);
        if (known is { DetailsUpdatedUtc: { } updated } && now - updated < _options.CacheLifetime)
        {
            return new StationDetailResult(known, PriceDataSource.Cache, PriceFailure.None);
        }

        if (!_connection.IsOnline)
        {
            return new StationDetailResult(known, PriceDataSource.OfflineFallback, PriceFailure.Offline);
        }

        try
        {
            var station = await _client.GetDetailAsync(id, cancellationToken).ConfigureAwait(false);
            await TrySaveAsync([station], cancellationToken).ConfigureAwait(false);
            return new StationDetailResult(station, PriceDataSource.Live, PriceFailure.None);
        }
        catch (PriceApiException ex)
        {
            _logger.LogWarning("Tankstellendetails nicht abrufbar ({Failure}); es werden die zuletzt bekannten Daten geliefert.", ex.Failure);
            return new StationDetailResult(known, PriceDataSource.OfflineFallback, ex.Failure);
        }
    }

    /// <inheritdoc />
    public async Task<PriceFailure> CheckAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        if (!_connection.IsOnline)
        {
            return PriceFailure.Offline;
        }

        try
        {
            await _client.SearchAsync(ProbeLatitude, ProbeLongitude, 1, cancellationToken).ConfigureAwait(false);
            return PriceFailure.None;
        }
        catch (PriceApiException ex)
        {
            return ex.Failure;
        }
    }

    private async Task<bool> TrySaveAsync(IReadOnlyCollection<StationInfo> stations, CancellationToken cancellationToken)
    {
        try
        {
            await _repository.SaveAsync(stations, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Die frisch abgerufenen Daten sind gültig; ein Speicherfehler darf sie dem Aufrufer nicht vorenthalten.
            _logger.LogError(ex, "Die abgerufenen Preise konnten nicht lokal gespeichert werden.");
            return false;
        }
    }

    private static IReadOnlyList<StationInfo> FilterByFuelTypes(IEnumerable<StationInfo> stations, IReadOnlyList<FuelType> fuelTypes)
    {
        var wanted = fuelTypes.ToHashSet();
        var result = new List<StationInfo>();
        foreach (var station in stations)
        {
            var prices = station.Prices.Where(p => wanted.Contains(p.FuelType)).ToList();
            if (prices.Count > 0)
            {
                result.Add(station.With(prices));
            }
        }

        return result;
    }

    private static string CacheKey(StationSearchQuery query)
    {
        return FormattableString.Invariant($"{Math.Round(query.Latitude, 3)}|{Math.Round(query.Longitude, 3)}|{query.RadiusKm}");
    }

    private async Task<StationSearchResult> SearchFallbackAsync(StationSearchQuery query, PriceFailure failure, CancellationToken cancellationToken)
    {
        var known = await _repository.FindNearbyAsync(query.Latitude, query.Longitude, query.RadiusKm, cancellationToken).ConfigureAwait(false);
        return new StationSearchResult(FilterByFuelTypes(known, query.FuelTypes), PriceDataSource.OfflineFallback, failure);
    }

    private void Validate(StationSearchQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!double.IsFinite(query.Latitude) || query.Latitude is < -90 or > 90)
        {
            throw new ArgumentException("Der Breitengrad muss zwischen -90 und 90 liegen.", nameof(query));
        }

        if (!double.IsFinite(query.Longitude) || query.Longitude is < -180 or > 180)
        {
            throw new ArgumentException("Der Längengrad muss zwischen -180 und 180 liegen.", nameof(query));
        }

        if (query.RadiusKm < 1 || query.RadiusKm > _options.MaxRadiusKm)
        {
            throw new ArgumentException($"Der Radius muss zwischen 1 und {_options.MaxRadiusKm} km liegen.", nameof(query));
        }

        if (query.FuelTypes is null || query.FuelTypes.Count == 0 || query.FuelTypes.Any(f => !Enum.IsDefined(f)))
        {
            throw new ArgumentException("Es muss mindestens eine gültige Spritsorte angegeben werden.", nameof(query));
        }
    }
}

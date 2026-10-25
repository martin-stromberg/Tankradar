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
    /// Gibt an, ob eine Umkreissuche aus dem Such-Cache bedient würde (innerhalb der Cache-Lebensdauer erfolgreich abgerufen), also keinen Abruf auslöst.
    /// </summary>
    /// <param name="query">Die Suchanfrage.</param>
    /// <returns><see langword="true"/>, wenn <see cref="SearchNearbyAsync"/> keinen Abruf beim Preisdienst auslösen würde.</returns>
    bool IsSearchCached(StationSearchQuery query);

    /// <summary>
    /// Ruft die Details einer Tankstelle ab. Fehler beim Abruf führen zum Rückfall auf die zuletzt bekannten Daten, nicht zu einer Ausnahme.
    /// </summary>
    /// <param name="stationId">Die Kennung der Tankstelle (UUID).</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis mit Herkunftsangabe.</returns>
    /// <exception cref="ArgumentException">Die Kennung ist ungültig.</exception>
    Task<StationDetailResult> GetStationDetailAsync(string stationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aktualisiert die Preise mehrerer Tankstellen nach Kennungen (höchstens 10 je Anfrage). Fehler führen zum Rückfall auf die zuletzt bekannten Preise, nicht zu einer Ausnahme.
    /// Tankstellen, die innerhalb der Cache-Lebensdauer erfolgreich aktualisiert wurden, werden ohne <paramref name="force"/> nicht erneut abgerufen.
    /// </summary>
    /// <param name="stationIds">Die Kennungen (UUID); ungültige und doppelte Kennungen werden ausgelassen.</param>
    /// <param name="force">Ruft unabhängig von der Frische ab.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Die lokal bekannten Tankstellen mit den zuletzt bekannten Preisen und Herkunftsangabe.</returns>
    Task<StationRefreshResult> RefreshStationsAsync(IReadOnlyCollection<string> stationIds, bool force, CancellationToken cancellationToken = default);

    /// <summary>
    /// Liefert die lokal bekannten Tankstellen zu den Kennungen mit den zuletzt bekannten Preisen, ohne Abruf.
    /// </summary>
    /// <param name="stationIds">Die Kennungen (UUID); ungültige und doppelte Kennungen werden ausgelassen.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Die bekannten Tankstellen in der Reihenfolge der Kennungen.</returns>
    Task<IReadOnlyList<StationInfo>> GetKnownStationsAsync(IReadOnlyCollection<string> stationIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prüft auf Veranlassung des Anwenders, ob der Preisdienst erreichbar ist und den Schlüssel akzeptiert.
    /// </summary>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns><see cref="PriceFailure.None"/>, wenn der Dienst erreichbar ist, sonst der Grund.</returns>
    Task<PriceFailure> CheckAvailabilityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Vergibt, ändert oder entfernt den eigenen Anzeigenamen einer lokal bekannten Tankstelle. Ein leerer Name entfernt ihn.
    /// </summary>
    /// <param name="stationId">Die Kennung der Tankstelle (UUID).</param>
    /// <param name="name">Der neue Name oder <see langword="null"/> bzw. leer zum Entfernen.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis; ein nicht lesbarer Speicher wird als Ausnahme durchgereicht.</returns>
    Task<StationNameResult> SetCustomNameAsync(string stationId, string? name, CancellationToken cancellationToken = default);
}

/// <summary>
/// Standardumsetzung von <see cref="IFuelPriceService"/>.
/// </summary>
public sealed class FuelPriceService : IFuelPriceService, IPriceCacheInvalidator
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
    private readonly ConcurrentDictionary<string, DateTime> _refreshed = new();

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
        PruneSearchCache(now);
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

            var withDetails = await WithKnownDetailsAsync(stations, cancellationToken).ConfigureAwait(false);
            var detailed = await WithCustomNamesAsync(withDetails, cancellationToken).ConfigureAwait(false);
            var sorted = detailed.OrderBy(s => s.DistanceKm ?? double.MaxValue).ToList();
            return new StationSearchResult(FilterByFuelTypes(sorted, query.FuelTypes), PriceDataSource.Live, PriceFailure.None);
        }
        catch (PriceApiException ex)
        {
            _logger.LogWarning("Umkreissuche nicht möglich ({Failure}); es werden die zuletzt bekannten Preise geliefert.", ex.Failure);
            return await SearchFallbackAsync(query, ex.Failure, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public bool IsSearchCached(StationSearchQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        return _searchCache.TryGetValue(CacheKey(query), out var cachedAt) && now - cachedAt < _options.CacheLifetime;
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
            var named = await WithCustomNamesAsync([station], cancellationToken).ConfigureAwait(false);
            return new StationDetailResult(named[0], PriceDataSource.Live, PriceFailure.None);
        }
        catch (PriceApiException ex)
        {
            _logger.LogWarning("Tankstellendetails nicht abrufbar ({Failure}); es werden die zuletzt bekannten Daten geliefert.", ex.Failure);
            return new StationDetailResult(known, PriceDataSource.OfflineFallback, ex.Failure);
        }
    }

    /// <inheritdoc />
    public async Task<StationRefreshResult> RefreshStationsAsync(IReadOnlyCollection<string> stationIds, bool force, CancellationToken cancellationToken = default)
    {
        var ids = NormalizeIds(stationIds);
        if (ids.Count == 0)
        {
            return new StationRefreshResult([], PriceDataSource.Cache, PriceFailure.None);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        PruneSearchCache(now);
        var due = force
            ? ids
            : ids.Where(id => !_refreshed.TryGetValue(id, out var at) || now - at >= _options.CacheLifetime).ToList();
        if (due.Count == 0)
        {
            return new StationRefreshResult(await _repository.GetStationsAsync(ids, cancellationToken).ConfigureAwait(false), PriceDataSource.Cache, PriceFailure.None);
        }

        if (!_connection.IsOnline)
        {
            return new StationRefreshResult(await _repository.GetStationsAsync(ids, cancellationToken).ConfigureAwait(false), PriceDataSource.OfflineFallback, PriceFailure.Offline);
        }

        var fetched = new Dictionary<string, IReadOnlyList<FuelPrice>>(StringComparer.OrdinalIgnoreCase);
        var failure = PriceFailure.None;
        foreach (var chunk in due.Chunk(TankerkoenigClient.MaxPriceIds))
        {
            try
            {
                var prices = await _client.GetPricesAsync(chunk, cancellationToken).ConfigureAwait(false);
                if (await TrySavePricesAsync(prices, cancellationToken).ConfigureAwait(false))
                {
                    var stamp = _timeProvider.GetUtcNow().UtcDateTime;
                    foreach (var id in chunk.Where(id => prices.Keys.Any(key => string.Equals(key, id, StringComparison.OrdinalIgnoreCase))))
                    {
                        _refreshed[id] = stamp;
                    }
                }

                foreach (var entry in prices)
                {
                    fetched[entry.Key] = entry.Value;
                }
            }
            catch (PriceApiException ex)
            {
                _logger.LogWarning("Preisabruf nach Kennungen nicht möglich ({Failure}); es werden die zuletzt bekannten Preise geliefert.", ex.Failure);
                failure = ex.Failure;
                break;
            }
        }

        var local = await _repository.GetStationsAsync(ids, cancellationToken).ConfigureAwait(false);
        var merged = local
            .Select(station => fetched.TryGetValue(station.Id, out var lines) && lines.Count > 0 ? WithFetchedPrices(station, lines) : station)
            .ToList();
        var source = fetched.Count == 0 && failure != PriceFailure.None ? PriceDataSource.OfflineFallback : PriceDataSource.Live;
        return new StationRefreshResult(merged, source, failure);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StationInfo>> GetKnownStationsAsync(IReadOnlyCollection<string> stationIds, CancellationToken cancellationToken = default)
    {
        var ids = NormalizeIds(stationIds);
        PruneSearchCache(_timeProvider.GetUtcNow().UtcDateTime);
        return ids.Count == 0 ? [] : await _repository.GetStationsAsync(ids, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StationNameResult> SetCustomNameAsync(string stationId, string? name, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stationId);
        var id = Guid.TryParse(stationId, out var parsed) ? parsed.ToString("D") : stationId;
        return await _repository.SetCustomNameAsync(id, name, cancellationToken).ConfigureAwait(false);
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

    /// <summary>
    /// Ergänzt die Tankstellen der Umkreissuche um Öffnungszeiten aus früheren Detailabfragen (die Umkreissuche der Quelle liefert sie nicht),
    /// sofern diese nicht älter als <see cref="DetailFreshness.MaxAge"/> sind.
    /// Ein Fehler beim Lesen des Caches lässt die Suche unberührt.
    /// </summary>
    /// <param name="stations">Die Tankstellen der Umkreissuche.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Die Tankstellen, soweit bekannt mit Öffnungszeiten.</returns>
    private async Task<IReadOnlyList<StationInfo>> WithKnownDetailsAsync(IReadOnlyList<StationInfo> stations, CancellationToken cancellationToken)
    {
        try
        {
            var known = await _repository.GetKnownDetailsAsync(stations.Select(s => s.Id).ToList(), cancellationToken).ConfigureAwait(false);
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            // Nur Detailangaben bis zur Altersgrenze werden übernommen; ältere bleiben ausgeblendet, bis die Detailansicht sie neu abgefragt hat.
            return stations
                .Select(s => known.TryGetValue(s.Id, out var details) && DetailFreshness.IsUsable(details.DetailsUpdatedUtc, now) ? s.WithDetails(details) : s)
                .ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Bekannte Tankstellendetails konnten nicht gelesen werden ({ExceptionType}).", ex.GetType().Name);
            return stations;
        }
    }

    /// <summary>
    /// Ergänzt frisch abgerufene Tankstellen um die lokal gespeicherten eigenen Anzeigenamen (die Quelle kennt sie nicht).
    /// Ein Lesefehler lässt die Anzeige ohne eigenen Namen; die abgerufenen Daten bleiben nutzbar.
    /// </summary>
    /// <param name="stations">Die abgerufenen Tankstellen.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Die Tankstellen, soweit vergeben mit eigenem Anzeigenamen.</returns>
    private async Task<IReadOnlyList<StationInfo>> WithCustomNamesAsync(IReadOnlyList<StationInfo> stations, CancellationToken cancellationToken)
    {
        try
        {
            var names = await _repository.GetCustomNamesAsync(stations.Select(s => s.Id).ToList(), cancellationToken).ConfigureAwait(false);
            return names.Count == 0
                ? stations
                : stations.Select(s => names.TryGetValue(s.Id, out var custom) ? s.WithCustomName(custom) : s).ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Eigene Anzeigenamen konnten nicht gelesen werden ({ExceptionType}).", ex.GetType().Name);
            return stations;
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
            _logger.LogError("Die abgerufenen Preise konnten nicht lokal gespeichert werden ({ExceptionType}).", ex.GetType().Name);
            return false;
        }
    }

    /// <inheritdoc />
    public void InvalidateCaches()
    {
        _searchCache.Clear();
        _refreshed.Clear();
    }

    /// <summary>
    /// Entfernt abgelaufene Einträge des Suchcaches, damit die gerundeten Koordinaten der Schlüssel nicht bis zum Ende der App im Arbeitsspeicher bleiben.
    /// </summary>
    /// <param name="now">Der aktuelle Zeitpunkt (UTC).</param>
    private void PruneSearchCache(DateTime now)
    {
        foreach (var entry in _searchCache)
        {
            if (now - entry.Value >= _options.CacheLifetime)
            {
                _searchCache.TryRemove(entry);
            }
        }
    }

    private static List<string> NormalizeIds(IReadOnlyCollection<string> stationIds)
    {
        ArgumentNullException.ThrowIfNull(stationIds);
        return stationIds
            .Select(id => Guid.TryParse(id, out var parsed) ? parsed.ToString("D") : null)
            .OfType<string>()
            .Distinct()
            .ToList();
    }

    private static StationInfo WithFetchedPrices(StationInfo station, IReadOnlyList<FuelPrice> fetched)
    {
        var replaced = fetched.Select(p => p.FuelType).ToHashSet();
        var merged = station.Prices.Where(p => !replaced.Contains(p.FuelType)).Concat(fetched).OrderBy(p => p.FuelType).ToList();
        return station.With(merged);
    }

    private async Task<bool> TrySavePricesAsync(IReadOnlyDictionary<string, IReadOnlyList<FuelPrice>> prices, CancellationToken cancellationToken)
    {
        try
        {
            await _repository.SavePricesAsync(prices, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError("Die abgerufenen Preise konnten nicht lokal gespeichert werden ({ExceptionType}).", ex.GetType().Name);
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

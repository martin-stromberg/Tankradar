using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;

namespace Tankradar.MAUI.Services.Pricing;

/// <summary>
/// Lokaler Preis-Cache: speichert Tankstellen und Preise mit Zeitstempel und liefert die zuletzt bekannten Werte.
/// </summary>
public interface IPriceRepository
{
    /// <summary>
    /// Speichert Tankstellen und je Preis einen neuen Stand mit dem Abrufzeitpunkt der Preise; ältere Stände bleiben erhalten.
    /// </summary>
    /// <param name="stations">Die abgerufenen Tankstellen.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Ein Task, der nach dem Speichern abgeschlossen ist.</returns>
    Task SaveAsync(IReadOnlyCollection<StationInfo> stations, CancellationToken cancellationToken = default);

    /// <summary>
    /// Liefert eine Tankstelle mit den zuletzt bekannten Preisen.
    /// </summary>
    /// <param name="stationId">Die Kennung.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Die Tankstelle oder <see langword="null"/>, wenn sie lokal unbekannt ist.</returns>
    Task<StationInfo?> GetStationAsync(string stationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Liefert die Tankstellen aus der Liste, deren Detailangaben (Öffnungszeiten) lokal aus einer früheren Detailabfrage bekannt sind.
    /// Die Preise der gelieferten Tankstellen sind nicht gefüllt.
    /// </summary>
    /// <param name="stationIds">Die Kennungen.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Die Tankstellen mit bekannten Detailangaben, je Kennung.</returns>
    Task<IReadOnlyDictionary<string, StationInfo>> GetKnownDetailsAsync(IReadOnlyCollection<string> stationIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Liefert die lokal bekannten Tankstellen im Umkreis einer Position mit den zuletzt bekannten Preisen und berechneter Entfernung, nach Entfernung sortiert.
    /// Die Position wird nicht gespeichert.
    /// </summary>
    /// <param name="latitude">Breitengrad der Suchposition.</param>
    /// <param name="longitude">Längengrad der Suchposition.</param>
    /// <param name="radiusKm">Radius in Kilometern.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Die Tankstellen im Umkreis.</returns>
    Task<IReadOnlyList<StationInfo>> FindNearbyAsync(double latitude, double longitude, double radiusKm, CancellationToken cancellationToken = default);
}

/// <summary>
/// EF-Core-Umsetzung von <see cref="IPriceRepository"/> auf der lokalen SQLite-Datenbank.
/// </summary>
public sealed class PriceRepository : IPriceRepository
{
    private const double EarthRadiusKm = 6371.0088;

    private readonly IDbContextFactory<TankradarDbContext> _contextFactory;
    private readonly IDatabaseInitializer _initializer;

    /// <summary>
    /// Erstellt das Repository.
    /// </summary>
    /// <param name="contextFactory">Factory für Datenbankkontexte.</param>
    /// <param name="initializer">Stellt vor jedem Zugriff die Datenbankinitialisierung sicher.</param>
    public PriceRepository(IDbContextFactory<TankradarDbContext> contextFactory, IDatabaseInitializer initializer)
    {
        _contextFactory = contextFactory;
        _initializer = initializer;
    }

    /// <summary>
    /// Berechnet die Entfernung zweier Positionen in Kilometern (Haversine-Formel).
    /// </summary>
    /// <param name="latitude1">Breitengrad der ersten Position.</param>
    /// <param name="longitude1">Längengrad der ersten Position.</param>
    /// <param name="latitude2">Breitengrad der zweiten Position.</param>
    /// <param name="longitude2">Längengrad der zweiten Position.</param>
    /// <returns>Die Entfernung in Kilometern.</returns>
    public static double DistanceKm(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        var dLat = ToRadians(latitude2 - latitude1);
        var dLon = ToRadians(longitude2 - longitude1);
        var a = Math.Pow(Math.Sin(dLat / 2), 2)
            + (Math.Cos(ToRadians(latitude1)) * Math.Cos(ToRadians(latitude2)) * Math.Pow(Math.Sin(dLon / 2), 2));
        return 2 * EarthRadiusKm * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    /// <inheritdoc />
    public async Task SaveAsync(IReadOnlyCollection<StationInfo> stations, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stations);
        if (stations.Count == 0)
        {
            return;
        }

        await _initializer.InitializeAsync().ConfigureAwait(false);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var ids = stations.Select(s => s.Id).ToList();
        var existing = await context.Stations
            .Where(s => ids.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken)
            .ConfigureAwait(false);

        foreach (var station in stations.DistinctBy(s => s.Id))
        {
            if (!existing.TryGetValue(station.Id, out var entity))
            {
                entity = new StationEntity { Id = station.Id };
                context.Stations.Add(entity);
            }

            entity.Name = station.Name;
            entity.Brand = station.Brand;
            entity.Street = station.Street;
            entity.HouseNumber = station.HouseNumber;
            entity.PostCode = station.PostCode;
            entity.Place = station.Place;
            entity.Latitude = station.Latitude;
            entity.Longitude = station.Longitude;

            if (station.DetailsUpdatedUtc is { } detailsTime)
            {
                entity.DetailsUpdatedUtc = detailsTime;
                entity.WholeDay = station.WholeDay;
                entity.OpeningTimesJson = JsonSerializer.Serialize(station.OpeningTimes);
            }

            foreach (var price in station.Prices)
            {
                context.PriceEntries.Add(new PriceEntryEntity
                {
                    StationId = station.Id,
                    FuelTypeKey = price.FuelType.ToString(),
                    Price = price.Price,
                    RetrievedUtc = price.RetrievedUtc,
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StationInfo?> GetStationAsync(string stationId, CancellationToken cancellationToken = default)
    {
        await _initializer.InitializeAsync().ConfigureAwait(false);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entity = await context.Stations.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == stationId, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            return null;
        }

        var prices = await LoadLatestPricesAsync(context, [stationId], cancellationToken).ConfigureAwait(false);
        return Map(entity, prices.GetValueOrDefault(stationId) ?? [], null);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, StationInfo>> GetKnownDetailsAsync(IReadOnlyCollection<string> stationIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stationIds);
        if (stationIds.Count == 0)
        {
            return new Dictionary<string, StationInfo>();
        }

        await _initializer.InitializeAsync().ConfigureAwait(false);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var ids = stationIds.ToList();
        var entities = await context.Stations.AsNoTracking()
            .Where(s => ids.Contains(s.Id) && s.DetailsUpdatedUtc != null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return entities.ToDictionary(entity => entity.Id, entity => Map(entity, [], null));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StationInfo>> FindNearbyAsync(double latitude, double longitude, double radiusKm, CancellationToken cancellationToken = default)
    {
        await _initializer.InitializeAsync().ConfigureAwait(false);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var latDelta = radiusKm / 111.0;
        var cosLat = Math.Max(0.01, Math.Cos(ToRadians(latitude)));
        var lonDelta = radiusKm / (111.0 * cosLat);
        var minLat = latitude - latDelta;
        var maxLat = latitude + latDelta;
        var minLon = longitude - lonDelta;
        var maxLon = longitude + lonDelta;

        var candidates = await context.Stations.AsNoTracking()
            .Where(s => s.Latitude >= minLat && s.Latitude <= maxLat && s.Longitude >= minLon && s.Longitude <= maxLon)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var inRadius = candidates
            .Select(s => (Entity: s, Distance: DistanceKm(latitude, longitude, s.Latitude, s.Longitude)))
            .Where(x => x.Distance <= radiusKm)
            .OrderBy(x => x.Distance)
            .ToList();
        if (inRadius.Count == 0)
        {
            return [];
        }

        var prices = await LoadLatestPricesAsync(context, inRadius.Select(x => x.Entity.Id).ToList(), cancellationToken).ConfigureAwait(false);
        return inRadius
            .Select(x => Map(x.Entity, prices.GetValueOrDefault(x.Entity.Id) ?? [], x.Distance))
            .ToList();
    }

    private static async Task<Dictionary<string, List<FuelPrice>>> LoadLatestPricesAsync(
        TankradarDbContext context,
        List<string> stationIds,
        CancellationToken cancellationToken)
    {
        var rows = await context.PriceEntries.AsNoTracking()
            .Where(p => stationIds.Contains(p.StationId)
                && !context.PriceEntries.Any(n => n.StationId == p.StationId
                    && n.FuelTypeKey == p.FuelTypeKey
                    && (n.RetrievedUtc > p.RetrievedUtc || (n.RetrievedUtc == p.RetrievedUtc && n.Id > p.Id))))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = new Dictionary<string, List<FuelPrice>>();
        foreach (var row in rows.OrderBy(r => r.FuelTypeKey, StringComparer.Ordinal))
        {
            if (!Enum.TryParse<FuelType>(row.FuelTypeKey, out var fuelType))
            {
                continue;
            }

            if (!result.TryGetValue(row.StationId, out var list))
            {
                list = [];
                result[row.StationId] = list;
            }

            list.Add(new FuelPrice(fuelType, row.Price, DateTime.SpecifyKind(row.RetrievedUtc, DateTimeKind.Utc)));
        }

        return result;
    }

    private static StationInfo Map(StationEntity entity, IReadOnlyList<FuelPrice> prices, double? distanceKm)
    {
        return new StationInfo
        {
            Id = entity.Id,
            Name = entity.Name,
            Brand = entity.Brand,
            Street = entity.Street,
            HouseNumber = entity.HouseNumber,
            PostCode = entity.PostCode,
            Place = entity.Place,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            DistanceKm = distanceKm,
            WholeDay = entity.WholeDay,
            OpeningTimes = ParseOpeningTimes(entity.OpeningTimesJson),
            Prices = prices.OrderBy(p => p.FuelType).ToList(),
            DetailsUpdatedUtc = entity.DetailsUpdatedUtc is { } details ? DateTime.SpecifyKind(details, DateTimeKind.Utc) : null,
        };
    }

    private static IReadOnlyList<OpeningTimeEntry> ParseOpeningTimes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<OpeningTimeEntry>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}

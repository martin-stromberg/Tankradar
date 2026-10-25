using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Geocoding;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.MAUI.Services.Routing;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// <see cref="IFuelPriceService"/> für Tests der Routensuche: liefert je Umkreisabfrage ein Ergebnis aus einer Funktion, protokolliert die Abfragen
/// und stellt sicher, dass nie zwei Abfragen gleichzeitig laufen.
/// </summary>
public sealed class ScriptedFuelPriceService : IFuelPriceService
{
    private int _active;

    /// <summary>
    /// Liefert das Ergebnis einer Abfrage anhand von Abfrage und laufender Nummer (ab 0).
    /// </summary>
    /// <returns>Der Wert.</returns>
    public Func<StationSearchQuery, int, StationSearchResult> Respond { get; set; } = NoStations;

    /// <summary>
    /// Alle empfangenen Abfragen in Reihenfolge.
    /// </summary>
    public List<StationSearchQuery> Queries { get; } = [];

    /// <summary>
    /// Die höchste Zahl gleichzeitig laufender Abfragen.
    /// </summary>
    public int MaxConcurrent { get; private set; }

    private static StationSearchResult NoStations(StationSearchQuery query, int index)
    {
        return new StationSearchResult([], PriceDataSource.Live, PriceFailure.None);
    }

    /// <summary>
    /// Bestimmt, welche Abfragen der Such-Cache bedienen würde (<see cref="IFuelPriceService.IsSearchCached"/>); ohne Angabe keine.
    /// </summary>
    public Func<StationSearchQuery, bool> Cached { get; set; } = _ => false;

    /// <inheritdoc />
    public bool IsSearchCached(StationSearchQuery query)
    {
        return Cached(query);
    }

    /// <inheritdoc />
    public async Task<StationSearchResult> SearchNearbyAsync(StationSearchQuery query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var index = Queries.Count;
        Queries.Add(query);
        MaxConcurrent = Math.Max(MaxConcurrent, Interlocked.Increment(ref _active));
        try
        {
            await Task.Yield();
            return Respond(query, index);
        }
        finally
        {
            Interlocked.Decrement(ref _active);
        }
    }

    /// <inheritdoc />
    public Task<StationDetailResult> GetStationDetailAsync(string stationId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new StationDetailResult(null, PriceDataSource.Live, PriceFailure.None));
    }

    /// <inheritdoc />
    public Task<StationRefreshResult> RefreshStationsAsync(IReadOnlyCollection<string> stationIds, bool force, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new StationRefreshResult([], PriceDataSource.Live, PriceFailure.None));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<StationInfo>> GetKnownStationsAsync(IReadOnlyCollection<string> stationIds, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<StationInfo>>([]);
    }

    /// <inheritdoc />
    public Task<StationNameResult> SetCustomNameAsync(string stationId, string? name, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(StationNameResult.Ok);
    }

    /// <inheritdoc />
    public Task<PriceFailure> CheckAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(PriceFailure.None);
    }
}

/// <summary>
/// <see cref="IGeocodingService"/> für Tests der Routensuche: löst Eingaben anhand einer Zuordnung auf und protokolliert sie.
/// </summary>
public sealed class ScriptedGeocodingService : IGeocodingService
{
    /// <summary>
    /// Die Ergebnisse je Eingabe (Groß- und Kleinschreibung unbeachtet); unbekannte Eingaben liefern <see cref="GeocodingStatus.NotFound"/>.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public Dictionary<string, GeocodingResult> Results { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Die empfangenen Eingaben in Reihenfolge.
    /// </summary>
    public List<string?> Inputs { get; } = [];

    /// <inheritdoc />
    public Task<GeocodingResult> ResolveAsync(string? input, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Inputs.Add(input);
        return Task.FromResult(input is not null && Results.TryGetValue(input, out var result) ? result : GeocodingResult.Failure(GeocodingStatus.NotFound));
    }
}

/// <summary>
/// <see cref="IRoutingService"/> für Tests mit vorgegebenem Ergebnis; protokolliert die Aufrufe.
/// </summary>
public sealed class FakeRoutingService : IRoutingService
{
    /// <summary>
    /// Gibt an, ob der Dienst verfügbar ist.
    /// </summary>
    public bool Available { get; set; } = true;

    /// <summary>
    /// Das gelieferte Ergebnis.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public RoutingResult Result { get; set; } = RoutingResult.Fail(RoutingFailure.Unreachable);

    /// <summary>
    /// Die Zahl der Routenberechnungen.
    /// </summary>
    public int Calls { get; private set; }

    /// <summary>
    /// Der Start der letzten Berechnung.
    /// </summary>
    public GeoPosition? LastStart { get; private set; }

    /// <summary>
    /// Das Ziel der letzten Berechnung.
    /// </summary>
    public GeoPosition? LastDestination { get; private set; }

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Available);
    }

    /// <inheritdoc />
    public Task<RoutingResult> GetRouteAsync(GeoPosition start, GeoPosition destination, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastStart = start;
        LastDestination = destination;
        return Task.FromResult(Result);
    }
}

/// <summary>
/// Fortschrittsempfänger, der alle Meldungen sofort und in Reihenfolge sammelt.
/// </summary>
public sealed class ListProgress : IProgress<RouteSearchProgress>
{
    /// <summary>
    /// Die gemeldeten Fortschritte in Reihenfolge.
    /// </summary>
    public List<RouteSearchProgress> Reports { get; } = [];

    /// <inheritdoc />
    public void Report(RouteSearchProgress value)
    {
        Reports.Add(value);
    }
}

/// <summary>
/// Erzeugt Teststationen mit frei wählbarer Position für die Auswahl entlang einer Route.
/// </summary>
public static class RouteStationFactory
{
    /// <summary>
    /// Erzeugt eine Station mit allen Sorten an der angegebenen Position.
    /// </summary>
    /// <param name="number">Laufende Nummer (bestimmt die Kennung).</param>
    /// <param name="latitude">Der Breitengrad.</param>
    /// <param name="longitude">Der Längengrad.</param>
    /// <param name="e5">Preis Super E5.</param>
    /// <returns>Die Station.</returns>
    public static StationInfo At(int number, double latitude, double longitude, decimal e5 = 1.80m)
    {
        var retrieved = SearchTestData.Now;
        return new StationInfo
        {
            Id = $"{number:D8}-0000-4000-8000-000000000000",
            Name = $"Station {number}",
            Latitude = latitude,
            Longitude = longitude,
            DistanceKm = 99,
            Prices = [new FuelPrice(FuelType.SuperE5, e5, retrieved), new FuelPrice(FuelType.Diesel, e5 - 0.15m, retrieved)],
        };
    }

    /// <summary>
    /// Berechnet den Längengrad-Versatz, der bei der angegebenen Breite einem seitlichen Abstand in Kilometern entspricht.
    /// </summary>
    /// <param name="latitude">Der Breitengrad.</param>
    /// <param name="kilometers">Der Abstand in Kilometern.</param>
    /// <returns>Der Versatz in Grad.</returns>
    public static double EastOffset(double latitude, double kilometers)
    {
        return kilometers / (111.195 * Math.Cos(latitude * Math.PI / 180));
    }

    /// <summary>
    /// Erzeugt ein Ergebnis mit Stationen aus der Preisquelle.
    /// </summary>
    /// <param name="stations">Die Stationen.</param>
    /// <returns>Das Ergebnis.</returns>
    public static StationSearchResult Live(params StationInfo[] stations)
    {
        return new StationSearchResult(stations, PriceDataSource.Live, PriceFailure.None);
    }
}

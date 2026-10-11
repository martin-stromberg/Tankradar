using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Pricing;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// <see cref="IFuelPriceService"/> für Tests mit vorgegebener Antwort; protokolliert die Suchanfragen.
/// </summary>
public sealed class FakeFuelPriceService : IFuelPriceService
{
    private static readonly StationDetailResult UnknownStation = CreateUnknownStation();

    /// <summary>
    /// Das gelieferte Suchergebnis (Standard: keine Tankstellen, live).
    /// </summary>
    /// <returns>Der Wert.</returns>
    public StationSearchResult Result { get; set; } = new StationSearchResult([], PriceDataSource.Live, PriceFailure.None);

    /// <summary>
    /// Wenn gesetzt, wird sie von <see cref="SearchNearbyAsync"/> ausgelöst.
    /// </summary>
    public Exception? SearchException { get; set; }

    /// <summary>
    /// Das gelieferte Ergebnis der Detailabfrage (Standard: unbekannte Tankstelle, live).
    /// </summary>
    public StationDetailResult DetailResult { get; set; } = UnknownStation;

    /// <summary>
    /// Wenn gesetzt, wird sie von <see cref="GetStationDetailAsync"/> ausgelöst.
    /// </summary>
    public Exception? DetailException { get; set; }

    /// <summary>
    /// Wenn gesetzt, wartet <see cref="GetStationDetailAsync"/> auf dieses Signal, bevor es das Ergebnis liefert.
    /// </summary>
    public Task? DetailGate { get; set; }

    /// <summary>
    /// Wenn gesetzt, ignoriert <see cref="GetStationDetailAsync"/> das Abbruchsignal beim Warten auf <see cref="DetailGate"/> (ein Dienst, der trotz Abbruch normal zurückkehrt).
    /// </summary>
    public bool IgnoreCancellation { get; set; }

    /// <summary>
    /// Alle abgefragten Tankstellenkennungen der Detailabfrage in Reihenfolge.
    /// </summary>
    public List<string> DetailRequests { get; } = [];

    /// <summary>
    /// Alle empfangenen Suchanfragen in Reihenfolge.
    /// </summary>
    public List<StationSearchQuery> Queries { get; } = [];

    /// <summary>
    /// Das gelieferte Ergebnis der Aktualisierung nach Kennungen (Standard: keine Tankstellen, live).
    /// </summary>
    /// <returns>Der Wert.</returns>
    public StationRefreshResult RefreshResult { get; set; } = new StationRefreshResult([], PriceDataSource.Live, PriceFailure.None);

    /// <summary>
    /// Die lokal bekannten Tankstellen, die <see cref="GetKnownStationsAsync"/> liefert.
    /// </summary>
    public List<StationInfo> KnownStations { get; } = [];

    /// <summary>
    /// Wenn gesetzt, wird sie von <see cref="RefreshStationsAsync"/> ausgelöst.
    /// </summary>
    public Exception? RefreshException { get; set; }

    /// <summary>
    /// Wenn gesetzt, wartet <see cref="RefreshStationsAsync"/> auf dieses Signal (abbrechbar), bevor es das Ergebnis liefert.
    /// </summary>
    public Task? RefreshGate { get; set; }

    private readonly TaskCompletionSource _refreshEntered = CreateSource();

    /// <summary>
    /// Wird abgeschlossen, sobald <see cref="RefreshStationsAsync"/> erstmals aufgerufen wurde.
    /// </summary>
    public TaskCompletionSource RefreshEntered => _refreshEntered;

    /// <summary>
    /// Die Kennungen aller Aktualisierungen nach Kennungen in Reihenfolge.
    /// </summary>
    public List<IReadOnlyList<string>> RefreshRequests { get; } = [];

    /// <summary>
    /// Der <c>force</c>-Wert aller Aktualisierungen nach Kennungen in Reihenfolge.
    /// </summary>
    public List<bool> RefreshForces { get; } = [];

    /// <summary>
    /// Alle Abfragen der lokal bekannten Tankstellen in Reihenfolge.
    /// </summary>
    public List<IReadOnlyList<string>> KnownRequests { get; } = [];

    /// <inheritdoc />
    public async Task<StationRefreshResult> RefreshStationsAsync(IReadOnlyCollection<string> stationIds, bool force, CancellationToken cancellationToken = default)
    {
        RefreshRequests.Add(stationIds.ToList());
        RefreshForces.Add(force);
        RefreshEntered.TrySetResult();
        if (RefreshGate is { } gate)
        {
            await gate.WaitAsync(cancellationToken);
        }

        return RefreshException is null ? RefreshResult : throw RefreshException;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<StationInfo>> GetKnownStationsAsync(IReadOnlyCollection<string> stationIds, CancellationToken cancellationToken = default)
    {
        KnownRequests.Add(stationIds.ToList());
        return Task.FromResult<IReadOnlyList<StationInfo>>(KnownStations.Where(s => stationIds.Contains(s.Id)).ToList());
    }

    /// <inheritdoc />
    public Task<StationSearchResult> SearchNearbyAsync(StationSearchQuery query, CancellationToken cancellationToken = default)
    {
        Queries.Add(query);
        return SearchException is null ? Task.FromResult(Result) : Task.FromException<StationSearchResult>(SearchException);
    }

    /// <inheritdoc />
    public async Task<StationDetailResult> GetStationDetailAsync(string stationId, CancellationToken cancellationToken = default)
    {
        DetailRequests.Add(stationId);
        if (DetailGate is { } gate)
        {
            await (IgnoreCancellation ? gate : gate.WaitAsync(cancellationToken));
        }

        return DetailException is null ? DetailResult : throw DetailException;
    }

    /// <inheritdoc />
    public Task<PriceFailure> CheckAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromException<PriceFailure>(new NotSupportedException());
    }

    private static StationDetailResult CreateUnknownStation()
    {
        return new StationDetailResult(null, PriceDataSource.Live, PriceFailure.None);
    }

    private static TaskCompletionSource CreateSource()
    {
        return new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}

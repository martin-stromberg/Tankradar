using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Pricing;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// <see cref="IFuelPriceService"/> für Tests mit vorgegebener Antwort; protokolliert die Suchanfragen.
/// </summary>
public sealed class FakeFuelPriceService : IFuelPriceService
{
    /// <summary>
    /// Das gelieferte Suchergebnis (Standard: keine Tankstellen, live).
    /// </summary>
    /// <returns>Der Wert.</returns>
    public StationSearchResult Result { get; set; } = new([], PriceDataSource.Live, PriceFailure.None);

    /// <summary>
    /// Wenn gesetzt, wird sie von <see cref="SearchNearbyAsync"/> ausgelöst.
    /// </summary>
    public Exception? SearchException { get; set; }

    /// <summary>
    /// Alle empfangenen Suchanfragen in Reihenfolge.
    /// </summary>
    public List<StationSearchQuery> Queries { get; } = [];

    /// <inheritdoc />
    public Task<StationSearchResult> SearchNearbyAsync(StationSearchQuery query, CancellationToken cancellationToken = default)
    {
        Queries.Add(query);
        return SearchException is null ? Task.FromResult(Result) : Task.FromException<StationSearchResult>(SearchException);
    }

    /// <inheritdoc />
    public Task<StationDetailResult> GetStationDetailAsync(string stationId, CancellationToken cancellationToken = default)
    {
        return Task.FromException<StationDetailResult>(new NotSupportedException());
    }

    /// <inheritdoc />
    public Task<PriceFailure> CheckAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromException<PriceFailure>(new NotSupportedException());
    }
}

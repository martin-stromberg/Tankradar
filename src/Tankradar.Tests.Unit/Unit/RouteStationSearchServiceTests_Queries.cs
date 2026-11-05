using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Services.Routing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Ablauf der Umkreisabfragen: strikt nacheinander, Anzahl und Parameter, Abbruch bei Fehler oder durch das Abbruchsignal, Fortschritt.
/// </summary>
public class RouteStationSearchServiceTests_Queries : BaseTest
{
    private static readonly FuelType[] Fuels = [FuelType.Diesel];

    private readonly ScriptedFuelPriceService _prices = new();
    private readonly RouteProfile _route = RouteTestData.NorthFromBerlin(1.5);

    private IReadOnlyList<RouteQueryPoint> Queries(int distanceKm = 2)
    {
        Assert.True(RouteSampler.TryCreateQueryPoints(_route, distanceKm, RouteTestOptions.Relaxed(), out var queries));
        return queries;
    }

    /// <summary>
    /// Prüft, dass je Abfragepunkt genau eine Abfrage mit dessen Mittelpunkt, Radius und den Sorten gestellt wird und nie zwei gleichzeitig laufen.
    /// </summary>
    [Fact]
    public async Task SearchAsync_QueriesEachPointOnceSequentially()
    {
        var queries = Queries();

        await RouteTestOptions.StationSearch(_prices).SearchAsync(_route, queries, 2, Fuels, null, CancellationToken.None);

        Assert.Equal(queries.Count, _prices.Queries.Count);
        Assert.Equal(1, _prices.MaxConcurrent);
        for (var i = 0; i < queries.Count; i++)
        {
            Assert.Equal(queries[i].Latitude, _prices.Queries[i].Latitude);
            Assert.Equal(queries[i].Longitude, _prices.Queries[i].Longitude);
            Assert.Equal(queries[i].RadiusKm, _prices.Queries[i].RadiusKm);
            Assert.Equal(Fuels, _prices.Queries[i].FuelTypes);
            Assert.InRange(_prices.Queries[i].RadiusKm, 1, 25);
        }
    }

    /// <summary>
    /// Prüft, dass bei einem Fehler einer Abfrage abgebrochen wird: keine weiteren Abfragen und keine Teilergebnisse.
    /// </summary>
    [Fact]
    public async Task SearchAsync_FailureInQuery_StopsWithoutPartialResults()
    {
        var queries = Queries();
        Assert.True(queries.Count >= 3);
        _prices.Respond = (_, index) => index == 1
            ? new StationSearchResult([], PriceDataSource.OfflineFallback, PriceFailure.Unreachable)
            : RouteStationFactory.Live(RouteStationFactory.At(index + 1, 52.6 + (index * 0.1), 13.405));

        var result = await RouteTestOptions.StationSearch(_prices).SearchAsync(_route, queries, 2, Fuels, null, CancellationToken.None);

        Assert.Equal(PriceFailure.Unreachable, result.Failure);
        Assert.Empty(result.Stations);
        Assert.Equal(2, _prices.Queries.Count);
    }

    /// <summary>
    /// Prüft, dass das Abbruchsignal die Suche beendet und keine weitere Abfrage stellt.
    /// </summary>
    [Fact]
    public async Task SearchAsync_Cancelled_ThrowsAndStopsQuerying()
    {
        var queries = Queries();
        using var cancellation = new CancellationTokenSource();
        _prices.Respond = (_, index) =>
        {
            if (index == 0)
            {
                cancellation.Cancel();
            }

            return RouteStationFactory.Live();
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => RouteTestOptions.StationSearch(_prices).SearchAsync(_route, queries, 2, Fuels, null, cancellation.Token));

        Assert.Single(_prices.Queries);
    }

    /// <summary>
    /// Prüft die Fortschrittsmeldungen „k von n“.
    /// </summary>
    [Fact]
    public async Task SearchAsync_ReportsProgressPerQuery()
    {
        var queries = Queries();
        var progress = new ListProgress();

        await RouteTestOptions.StationSearch(_prices).SearchAsync(_route, queries, 2, Fuels, progress, CancellationToken.None);

        Assert.Equal(queries.Count, progress.Reports.Count);
        Assert.All(progress.Reports, report => Assert.Equal(RouteSearchPhase.SearchingStations, report.Phase));
        Assert.Equal(Enumerable.Range(1, queries.Count).ToArray(), progress.Reports.Select(r => r.Current).ToArray());
        Assert.All(progress.Reports, report => Assert.Equal(queries.Count, report.Total));
    }

    /// <summary>
    /// Prüft, dass ohne Abfragepunkte nichts abgefragt wird.
    /// </summary>
    [Fact]
    public async Task SearchAsync_NoQueryPoints_ReturnsEmptyResult()
    {
        var result = await RouteTestOptions.StationSearch(_prices).SearchAsync(_route, [], 2, Fuels, null, CancellationToken.None);

        Assert.Empty(result.Stations);
        Assert.Empty(_prices.Queries);
    }
}

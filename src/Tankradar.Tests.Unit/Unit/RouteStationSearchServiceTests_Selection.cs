using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Services.Routing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Auswahl der Tankstellen entlang der Route: Deduplizierung, Abstandsfilter, Sortierung nach Streckenposition, Enden der Route.
/// </summary>
public class RouteStationSearchServiceTests_Selection : BaseTest
{
    private static readonly FuelType[] Fuels = [FuelType.SuperE5, FuelType.Diesel];

    private readonly ScriptedFuelPriceService _prices = new();
    private readonly RouteProfile _route = RouteTestData.NorthFromBerlin(0.5);

    private async Task<RouteStationSearchResult> SearchAsync(int distanceKm = 2)
    {
        Assert.True(RouteSampler.TryCreateQueryPoints(_route, distanceKm, new RouteSearchOptions(), out var queries));
        return await new RouteStationSearchService(_prices).SearchAsync(_route, queries, distanceKm, Fuels, null, CancellationToken.None);
    }

    /// <summary>
    /// Prüft, dass Stationen innerhalb des Abstands in der Reihenfolge der Streckenposition geliefert werden, auch wenn die Quelle sie anders ordnet.
    /// </summary>
    [Fact]
    public async Task SearchAsync_StationsInBand_AreSortedByAlongPosition()
    {
        var late = RouteStationFactory.At(1, 52.90, 13.405);
        var early = RouteStationFactory.At(2, 52.60, 13.405 + RouteStationFactory.EastOffset(52.6, 1.0));
        var middle = RouteStationFactory.At(3, 52.75, 13.405);
        _prices.Respond = (_, _) => RouteStationFactory.Live(late, early, middle);

        var result = await SearchAsync();

        Assert.Equal(PriceFailure.None, result.Failure);
        Assert.Equal([early.Id, middle.Id, late.Id], result.Stations.Select(s => s.Station.Id).ToArray());
        Assert.True(result.Stations[0].AlongKm < result.Stations[1].AlongKm);
        Assert.Equal(1.0, result.Stations[0].OffsetKm, 1);
    }

    /// <summary>
    /// Prüft den Abstandsfilter: knapp innerhalb bleibt, knapp außerhalb entfällt.
    /// </summary>
    [Fact]
    public async Task SearchAsync_StationJustInsideAndOutsideBand_IsFiltered()
    {
        var inside = RouteStationFactory.At(1, 52.70, 13.405 + RouteStationFactory.EastOffset(52.7, 1.95));
        var outside = RouteStationFactory.At(2, 52.70, 13.405 + RouteStationFactory.EastOffset(52.7, 2.05));
        var far = RouteStationFactory.At(3, 52.70, 13.405 + RouteStationFactory.EastOffset(52.7, 12));
        _prices.Respond = (_, _) => RouteStationFactory.Live(inside, outside, far);

        var result = await SearchAsync(2);

        Assert.Equal([inside.Id], result.Stations.Select(s => s.Station.Id).ToArray());
    }

    /// <summary>
    /// Prüft, dass ein größerer Abstand mehr Stationen liefert.
    /// </summary>
    /// <param name="distanceKm">Der Abstand.</param>
    /// <param name="expectedCount">Die erwartete Zahl der Stationen.</param>
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(5, 3)]
    [InlineData(10, 4)]
    public async Task SearchAsync_DistanceSteps_ChangeNumberOfStations(int distanceKm, int expectedCount)
    {
        var stations = new[]
        {
            RouteStationFactory.At(1, 52.65, 13.405 + RouteStationFactory.EastOffset(52.65, 0.5)),
            RouteStationFactory.At(2, 52.70, 13.405 + RouteStationFactory.EastOffset(52.7, 1.5)),
            RouteStationFactory.At(3, 52.75, 13.405 + RouteStationFactory.EastOffset(52.75, 4)),
            RouteStationFactory.At(4, 52.80, 13.405 + RouteStationFactory.EastOffset(52.8, 9)),
            RouteStationFactory.At(5, 52.85, 13.405 + RouteStationFactory.EastOffset(52.85, 12)),
        };
        _prices.Respond = (_, _) => RouteStationFactory.Live(stations);

        var result = await SearchAsync(distanceKm);

        Assert.Equal(expectedCount, result.Stations.Count);
    }

    /// <summary>
    /// Prüft, dass dieselbe Station aus mehreren Abfragen nur einmal erscheint.
    /// </summary>
    [Fact]
    public async Task SearchAsync_SameStationInSeveralQueries_AppearsOnce()
    {
        var shared = RouteStationFactory.At(1, 52.70, 13.405);
        _prices.Respond = (_, _) => RouteStationFactory.Live(shared);
        var longRoute = RouteTestData.NorthFromBerlin(1.5);
        Assert.True(RouteSampler.TryCreateQueryPoints(longRoute, 2, new RouteSearchOptions(), out var queries));
        Assert.True(queries.Count > 1);

        var result = await new RouteStationSearchService(_prices).SearchAsync(longRoute, queries, 2, Fuels, null, CancellationToken.None);

        Assert.Single(result.Stations);
    }

    /// <summary>
    /// Prüft Start und Ziel: Eine Station am Start hat die Position 0, eine kurz hinter dem Ziel liegt am Ende, eine weiter hinter dem Ziel entfällt.
    /// </summary>
    [Fact]
    public async Task SearchAsync_StationsAtStartAndBeyondDestination_AreHandled()
    {
        var atStart = RouteStationFactory.At(1, 52.52, 13.405);
        var justBeyond = RouteStationFactory.At(2, 53.02 + (1.0 / 111.195), 13.405);
        var farBeyond = RouteStationFactory.At(3, 53.02 + (3.0 / 111.195), 13.405);
        _prices.Respond = (_, _) => RouteStationFactory.Live(atStart, justBeyond, farBeyond);

        var result = await SearchAsync();

        Assert.Equal([atStart.Id, justBeyond.Id], result.Stations.Select(s => s.Station.Id).ToArray());
        Assert.Equal(0, result.Stations[0].AlongKm, 3);
        Assert.Equal(_route.LengthKm, result.Stations[1].AlongKm, 3);
        Assert.Equal(1.0, result.Stations[1].OffsetKm, 1);
    }

    /// <summary>
    /// Prüft, dass Stationen ohne gültige Position verworfen werden.
    /// </summary>
    [Fact]
    public async Task SearchAsync_StationWithoutValidPosition_IsDropped()
    {
        var nowhere = RouteStationFactory.At(1, 0, 0);
        var good = RouteStationFactory.At(2, 52.70, 13.405);
        _prices.Respond = (_, _) => RouteStationFactory.Live(nowhere, good);

        var result = await SearchAsync();

        Assert.Equal([good.Id], result.Stations.Select(s => s.Station.Id).ToArray());
    }

    /// <summary>
    /// Prüft, dass bei gleicher Streckenposition die Kennung die Reihenfolge bestimmt.
    /// </summary>
    [Fact]
    public async Task SearchAsync_SameAlongPosition_IsOrderedById()
    {
        var second = RouteStationFactory.At(2, 52.70, 13.405 + RouteStationFactory.EastOffset(52.7, 0.5));
        var first = RouteStationFactory.At(1, 52.70, 13.405 - RouteStationFactory.EastOffset(52.7, 0.5));
        _prices.Respond = (_, _) => RouteStationFactory.Live(second, first);

        var result = await SearchAsync();

        Assert.Equal([first.Id, second.Id], result.Stations.Select(s => s.Station.Id).ToArray());
    }
}

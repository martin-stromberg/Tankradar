using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Services.Search;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Aufbereitung der Ergebnisliste einer Routensuche: Streckenposition als Entfernung, Abstand zur Route, Filter und Sortierungen.
/// </summary>
public class StationResultBuilderTests_Route : BaseTest
{
    private static readonly DateTime Now = SearchTestData.Now;

    private static RouteStation At(int number, string name, double along, double offset, decimal e5, decimal? diesel)
    {
        var prices = diesel is { } d ? new[] { (FuelType.SuperE5, e5), (FuelType.Diesel, d) } : [(FuelType.SuperE5, e5)];
        return new RouteStation(StationFactory.CreateCustom(number, name, 99, Now, true, true, prices), along, offset);
    }

    private static RouteStation[] Sample()
    {
        return
        [
            At(1, "Zeta", 30.0, 1.2, 1.90m, 1.50m),
            At(2, "Alpha", 5.5, 0.4, 1.70m, 1.80m),
            At(3, "Mitte", 12.34, 1.9, 1.80m, null),
        ];
    }

    /// <summary>
    /// Prüft, dass die Streckenposition als Entfernung dient und mit „ab Start“ angezeigt wird, zusammen mit dem Abstand zur Route.
    /// </summary>
    [Fact]
    public void BuildForRoute_UsesAlongPositionAsDistance()
    {
        var items = StationResultBuilder.BuildForRoute(Sample(), SearchTestData.AllFuels, null, ResultSortOrder.Distance, Now);

        Assert.Equal(["Alpha", "Mitte", "Zeta"], items.Select(item => item.Name));
        Assert.Equal([5.5, 12.34, 30.0], items.Select(item => item.DistanceKm!.Value));
        Assert.Equal(["5,5 km ab Start", "12,3 km ab Start", "30,0 km ab Start"], items.Select(item => item.DistanceText));
        Assert.Equal(["0,4 km abseits der Route", "1,9 km abseits der Route", "1,2 km abseits der Route"], items.Select(item => item.RouteOffsetText));
        Assert.Equal([0.4, 1.9, 1.2], items.Select(item => item.RouteOffsetKm!.Value));
        Assert.All(items, item => Assert.True(item.HasRouteOffset));
    }

    /// <summary>
    /// Prüft die Sortierungen nach Preis und Name.
    /// </summary>
    [Fact]
    public void BuildForRoute_SortsByPriceAndName()
    {
        var byPrice = StationResultBuilder.BuildForRoute(Sample(), SearchTestData.AllFuels, null, ResultSortOrder.Price, Now);
        var byName = StationResultBuilder.BuildForRoute(Sample(), SearchTestData.AllFuels, null, ResultSortOrder.Name, Now);

        Assert.Equal(["Alpha", "Mitte", "Zeta"], byPrice.Select(item => item.Name));
        Assert.Equal(["Alpha", "Mitte", "Zeta"], byName.Select(item => item.Name));
    }

    /// <summary>
    /// Prüft den Filter nach Sorte: Tankstellen ohne diese Sorte entfallen, die Reihenfolge nach Preis richtet sich nach der Sorte.
    /// </summary>
    [Fact]
    public void BuildForRoute_FilterByFuel_DropsStationsWithoutFuel()
    {
        var items = StationResultBuilder.BuildForRoute(Sample(), SearchTestData.AllFuels, FuelType.Diesel, ResultSortOrder.Price, Now);

        Assert.Equal(["Zeta", "Alpha"], items.Select(item => item.Name));
    }

    /// <summary>
    /// Prüft, dass die normale Aufbereitung weiterhin keinen Abstand zur Route trägt und nicht „ab Start“ meldet.
    /// </summary>
    [Fact]
    public void Build_WithoutRoute_HasNoRouteOffset()
    {
        var stations = new[] { StationFactory.CreateCustom(1, "Nahe", 1.4, Now, true, true, (FuelType.SuperE5, 1.8m)) };

        var item = Assert.Single(StationResultBuilder.Build(stations, SearchTestData.AllFuels, null, ResultSortOrder.Price, Now));

        Assert.False(item.HasRouteOffset);
        Assert.Null(item.RouteOffsetKm);
        Assert.Equal("1,4 km", item.DistanceText);
    }

    /// <summary>
    /// Prüft, dass die Quelldaten unverändert bleiben (Kopie je Station) und dass eine leere Liste eine leere Ergebnisliste ergibt.
    /// </summary>
    [Fact]
    public void BuildForRoute_DoesNotModifySourceAndHandlesEmptyList()
    {
        var sample = Sample();

        StationResultBuilder.BuildForRoute(sample, SearchTestData.AllFuels, null, ResultSortOrder.Distance, Now);

        Assert.All(sample, routeStation => Assert.Equal(99, routeStation.Station.DistanceKm));
        Assert.Empty(StationResultBuilder.BuildForRoute([], SearchTestData.AllFuels, null, ResultSortOrder.Distance, Now));
    }
}

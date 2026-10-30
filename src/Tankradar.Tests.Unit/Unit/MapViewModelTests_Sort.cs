using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Sortierung des Ergebnisses nach Preis, Name und Entfernung im <see cref="MapViewModel"/> mit Ergebnissen, deren Reihenfolgen paarweise verschieden sind
/// (Standortsuche und Route), einschließlich Rücksetzen der Seite und Behandlung fehlender Werte.
/// </summary>
public class MapViewModelTests_Sort : MapViewModelRouteTestBase
{
    private IEnumerable<string> Names()
    {
        return ViewModel.Stations.Select(item => item.Name);
    }

    private void Sort(ResultSortOrder order)
    {
        ViewModel.SortOptions.Single(option => option.Value == order).SelectCommand.Execute(null);
    }

    private StationInfo[] DistinctOrderStations()
    {
        // Name: Alpha, Bravo, Charlie; Preis: Bravo, Charlie, Alpha; Entfernung: Charlie, Alpha, Bravo.
        return
        [
            StationFactory.CreateCustom(1, "Alpha", 2.0, Now, true, true, (FuelType.SuperE5, 1.90m)),
            StationFactory.CreateCustom(2, "Bravo", 3.0, Now, true, true, (FuelType.SuperE5, 1.70m)),
            StationFactory.CreateCustom(3, "Charlie", 1.0, Now, true, true, (FuelType.SuperE5, 1.80m)),
        ];
    }

    private StationInfo[] ManyDistinctStations(int count)
    {
        // Preis und Name steigen mit der Nummer, die Entfernung fällt.
        return Enumerable.Range(1, count)
            .Select(number => StationFactory.CreateCustom(number, $"Station {number:D3}", count + 1 - number, Now, null, true, (FuelType.SuperE5, 1.50m + (number / 1000m))))
            .ToArray();
    }

    /// <summary>
    /// Prüft, dass die drei Sortierungen der Standortsuche paarweise verschiedene Reihenfolgen liefern und ohne neue Abfrage wechseln.
    /// </summary>
    [Fact]
    public async Task Sort_ThreeOrders_AreDistinctForSearchResult()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, DistinctOrderStations());
        await AppearAsync();
        await SearchAsync();
        Assert.Equal(["Bravo", "Charlie", "Alpha"], Names());

        Sort(ResultSortOrder.Name);
        Assert.Equal(["Alpha", "Bravo", "Charlie"], Names());

        Sort(ResultSortOrder.Distance);
        Assert.Equal(["Charlie", "Alpha", "Bravo"], Names());

        Sort(ResultSortOrder.Price);
        Assert.Equal(["Bravo", "Charlie", "Alpha"], Names());
        Assert.Single(Location.Calls);
        Assert.Single(Prices.Queries);
    }

    /// <summary>
    /// Prüft, dass ein Sortierwechsel nach "Weitere anzeigen" die erste Seite in der neuen Reihenfolge zeigt und den Zähler zurücksetzt.
    /// </summary>
    [Fact]
    public async Task Sort_AfterMultiplePages_ShowsFirstPageInNewOrder()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, ManyDistinctStations(60));
        await AppearAsync();
        await SearchAsync();
        ViewModel.ShowMoreCommand.Execute(null);
        Assert.Equal(50, ViewModel.Stations.Count);
        Assert.True(ViewModel.ShowMoreVisible);

        Sort(ResultSortOrder.Distance);

        Assert.Equal(MapViewModel.PageSize, ViewModel.Stations.Count);
        Assert.Equal("Station 060", ViewModel.Stations[0].Name);
        Assert.Equal("Station 036", ViewModel.Stations[^1].Name);
        Assert.Equal("Weitere anzeigen (35 übrig)", ViewModel.ShowMoreText);
        Assert.True(ViewModel.ShowMoreVisible);

        ViewModel.ShowMoreCommand.Execute(null);
        Sort(ResultSortOrder.Name);

        Assert.Equal(MapViewModel.PageSize, ViewModel.Stations.Count);
        Assert.Equal("Station 001", ViewModel.Stations[0].Name);
        Assert.Equal("Weitere anzeigen (35 übrig)", ViewModel.ShowMoreText);
    }

    /// <summary>
    /// Prüft, dass Stationen ohne Preis der Sorte bzw. ohne Entfernung bei jeder Sortierung am Ende stehen.
    /// </summary>
    [Fact]
    public async Task Sort_WithMissingValues_PutsUnknownLast()
    {
        Prices.Result = Result(
            PriceDataSource.Live,
            PriceFailure.None,
            StationFactory.CreateCustom(1, "Ohneentfernung", null, Now, true, true, (FuelType.SuperE5, 1.60m)),
            StationFactory.CreateCustom(2, "Ohnepreis", 1.0, Now, true, true, (FuelType.Diesel, 1.50m)),
            StationFactory.CreateCustom(3, "Vollstaendig", 2.0, Now, true, true, (FuelType.SuperE5, 1.70m)));
        await AppearAsync();
        await SearchAsync();

        Assert.Equal(["Ohneentfernung", "Vollstaendig", "Ohnepreis"], Names());

        Sort(ResultSortOrder.Distance);
        Assert.Equal(["Ohnepreis", "Vollstaendig", "Ohneentfernung"], Names());
    }

    /// <summary>
    /// Prüft, dass die Routensuche alle drei Sortierungen mit paarweise verschiedener Reihenfolge anbietet.
    /// </summary>
    [Fact]
    public async Task Sort_RouteResult_PriceDiffersFromNameAndDistance()
    {
        await PrepareRouteAsync();
        RouteSearch.Result = RouteSuccess(
            "A",
            "B",
            RouteStationAt(1, "Zeta", 4.0, 0.5, 1.70m, 1.50m),
            RouteStationAt(2, "Alpha", 12.0, 0.5, 1.80m, 1.70m),
            RouteStationAt(3, "Mitte", 8.0, 0.5, 1.60m, 1.60m));
        await SearchAsync();
        Assert.Equal(["Mitte", "Zeta", "Alpha"], Names());

        Sort(ResultSortOrder.Name);
        Assert.Equal(["Alpha", "Mitte", "Zeta"], Names());

        Sort(ResultSortOrder.Distance);
        Assert.Equal(["Zeta", "Mitte", "Alpha"], Names());

        Sort(ResultSortOrder.Price);
        Assert.Equal(["Mitte", "Zeta", "Alpha"], Names());
        Assert.Single(RouteSearch.Requests);
    }
}

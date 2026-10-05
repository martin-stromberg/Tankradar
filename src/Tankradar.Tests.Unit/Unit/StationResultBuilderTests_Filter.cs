using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Search;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Spritsortenfilter der Ergebnisaufbereitung.
/// </summary>
public class StationResultBuilderTests_Filter : BaseTest
{
    private static readonly DateTime Now = SearchTestData.Now;

    private static List<StationInfo> Stations()
    {
        return
        [
            StationFactory.CreateCustom(1, "Eins", 1.0, Now, null, null, (FuelType.SuperE5, 1.80m), (FuelType.Diesel, 1.60m)),
            StationFactory.CreateCustom(2, "Zwei", 2.0, Now, null, null, (FuelType.SuperE5, 1.85m), (FuelType.SuperE10, 1.75m)),
        ];
    }

    /// <summary>
    /// Prüft, dass „Alle“ (kein Filter) alle Tankstellen behält.
    /// </summary>
    [Fact]
    public void Build_NoFilter_KeepsAllStations()
    {
        var items = StationResultBuilder.Build(Stations(), SearchTestData.AllFuels, null, ResultSortOrder.Distance, Now);

        Assert.Equal(["Eins", "Zwei"], items.Select(item => item.Name));
    }

    /// <summary>
    /// Prüft, dass ein Sortenfilter Tankstellen ohne diese Sorte entfernt, die Preiszeilen aber alle gewählten Sorten zeigen.
    /// </summary>
    [Fact]
    public void Build_FuelFilter_RemovesStationsWithoutThatFuelButKeepsAllPriceLines()
    {
        var items = StationResultBuilder.Build(Stations(), SearchTestData.AllFuels, FuelType.Diesel, ResultSortOrder.Distance, Now);

        var item = Assert.Single(items);
        Assert.Equal("Eins", item.Name);
        Assert.Equal([FuelType.SuperE5, FuelType.Diesel], item.PriceLines.Select(line => line.FuelType));
    }

    /// <summary>
    /// Prüft, dass eine Sorte, die keine Tankstelle führt, eine leere Liste ergibt.
    /// </summary>
    [Fact]
    public void Build_FuelFilterWithoutMatch_ReturnsEmptyList()
    {
        var stations = new List<StationInfo> { StationFactory.CreateCustom(1, "Eins", 1.0, Now, null, null, (FuelType.SuperE5, 1.80m)) };

        var items = StationResultBuilder.Build(stations, SearchTestData.AllFuels, FuelType.SuperE10, ResultSortOrder.Price, Now);

        Assert.Empty(items);
    }

    /// <summary>
    /// Prüft, dass ein nicht in den Einstellungen gewählter oder undefinierter Filter wie „Alle“ behandelt wird.
    /// </summary>
    /// <param name="filter">Der Filterwert.</param>
    [Theory]
    [InlineData(FuelType.SuperE10)]
    [InlineData((FuelType)99)]
    public void Build_FilterNotSelectedInSettings_IsIgnored(FuelType filter)
    {
        var items = StationResultBuilder.Build(Stations(), SearchTestData.Only(FuelType.SuperE5, FuelType.Diesel), filter, ResultSortOrder.Distance, Now);

        Assert.Equal(2, items.Count);
    }

    /// <summary>
    /// Prüft, dass eine leere Trefferliste eine leere Ergebnisliste ergibt.
    /// </summary>
    [Fact]
    public void Build_NoStations_ReturnsEmptyList()
    {
        Assert.Empty(StationResultBuilder.Build([], SearchTestData.AllFuels, null, ResultSortOrder.Price, Now));
    }
}

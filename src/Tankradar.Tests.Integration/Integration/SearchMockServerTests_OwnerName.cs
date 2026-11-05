using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Search;
using Tankradar.Tests.Integration.Integration.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft über Dienst, Client, Preis-Cache und Mock-Server den Anzeigenamen einer Tankstelle, deren Name der Inhaber ist: Die Marke steht vorn, der Stammdatenname klein
/// darunter, Stationen ohne Marke behalten ihren Namen, und ein eigener Name bleibt bei jedem Abruf erhalten.
/// </summary>
public class SearchMockServerTests_OwnerName : SearchMockServerTestBase
{
    /// <summary>
    /// Prüft die Liste: Die Tankstelle mit Inhabernamen erscheint unter ihrer Marke, Stationen ohne Marke unter ihrem Namen.
    /// </summary>
    [Fact]
    public async Task Search_OwnerNamedStation_ShowsBrandWithSubName()
    {
        Server.AddOwnerNamedStation();
        var result = await CreateService().SearchNearbyAsync(Query(5));

        var items = StationResultBuilder.Build(result.Stations, AllFuels.Select(fuel => new FuelTypeSelection(fuel, true)).ToList(), null, ResultSortOrder.Distance, DateTime.UtcNow);

        var owner = items.Single(item => item.Id == MockTankerkoenigServer.StationZeta);
        Assert.Equal(("Westfalen", "Klaus Heinrich"), (owner.Name, owner.SubName));
        var alpha = items.Single(item => item.Id == MockTankerkoenigServer.StationAlpha);
        Assert.Equal("Alpha Tankstelle", alpha.Name);
        Assert.False(alpha.HasSubName);
    }

    /// <summary>
    /// Prüft, dass ein eigener Name nach einem erneuten Live-Abruf der Details und einer neuen Suche weiter gilt und die Marke nicht verdrängt wird.
    /// </summary>
    [Fact]
    public async Task CustomName_SurvivesLiveDetailAndSearch()
    {
        Server.AddOwnerNamedStation();
        var service = CreateService();
        var first = await service.GetStationDetailAsync(MockTankerkoenigServer.StationZeta);
        Assert.Equal("Westfalen", first.Station!.DisplayName);

        Assert.Equal(StationNameResult.Ok, await service.SetCustomNameAsync(MockTankerkoenigServer.StationZeta, "Zuhause"));
        Clock.Advance(TimeSpan.FromHours(2));
        var second = await service.GetStationDetailAsync(MockTankerkoenigServer.StationZeta);
        var search = await service.SearchNearbyAsync(Query(5));

        Assert.Equal(PriceDataSource.Live, second.Source);
        Assert.Equal(("Zuhause", "Westfalen", "Klaus Heinrich"), (second.Station!.DisplayName, second.Station.Brand, second.Station.Name));
        Assert.Equal("Zuhause", search.Stations.Single(s => s.Id == MockTankerkoenigServer.StationZeta).DisplayName);
        Assert.Equal("Klaus Heinrich", search.Stations.Single(s => s.Id == MockTankerkoenigServer.StationZeta).SubName);
    }
}

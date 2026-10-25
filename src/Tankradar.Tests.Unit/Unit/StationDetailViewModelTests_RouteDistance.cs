using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Services.Search;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass die Detailansicht einer Tankstelle aus der Routensuche die Entfernung als Streckenposition („ab Start“) mit dem Abstand zur Route nennt.
/// </summary>
public class StationDetailViewModelTests_RouteDistance : StationDetailViewModelTestBase
{
    private async Task LoadRouteStationAsync()
    {
        var station = CreateStation(Now.AddMinutes(-5), Now.AddMinutes(-5));
        Prices.DetailResult = new StationDetailResult(station, PriceDataSource.Live, PriceFailure.None);
        var routeStation = new RouteStation(
            new StationInfo
            {
                Id = StationId,
                Name = "Alpha Tankstelle",
                Prices = [new FuelPrice(FuelType.SuperE5, 1.850m, Now.AddMinutes(-10))],
            },
            8.04,
            0.8);
        var origin = StationResultBuilder.BuildForRoute([routeStation], MAUI.Models.AppSettings.CreateDefault().FuelTypes, null, ResultSortOrder.Price, Now).Single();
        ViewModel.Show(origin);
        Assert.Equal("8,0 km ab Start, 0,8 km abseits der Route", ViewModel.Detail!.DistanceText);
        await AppearAsync();
    }

    /// <summary>
    /// Prüft, dass die Entfernung sowohl in der Anfangsanzeige als auch nach dem Laden der Details den Text der Liste trägt.
    /// </summary>
    [Fact]
    public async Task RouteOrigin_ShowsAlongPositionAndOffsetBeforeAndAfterLoading()
    {
        await LoadRouteStationAsync();

        Assert.Equal("8,0 km ab Start, 0,8 km abseits der Route", ViewModel.Detail!.DistanceText);
        Assert.True(ViewModel.Detail.HasDistance);
    }

    /// <summary>
    /// Prüft, dass die Umkreissuche unverändert die einfache Entfernung zeigt.
    /// </summary>
    [Fact]
    public async Task NearbyOrigin_KeepsPlainDistance()
    {
        Prices.DetailResult = new StationDetailResult(CreateStation(Now.AddMinutes(-5), Now.AddMinutes(-5)), PriceDataSource.Live, PriceFailure.None);
        ViewModel.Show(CreateOrigin());
        await AppearAsync();

        Assert.Equal("1,8 km", ViewModel.Detail!.DistanceText);
    }
}

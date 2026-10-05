using System.Diagnostics;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Search;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Integration.Integration.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft die Umkreissuche mit dem realen Antwortformat der Quelle (<c>list.php</c> ohne Öffnungszeiten) und große Ergebnismengen.
/// Es wird ausschließlich der lokale Mock angesprochen.
/// </summary>
public class SearchMockServerTests_RealFormat : SearchMockServerTestBase
{
    private bool AutomatedAfterBuild(IReadOnlyList<StationInfo> stations, string name)
    {
        var items = StationResultBuilder.Build(stations, AppSettings.CreateDefault().FuelTypes, null, ResultSortOrder.Name, Clock.GetUtcNow().UtcDateTime);
        return items.Single(item => item.Name == name).IsAutomatedStation;
    }

    /// <summary>
    /// Prüft, dass die Umkreissuche ohne bekannte Details keinen Hinweis „Automatentankstelle“ liefert (die Quelle liefert in der Liste keine Öffnungszeiten).
    /// </summary>
    [Fact]
    public async Task Search_WithoutKnownDetails_ShowsNoAutomatedHint()
    {
        var result = await CreateService().SearchNearbyAsync(Query(5));

        Assert.All(result.Stations, station => Assert.Null(station.WholeDay));
        Assert.All(result.Stations, station => Assert.Empty(station.OpeningTimes));
        Assert.False(AutomatedAfterBuild(result.Stations, "Alpha Tankstelle"));
    }

    /// <summary>
    /// Prüft, dass der Hinweis erscheint, sobald die Details der Tankstelle aus einer früheren Detailabfrage im Cache liegen.
    /// </summary>
    [Fact]
    public async Task Search_AfterDetailRequest_ShowsAutomatedHintFromCache()
    {
        var service = CreateService();
        await service.GetStationDetailAsync(MockTankerkoenigServer.StationAlpha);
        await service.GetStationDetailAsync(MockTankerkoenigServer.StationBeta);

        var result = await service.SearchNearbyAsync(Query(5));

        Assert.Equal(PriceDataSource.Live, result.Source);
        Assert.True(AutomatedAfterBuild(result.Stations, "Alpha Tankstelle"));
        Assert.False(AutomatedAfterBuild(result.Stations, "Beta Tankstelle"));
    }

    /// <summary>
    /// Prüft bei 300 Tankstellen, dass nur die erste Seite dargestellt wird, „Weitere anzeigen“ nachlädt und Filter- sowie Sortierwechsel zügig bleiben.
    /// </summary>
    [Fact]
    public async Task Search_With300Stations_ShowsPagedListAndStaysFast()
    {
        Server.AddGeneratedStations(300);
        var viewModel = CreateViewModel(CreateService());
        viewModel.OnAppearing();
        await viewModel.LastSettingsTask;

        await viewModel.SearchAsync();

        Assert.Equal(302, viewModel.TotalStationCount);
        Assert.Equal(MapViewModel.PageSize, viewModel.Stations.Count);
        Assert.True(viewModel.HasMore);
        Assert.Contains("277", viewModel.ShowMoreText, StringComparison.Ordinal);

        var stopwatch = Stopwatch.StartNew();
        for (var round = 0; round < 10; round++)
        {
            viewModel.FuelFilterOptions.Single(option => option.AutomationKey == "Diesel").IsSelected = true;
            viewModel.SortOptions.Single(option => option.Value == ResultSortOrder.Distance).IsSelected = true;
            viewModel.FuelFilterOptions.Single(option => option.AutomationKey == "All").IsSelected = true;
            viewModel.SortOptions.Single(option => option.Value == ResultSortOrder.Name).IsSelected = true;
        }

        stopwatch.Stop();
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"40 Filter-/Sortierwechsel dauerten {stopwatch.Elapsed.TotalMilliseconds:0} ms.");
        Assert.Equal(MapViewModel.PageSize, viewModel.Stations.Count);

        viewModel.ShowMoreCommand.Execute(null);
        Assert.Equal(2 * MapViewModel.PageSize, viewModel.Stations.Count);
        viewModel.SortOptions.Single(option => option.Value == ResultSortOrder.Price).IsSelected = true;
        Assert.Equal(MapViewModel.PageSize, viewModel.Stations.Count);
    }
}

using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Routing;
using Tankradar.TestSupport;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft die vorläufige Abfragegrenze (höchstens drei Umkreisabfragen mit Mindestabstand) mit echten Diensten gegen die Mock-Server.
/// </summary>
public class RouteSearchMockServerTests_Limit : RouteSearchMockServerTestBase
{
    /// <summary>
    /// Prüft, dass die Abfragen der Routensuche den eingestellten Mindestabstand einhalten, auch zwischen zwei Suchen, und die Fortschrittsanzeige die anstehende Abfrage nennt.
    /// </summary>
    [Fact]
    public async Task Search_WithMinimumInterval_SpacesQueriesAcrossSearches()
    {
        Routing.SetRoute([(52.52, 13.405), (52.80, 13.405), (53.10, 13.405), (53.40, 13.405)]);
        var interval = TimeSpan.FromMilliseconds(400);
        var service = CreateService();
        var routeSearch = CreateRouteSearch(service, searchOptions: new RouteSearchOptions { MinQueryInterval = interval });
        var viewModel = await CreateRouteViewModelAsync(service, routeSearch);
        var progress = new List<string?>();
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(viewModel.RouteProgressText))
            {
                progress.Add(viewModel.RouteProgressText);
            }
        };

        await viewModel.SearchAsync();

        var times = Server.ListRequestTimes;
        Assert.Equal(3, times.Count);
        for (var i = 1; i < times.Count; i++)
        {
            Assert.True(times[i] - times[i - 1] >= interval - TimeSpan.FromMilliseconds(30), $"Abstand der Abfragen {i - 1} und {i} zu kurz: {(times[i] - times[i - 1]).TotalMilliseconds:0} ms");
        }

        Assert.Contains("Tankstellen werden gesucht (2 von 3) …", progress);
        Assert.Contains("Tankstellen werden gesucht (3 von 3) …", progress);
        Assert.Null(viewModel.RouteProgressText);
        Assert.Null(viewModel.StatusMessage);
    }

    /// <summary>
    /// Prüft, dass eine Route, die mehr als drei Abfragen bräuchte, vor der ersten Preisabfrage mit der durchsuchbaren Länge abgelehnt wird.
    /// </summary>
    [Fact]
    public async Task Search_RouteNeedingMoreThanThreeQueries_IsRejectedBeforeFirstPriceQuery()
    {
        Routing.SetRoute([(52.52, 13.405), (53.20, 13.405), (53.90, 13.405)]);
        var service = CreateService();
        var routeSearch = CreateRouteSearch(service, searchOptions: new RouteSearchOptions { MinQueryInterval = TimeSpan.Zero });
        var viewModel = await CreateRouteViewModelAsync(service, routeSearch);

        await viewModel.SearchAsync();

        Assert.Equal(0, Server.ListRequests);
        Assert.Equal(1, Routing.Requests);
        Assert.Contains("kann nur bis 131 km Länge gesucht werden", viewModel.StatusMessage, StringComparison.Ordinal);
        Assert.Contains("2 km Abstand", viewModel.StatusMessage, StringComparison.Ordinal);
        Assert.Empty(viewModel.Stations);
        Assert.False(viewModel.HasMapRoute);
        Assert.False(viewModel.IsBusy);
    }

    /// <summary>
    /// Prüft, dass ein Abbruch während der Wartezeit zwischen den Abfragen die weiteren Abfragen verhindert.
    /// </summary>
    [Fact]
    public async Task Search_CancelledWhileWaiting_SendsNoFurtherQueries()
    {
        Routing.SetRoute([(52.52, 13.405), (52.80, 13.405), (53.10, 13.405), (53.40, 13.405)]);
        var service = CreateService();
        var routeSearch = CreateRouteSearch(service, searchOptions: new RouteSearchOptions { MinQueryInterval = TimeSpan.FromSeconds(30) });
        var viewModel = await CreateRouteViewModelAsync(service, routeSearch);

        var search = viewModel.SearchAsync();
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (Server.ListRequests < 1 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }

        viewModel.CancelRouteSearchCommand.Execute(null);
        await search;

        Assert.Equal(1, Server.ListRequests);
        Assert.False(viewModel.IsBusy);
        Assert.Equal(SearchTexts.RouteCancelled, viewModel.StatusMessage);
    }
}

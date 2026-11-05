using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft den Offline-Rückfall der Umkreissuche: nach erfolgreicher Suche und beendetem Preisdienst liefert der Preis-Cache die zuletzt bekannten Preise mit Alter.
/// </summary>
public class SearchMockServerTests_Offline : SearchMockServerTestBase
{
    /// <summary>
    /// Prüft, dass nach beendetem Server die zuletzt bekannten Preise mit ihrem Alter geliefert werden.
    /// </summary>
    [Fact]
    public async Task Search_ServerGone_ReturnsCachedStationsWithAge()
    {
        await CreateService().SearchNearbyAsync(Query(5));
        var closedUrl = StopServer();
        Clock.Advance(TimeSpan.FromMinutes(75));

        var result = await CreateService(baseUrl: closedUrl).SearchNearbyAsync(Query(5));

        Assert.Equal(PriceDataSource.OfflineFallback, result.Source);
        Assert.Equal(["Alpha Tankstelle", "Beta Tankstelle"], result.Stations.Select(s => s.Name));
        var price = result.Stations[0].Prices[0];
        Assert.Equal("vor 75 Min.", PriceFreshness.FormatAge(price.RetrievedUtc, Clock.GetUtcNow().UtcDateTime));
        Assert.True(PriceFreshness.IsStale(price.RetrievedUtc, Clock.GetUtcNow().UtcDateTime));
    }

    /// <summary>
    /// Prüft, dass ohne Verbindung gar keine Anfrage gesendet wird und die Cache-Daten geliefert werden.
    /// </summary>
    [Fact]
    public async Task Search_NoConnection_UsesCacheWithoutRequest()
    {
        await CreateService().SearchNearbyAsync(Query(5));
        var requests = Server.TotalRequests;
        Clock.Advance(TimeSpan.FromMinutes(10));
        Connection.Change(false);

        var result = await CreateService().SearchNearbyAsync(Query(5));

        Assert.Equal(PriceDataSource.OfflineFallback, result.Source);
        Assert.Equal(PriceFailure.Offline, result.Failure);
        Assert.Equal(2, result.Stations.Count);
        Assert.Equal(requests, Server.TotalRequests);
    }

    /// <summary>
    /// Prüft das ViewModel: Banner, Quellenhinweis und aufbereitete Liste mit Amber-Kennzeichnung und Hinweis „Preis unbestätigt“ nach dem Ausfall.
    /// </summary>
    [Fact]
    public async Task ViewModel_ServerGone_ShowsBannerStaleAgeAndUnconfirmedHint()
    {
        await CreateService().SearchNearbyAsync(Query(5));
        var closedUrl = StopServer();
        Clock.Advance(TimeSpan.FromMinutes(90));
        var viewModel = CreateViewModel(CreateService(baseUrl: closedUrl));
        viewModel.OnAppearing();
        await viewModel.LastSettingsTask;

        await viewModel.SearchAsync();

        Assert.True(viewModel.IsOffline);
        Assert.Equal(SearchTexts.OfflineFallbackNote, viewModel.SourceNote);
        Assert.Equal(2, viewModel.Stations.Count);
        Assert.All(viewModel.Stations, item => Assert.True(item.HasUnconfirmedPrice));
        Assert.All(viewModel.Stations.SelectMany(item => item.PriceLines), line =>
        {
            Assert.True(line.IsStale);
            Assert.Equal("vor 90 Min.", line.AgeText);
        });
    }

    /// <summary>
    /// Prüft, dass ohne Cache und ohne Preisdienst eine Fehlermeldung statt des Leerzustands erscheint.
    /// </summary>
    [Fact]
    public async Task ViewModel_ServerGoneWithoutCache_ShowsFailureInsteadOfEmptyState()
    {
        var closedUrl = StopServer();
        var viewModel = CreateViewModel(CreateService(baseUrl: closedUrl));
        viewModel.OnAppearing();
        await viewModel.LastSettingsTask;

        await viewModel.SearchAsync();

        Assert.Equal("Der Preisdienst ist nicht erreichbar.", viewModel.StatusMessage);
        Assert.False(viewModel.ShowEmptyState);
        Assert.Empty(viewModel.Stations);
    }
}

using Tankradar.MAUI.Resources.Texts;
using Tankradar.TestSupport;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft das Fehlerverhalten der Routensuche mit echten Diensten gegen die Mock-Server: verständliche Meldungen, Wiederholung mit Wartezeit,
/// keine Folgeaufrufe nach einem Fehler und keine Teilergebnisse.
/// </summary>
public class RouteSearchMockServerTests_Errors : RouteSearchMockServerTestBase
{
    private async Task<MAUI.ViewModels.MapViewModel> SearchWithAsync(Action<MAUI.ViewModels.MapViewModel>? configure = null, int distanceKm = 2)
    {
        var viewModel = await CreateRouteViewModelAsync(distanceKm: distanceKm);
        configure?.Invoke(viewModel);
        await viewModel.SearchAsync();
        return viewModel;
    }

    private void AssertNoPriceQueriesAndNoResult(MAUI.ViewModels.MapViewModel viewModel)
    {
        Assert.Equal(0, Server.ListRequests);
        Assert.Empty(viewModel.Stations);
        Assert.False(viewModel.HasMapRoute);
        Assert.NotNull(viewModel.StatusMessage);
    }

    /// <summary>
    /// Prüft, dass ein dauerhaft ausgelasteter Routingdienst (429) dreimal versucht wird und die Kontingent-Meldung erscheint.
    /// </summary>
    [Fact]
    public async Task Search_RoutingTooManyRequests_RetriesThreeTimesAndReportsQuota()
    {
        Routing.EnqueueStatuses(429, 429, 429);

        var viewModel = await SearchWithAsync();

        Assert.Equal(3, Routing.Requests);
        Assert.Equal(SearchTexts.RoutingQuotaExceeded, viewModel.StatusMessage);
        AssertNoPriceQueriesAndNoResult(viewModel);
    }

    /// <summary>
    /// Prüft, dass ein einmaliger Serverfehler wiederholt wird und die Suche danach gelingt.
    /// </summary>
    [Fact]
    public async Task Search_RoutingServerErrorOnce_IsRetriedAndSucceeds()
    {
        Routing.EnqueueStatuses(503);

        var viewModel = await SearchWithAsync();

        Assert.Equal(2, Routing.Requests);
        Assert.Null(viewModel.StatusMessage);
        Assert.NotEmpty(viewModel.Stations);
    }

    /// <summary>
    /// Prüft, dass ein dauerhaft nicht erreichbarer Routingdienst gemeldet wird.
    /// </summary>
    [Fact]
    public async Task Search_RoutingServerErrors_ReportsUnreachable()
    {
        Routing.EnqueueStatuses(500, 502, 503);

        var viewModel = await SearchWithAsync();

        Assert.Equal(SearchTexts.RoutingUnreachable, viewModel.StatusMessage);
        AssertNoPriceQueriesAndNoResult(viewModel);
    }

    /// <summary>
    /// Prüft das Zeitlimit: Antwortet der Routingdienst nicht rechtzeitig, wird wiederholt und dann „nicht erreichbar“ gemeldet.
    /// </summary>
    [Fact]
    public async Task Search_RoutingTimeout_IsRetriedAndReportsUnreachable()
    {
        Routing.ResponseDelay = TimeSpan.FromSeconds(2);
        var service = CreateService();
        var routeSearch = CreateRouteSearch(service, requestTimeout: TimeSpan.FromMilliseconds(150));
        var viewModel = await CreateRouteViewModelAsync(service, routeSearch);

        await viewModel.SearchAsync();

        Assert.Equal(SearchTexts.RoutingUnreachable, viewModel.StatusMessage);
        Assert.Equal(3, Routing.Requests);
        AssertNoPriceQueriesAndNoResult(viewModel);
    }

    /// <summary>
    /// Prüft, dass ein abgelehnter Schlüssel ohne Wiederholung gemeldet wird.
    /// </summary>
    [Fact]
    public async Task Search_WrongRoutingKey_ReportsKeyRejectedWithoutRetry()
    {
        var service = CreateService();
        var viewModel = await CreateRouteViewModelAsync(service, CreateRouteSearch(service, key: "falscher-routing-schluessel"));

        await viewModel.SearchAsync();

        Assert.Equal(SearchTexts.RoutingKeyRejected, viewModel.StatusMessage);
        Assert.Equal(1, Routing.Requests);
        Assert.DoesNotContain("falscher-routing-schluessel", viewModel.StatusMessage, StringComparison.Ordinal);
        AssertNoPriceQueriesAndNoResult(viewModel);
    }

    /// <summary>
    /// Prüft, dass ein fehlender Schlüssel „nicht verfügbar“ meldet, ohne irgendeinen externen Dienst anzusprechen, und die übrige Suche weiter funktioniert.
    /// </summary>
    [Fact]
    public async Task Search_MissingRoutingKey_ReportsUnavailableWithoutAnyCallAndOtherModesStillWork()
    {
        var service = CreateService();
        var viewModel = await CreateRouteViewModelAsync(service, CreateRouteSearch(service, key: null));

        await viewModel.SearchAsync();

        Assert.Equal(SearchTexts.RoutingUnavailable, viewModel.StatusMessage);
        Assert.Equal(0, Nominatim.Requests);
        Assert.Equal(0, Routing.Requests);
        AssertNoPriceQueriesAndNoResult(viewModel);

        viewModel.ModeOptions.Single(option => option.Value == MAUI.Models.Search.SearchMode.CurrentLocation).SelectCommand.Execute(null);
        await viewModel.SearchAsync();

        Assert.NotEmpty(viewModel.Stations);
        Assert.Null(viewModel.StatusMessage);
    }

    /// <summary>
    /// Prüft die Meldung „Route nicht berechenbar“ (ORS 404, Code 2010).
    /// </summary>
    [Fact]
    public async Task Search_RouteNotFound_ReportsRouteNotCalculable()
    {
        Routing.EnqueueError(404, 2010);

        var viewModel = await SearchWithAsync();

        Assert.Equal(SearchTexts.RoutingRouteNotFound, viewModel.StatusMessage);
        Assert.Equal(1, Routing.Requests);
        AssertNoPriceQueriesAndNoResult(viewModel);
    }

    /// <summary>
    /// Prüft die Meldung bei Überschreitung der Streckengrenze des Routingdienstes (ORS 400, Code 2004).
    /// </summary>
    [Fact]
    public async Task Search_RoutingDistanceLimit_ReportsRouteTooLong()
    {
        Routing.EnqueueError(400, 2004);

        var viewModel = await SearchWithAsync();

        Assert.Equal(SearchTexts.RouteTooLongForService, viewModel.StatusMessage);
        AssertNoPriceQueriesAndNoResult(viewModel);
    }

    /// <summary>
    /// Prüft, dass eine Route über 400 km ohne Tankstellenabfrage als zu lang gemeldet wird.
    /// </summary>
    [Fact]
    public async Task Search_RouteOfSixHundredKilometers_ReportsTooLongWithoutPriceQueries()
    {
        Routing.SetRoute([(52.52, 13.405), (50.0, 13.405), (47.2, 13.405)]);

        var viewModel = await SearchWithAsync();

        Assert.Contains("kann nur bis 400 km Länge gesucht werden", viewModel.StatusMessage, StringComparison.Ordinal);
        AssertNoPriceQueriesAndNoResult(viewModel);
    }

    /// <summary>
    /// Prüft, dass ein unbekanntes Ziel gemeldet wird, ohne Route zu berechnen oder Preise abzufragen.
    /// </summary>
    [Fact]
    public async Task Search_UnknownDestination_ReportsNotFoundWithoutRouting()
    {
        var viewModel = await SearchWithAsync(vm => vm.DestinationInput = MockNominatimServer.QueryUnknown);

        Assert.Equal(SearchTexts.RouteDestinationNotFound, viewModel.StatusMessage);
        Assert.Equal(0, Routing.Requests);
        AssertNoPriceQueriesAndNoResult(viewModel);
    }

    /// <summary>
    /// Prüft, dass ein unbekannter Start gemeldet wird, ohne das Ziel aufzulösen.
    /// </summary>
    [Fact]
    public async Task Search_UnknownStart_ReportsNotFoundBeforeDestination()
    {
        var viewModel = await SearchWithAsync(vm => vm.StartInput = MockNominatimServer.QueryUnknown);

        Assert.Equal(SearchTexts.RouteStartNotFound, viewModel.StatusMessage);
        Assert.Equal(1, Nominatim.Requests);
        Assert.Equal(0, Routing.Requests);
        AssertNoPriceQueriesAndNoResult(viewModel);
    }

    /// <summary>
    /// Prüft, dass Start und Ziel am selben Ort ohne Routing-Aufruf gemeldet werden.
    /// </summary>
    [Fact]
    public async Task Search_SamePlace_ReportsWithoutRouting()
    {
        var viewModel = await SearchWithAsync(vm => vm.DestinationInput = "10115");

        Assert.Equal(SearchTexts.RouteSamePlace, viewModel.StatusMessage);
        Assert.Equal(0, Routing.Requests);
        AssertNoPriceQueriesAndNoResult(viewModel);
    }

    /// <summary>
    /// Prüft, dass ein Fehler der Ortssuche (Nominatim) verständlich gemeldet wird.
    /// </summary>
    [Fact]
    public async Task Search_GeocodingServerError_ReportsGeocodingUnavailable()
    {
        Nominatim.EnqueueStatuses(503);

        var viewModel = await SearchWithAsync();

        Assert.Equal(SearchTexts.GeocodingUnavailable, viewModel.StatusMessage);
        Assert.Equal(0, Routing.Requests);
        AssertNoPriceQueriesAndNoResult(viewModel);
    }

    /// <summary>
    /// Prüft, dass ein nicht erreichbarer Routingdienst (Server beendet) gemeldet wird.
    /// </summary>
    [Fact]
    public async Task Search_RoutingServerStopped_ReportsUnreachable()
    {
        var closed = Routing.BaseUrl;
        Routing.Dispose();
        var service = CreateService();
        var viewModel = await CreateRouteViewModelAsync(service, CreateRouteSearch(service, routingUrl: closed));

        await viewModel.SearchAsync();

        Assert.Equal(SearchTexts.RoutingUnreachable, viewModel.StatusMessage);
        AssertNoPriceQueriesAndNoResult(viewModel);
    }

    /// <summary>
    /// Prüft, dass ein Fehler des Preisdienstes ohne Teilergebnisse gemeldet wird.
    /// </summary>
    [Fact]
    public async Task Search_PriceServiceError_ReportsFailureWithoutPartialResults()
    {
        Server.EnqueueStatuses(503, 503, 503);

        var viewModel = await SearchWithAsync();

        Assert.Equal(SearchTexts.GetFailureMessage(MAUI.Models.Pricing.PriceFailure.Unreachable, false), viewModel.StatusMessage);
        Assert.Empty(viewModel.Stations);
        Assert.False(viewModel.HasMapRoute);
    }

    /// <summary>
    /// Prüft, dass ungültige Eingaben vor jedem externen Aufruf abgelehnt werden (kein einziger Zugriff auf einen Mock-Server).
    /// </summary>
    [Fact]
    public async Task Search_InvalidInput_MakesNoExternalCalls()
    {
        var viewModel = await SearchWithAsync(vm => vm.DestinationInput = "ab");

        Assert.NotNull(viewModel.StatusMessage);
        Assert.Equal(0, Nominatim.Requests);
        Assert.Equal(0, Routing.Requests);
        Assert.Equal(0, Server.TotalRequests);
    }

    /// <summary>
    /// Prüft, dass ohne Verbindung die Meldung erscheint und kein externer Dienst angesprochen wird.
    /// </summary>
    [Fact]
    public async Task Search_Offline_ReportsWithoutExternalCalls()
    {
        var viewModel = await CreateRouteViewModelAsync();
        Connection.Change(false);

        await viewModel.SearchAsync();

        Assert.Equal(SearchTexts.RouteOffline, viewModel.StatusMessage);
        Assert.Equal(0, Nominatim.Requests);
        Assert.Equal(0, Routing.Requests);
        Assert.Equal(0, Server.TotalRequests);
    }
}

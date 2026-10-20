using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Geocoding;
using Tankradar.MAUI.Services.Routing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft jeden Fehlerausgang der Routensuche: der richtige <see cref="RouteSearchStatus"/> und keine Folgeaufrufe nach einem Fehler.
/// </summary>
public class RouteSearchServiceTests_Failures : RouteSearchServiceTestBase
{
    /// <summary>
    /// Prüft, dass ein nicht verfügbarer Routingdienst vor allen weiteren Aufrufen gemeldet wird.
    /// </summary>
    [Fact]
    public async Task SearchAsync_RoutingUnavailable_MakesNoFurtherCalls()
    {
        Routing.Available = false;

        var result = await SearchAsync();

        Assert.Equal(RouteSearchStatus.RoutingUnavailable, result.Status);
        Assert.Empty(Geocoding.Inputs);
        Assert.Equal(0, Routing.Calls);
        Assert.Empty(Prices.Queries);
    }

    /// <summary>
    /// Prüft, dass ein unbekannter Start gemeldet wird, ohne das Ziel aufzulösen.
    /// </summary>
    [Fact]
    public async Task SearchAsync_StartNotFound_StopsBeforeDestination()
    {
        Geocoding.Results.Remove(StartText);

        var result = await SearchAsync();

        Assert.Equal(RouteSearchStatus.StartNotFound, result.Status);
        Assert.Equal([StartText], Geocoding.Inputs);
        Assert.Equal(0, Routing.Calls);
        Assert.Empty(Prices.Queries);
    }

    /// <summary>
    /// Prüft, dass ein unbekanntes Ziel gemeldet wird, ohne die Route zu berechnen.
    /// </summary>
    [Fact]
    public async Task SearchAsync_DestinationNotFound_StopsBeforeRouting()
    {
        Geocoding.Results.Remove(DestinationText);

        var result = await SearchAsync();

        Assert.Equal(RouteSearchStatus.DestinationNotFound, result.Status);
        Assert.Equal(0, Routing.Calls);
        Assert.Empty(Prices.Queries);
    }

    /// <summary>
    /// Prüft, dass andere Fehler der Ortsauflösung als Fehlschlag der Auflösung mit ihrem Grund gemeldet werden.
    /// </summary>
    /// <param name="status">Der Fehlschlag der Auflösung.</param>
    [Theory]
    [InlineData(GeocodingStatus.Unavailable)]
    [InlineData(GeocodingStatus.Rejected)]
    [InlineData(GeocodingStatus.InvalidResponse)]
    [InlineData(GeocodingStatus.EndpointNotConfigured)]
    [InlineData(GeocodingStatus.InvalidInput)]
    public async Task SearchAsync_GeocodingFailure_ReportsStatus(GeocodingStatus status)
    {
        Geocoding.Results[DestinationText] = GeocodingResult.Failure(status);

        var result = await SearchAsync();

        Assert.Equal(RouteSearchStatus.GeocodingFailed, result.Status);
        Assert.Equal(status, result.GeocodingStatus);
        Assert.Equal(0, Routing.Calls);
    }

    /// <summary>
    /// Prüft, dass Start und Ziel am selben Ort ohne Routing-Aufruf gemeldet werden.
    /// </summary>
    [Fact]
    public async Task SearchAsync_SamePlace_MakesNoRoutingCall()
    {
        Geocoding.Results[DestinationText] = GeocodingResult.Success(new GeoPosition(52.5205, 13.405), "Nebenan");

        var result = await SearchAsync();

        Assert.Equal(RouteSearchStatus.SamePlace, result.Status);
        Assert.Equal(0, Routing.Calls);
        Assert.Empty(Prices.Queries);
    }

    /// <summary>
    /// Prüft, dass knapp über 200 m Abstand nicht mehr als derselbe Ort gelten.
    /// </summary>
    [Fact]
    public async Task SearchAsync_JustOver200Meters_IsNotSamePlace()
    {
        Geocoding.Results[DestinationText] = GeocodingResult.Success(new GeoPosition(52.52 + (0.25 / 111.195), 13.405), "Fast nebenan");

        var result = await SearchAsync();

        Assert.Equal(RouteSearchStatus.Success, result.Status);
    }

    /// <summary>
    /// Prüft, dass ein Fehler des Routingdienstes mit seinem Grund gemeldet wird, ohne Tankstellen abzufragen.
    /// </summary>
    /// <param name="failure">Der Fehlergrund.</param>
    [Theory]
    [InlineData(RoutingFailure.Unreachable)]
    [InlineData(RoutingFailure.QuotaExceeded)]
    [InlineData(RoutingFailure.RouteNotFound)]
    [InlineData(RoutingFailure.KeyRejected)]
    [InlineData(RoutingFailure.Rejected)]
    [InlineData(RoutingFailure.InvalidResponse)]
    public async Task SearchAsync_RoutingFailure_ReportsReasonWithoutPriceQueries(RoutingFailure failure)
    {
        Routing.Result = RoutingResult.Fail(failure);

        var result = await SearchAsync();

        Assert.Equal(RouteSearchStatus.RoutingFailed, result.Status);
        Assert.Equal(failure, result.RoutingFailure);
        Assert.Empty(Prices.Queries);
    }

    /// <summary>
    /// Prüft, dass die Streckenbegrenzung des Routingdienstes als zu lange Route gemeldet wird.
    /// </summary>
    [Fact]
    public async Task SearchAsync_RoutingReportsTooLong_ReportsRouteTooLong()
    {
        Routing.Result = RoutingResult.Fail(RoutingFailure.RouteTooLong);

        var result = await SearchAsync();

        Assert.Equal(RouteSearchStatus.RouteTooLong, result.Status);
        Assert.Empty(Prices.Queries);
    }

    /// <summary>
    /// Prüft, dass eine Route über 400 km ohne Tankstellenabfrage als zu lang gemeldet wird.
    /// </summary>
    [Fact]
    public async Task SearchAsync_RouteLongerThanMaximum_ReportsRouteTooLongWithoutQueries()
    {
        Routing.Result = RoutingResult.Success(RouteTestData.NorthFromBerlin(450 / 111.195));

        var result = await SearchAsync();

        Assert.Equal(RouteSearchStatus.RouteTooLong, result.Status);
        Assert.Empty(Prices.Queries);
    }

    /// <summary>
    /// Prüft, dass eine Route, die mehr Abfragen als erlaubt bräuchte, als zu lang gemeldet wird.
    /// </summary>
    [Fact]
    public async Task SearchAsync_MoreQueriesThanAllowed_ReportsRouteTooLong()
    {
        Routing.Result = RoutingResult.Success(RouteTestData.NorthFromBerlin(150 / 111.195));
        Service = new RouteSearchService(Geocoding, Routing, new RouteStationSearchService(Prices), new RouteSearchOptions { MaxQueries = 2 });

        var result = await SearchAsync();

        Assert.Equal(RouteSearchStatus.RouteTooLong, result.Status);
        Assert.Empty(Prices.Queries);
    }

    /// <summary>
    /// Prüft, dass ein Fehler des Preisdienstes ohne Teilergebnisse gemeldet wird.
    /// </summary>
    [Fact]
    public async Task SearchAsync_PriceFailure_ReportsReasonWithoutStations()
    {
        Prices.Respond = (_, _) => new StationSearchResult([], PriceDataSource.OfflineFallback, PriceFailure.ApiKeyMissing);

        var result = await SearchAsync();

        Assert.Equal(RouteSearchStatus.PriceFailed, result.Status);
        Assert.Equal(PriceFailure.ApiKeyMissing, result.PriceFailure);
        Assert.Empty(result.Stations);
    }

    /// <summary>
    /// Prüft, dass das Abbruchsignal bis zum Aufrufer durchgereicht wird.
    /// </summary>
    [Fact]
    public async Task SearchAsync_Cancelled_Throws()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Service.SearchAsync(Request(), null, cancellation.Token));
    }

    /// <summary>
    /// Prüft, dass ein Fehlschlag keinen Erfolgsstatus tragen darf.
    /// </summary>
    [Fact]
    public void Fail_WithSuccess_Throws()
    {
        Assert.Throws<ArgumentException>(() => RouteSearchResult.Fail(RouteSearchStatus.Success));
        Assert.Throws<ArgumentException>(() => RoutingResult.Fail(RoutingFailure.None));
    }
}

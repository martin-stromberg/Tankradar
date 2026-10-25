using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Geocoding;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die verständlichen Meldungen bei Fehlern der Routensuche und dass vorherige Ergebnisse dabei entfernt werden.
/// </summary>
public class MapViewModelTests_RouteFailures : MapViewModelRouteTestBase
{
    /// <summary>
    /// Prüft für jeden Fehlerausgang die Meldung, das Entfernen früherer Ergebnisse und dass kein Rohtext des Dienstes erscheint.
    /// </summary>
    /// <param name="status">Der Ausgang der Suche.</param>
    /// <param name="expected">Die erwartete Meldung.</param>
    [Theory]
    [InlineData(RouteSearchStatus.StartNotFound, SearchTexts.RouteStartNotFound)]
    [InlineData(RouteSearchStatus.DestinationNotFound, SearchTexts.RouteDestinationNotFound)]
    [InlineData(RouteSearchStatus.SamePlace, SearchTexts.RouteSamePlace)]
    [InlineData(RouteSearchStatus.RoutingUnavailable, SearchTexts.RoutingUnavailable)]
    public async Task Search_SimpleFailureStatus_ShowsMessageAndClearsResult(RouteSearchStatus status, string expected)
    {
        await PrepareRouteAsync();
        RouteSearch.Result = RouteSuccess("A", "B", RouteStationAt(1, "Erste", 4.2, 0.8, 1.90m, 1.70m));
        await SearchAsync();
        Assert.True(ViewModel.HasMapRoute);

        RouteSearch.Result = RouteSearchResult.Fail(status);
        await SearchAsync();

        Assert.Equal(expected, ViewModel.StatusMessage);
        Assert.Empty(ViewModel.Stations);
        Assert.False(ViewModel.HasMapRoute);
        Assert.Null(ViewModel.RouteSummary);
        Assert.Null(ViewModel.Origin);
        Assert.False(ViewModel.IsBusy);
    }

    /// <summary>
    /// Prüft die Meldung bei zu langer Route mit der Angabe der Grenze.
    /// </summary>
    [Fact]
    public async Task Search_RouteTooLong_ShowsLimitInMessage()
    {
        await PrepareRouteAsync();
        RouteSearch.Result = RouteSearchResult.FailLimit(new RouteLimit(2, 300, 131.1));

        await SearchAsync();

        Assert.Contains("131 km", ViewModel.StatusMessage, StringComparison.Ordinal);
        Assert.Contains("2 km Abstand", ViewModel.StatusMessage, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft die Meldungen der Fehler der Ortsauflösung.
    /// </summary>
    /// <param name="status">Der Grund.</param>
    [Theory]
    [InlineData(GeocodingStatus.Unavailable)]
    [InlineData(GeocodingStatus.Rejected)]
    [InlineData(GeocodingStatus.InvalidResponse)]
    [InlineData(GeocodingStatus.EndpointNotConfigured)]
    public async Task Search_GeocodingFailure_ShowsGeocodingMessage(GeocodingStatus status)
    {
        await PrepareRouteAsync();
        RouteSearch.Result = RouteSearchResult.FailGeocoding(status);

        await SearchAsync();

        Assert.Equal(SearchTexts.GetGeocodingMessage(status, isOnline: true), ViewModel.StatusMessage);
    }

    /// <summary>
    /// Prüft die Meldungen der Fehler des Routingdienstes.
    /// </summary>
    /// <param name="failure">Der Grund.</param>
    [Theory]
    [InlineData(RoutingFailure.ApiKeyMissing)]
    [InlineData(RoutingFailure.EndpointNotConfigured)]
    [InlineData(RoutingFailure.KeyRejected)]
    [InlineData(RoutingFailure.Unreachable)]
    [InlineData(RoutingFailure.QuotaExceeded)]
    [InlineData(RoutingFailure.RouteNotFound)]
    [InlineData(RoutingFailure.Rejected)]
    [InlineData(RoutingFailure.InvalidResponse)]
    public async Task Search_RoutingFailure_ShowsRoutingMessage(RoutingFailure failure)
    {
        await PrepareRouteAsync();
        RouteSearch.Result = RouteSearchResult.FailRouting(failure);

        await SearchAsync();

        Assert.Equal(SearchTexts.GetRoutingMessage(failure), ViewModel.StatusMessage);
        Assert.DoesNotContain(failure.ToString(), ViewModel.StatusMessage!, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft die Meldung bei Fehler des Preisdienstes.
    /// </summary>
    [Fact]
    public async Task Search_PriceFailure_ShowsPriceMessage()
    {
        await PrepareRouteAsync();
        RouteSearch.Result = RouteSearchResult.FailPrice(PriceFailure.Unreachable);

        await SearchAsync();

        Assert.Equal(SearchTexts.GetFailureMessage(PriceFailure.Unreachable, hasStations: false), ViewModel.StatusMessage);
    }

    /// <summary>
    /// Prüft, dass eine unerwartete Ausnahme zur allgemeinen Meldung führt und nur der Typ protokolliert wird.
    /// </summary>
    [Fact]
    public async Task Search_UnexpectedException_ShowsGenericMessageAndLogsOnlyType()
    {
        await PrepareRouteAsync();
        RouteSearch.Exception = new InvalidOperationException($"Fehler bei {StartText} nach {DestinationText}");

        await SearchAsync();

        Assert.Equal(SearchTexts.SearchFailed, ViewModel.StatusMessage);
        Assert.DoesNotContain(StartText, Logger.AllText, StringComparison.Ordinal);
        Assert.DoesNotContain(DestinationText, Logger.AllText, StringComparison.Ordinal);
        Assert.Contains("InvalidOperationException", Logger.AllText, StringComparison.Ordinal);
        Assert.False(ViewModel.IsBusy);
    }

    /// <summary>
    /// Prüft, dass nach einem Fehler eine neue Suche wieder gelingt.
    /// </summary>
    [Fact]
    public async Task Search_AfterFailure_NewSearchSucceeds()
    {
        await PrepareRouteAsync();
        RouteSearch.Result = RouteSearchResult.FailRouting(RoutingFailure.Unreachable);
        await SearchAsync();
        Assert.NotNull(ViewModel.StatusMessage);

        RouteSearch.Result = RouteSuccess("A", "B", RouteStationAt(1, "Erste", 4.2, 0.8, 1.90m, 1.70m));
        await SearchAsync();

        Assert.Null(ViewModel.StatusMessage);
        Assert.True(ViewModel.HasMapRoute);
        Assert.Single(ViewModel.Stations);
    }

    /// <summary>
    /// Prüft die Meldungen als Bündel: jeder Fehlergrund hat einen nicht leeren, eigenen Text.
    /// </summary>
    [Fact]
    public void Messages_AreNotEmptyAndDistinctPerReason()
    {
        var routing = Enum.GetValues<RoutingFailure>().Where(f => f is not (RoutingFailure.None or RoutingFailure.ApiKeyMissing or RoutingFailure.EndpointNotConfigured)).ToList();

        Assert.All(routing, failure => Assert.NotEmpty(SearchTexts.GetRoutingMessage(failure)));
        Assert.Equal(routing.Count, routing.Select(SearchTexts.GetRoutingMessage).Distinct().Count());
        Assert.Equal(SearchTexts.RoutingUnavailable, SearchTexts.GetRoutingMessage(RoutingFailure.ApiKeyMissing));
    }
}

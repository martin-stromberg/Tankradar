using System.Net;
using Tankradar.MAUI.Models.Routing;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Abbildung der Fehlerantworten des Routingdienstes auf <see cref="RoutingFailure"/>.
/// </summary>
public class OpenRouteServiceClientTests_Errors : OpenRouteServiceClientTestBase
{
    /// <summary>
    /// Prüft die Abbildung der Clientfehler ohne Wiederholung.
    /// </summary>
    /// <param name="status">Der Statuscode.</param>
    /// <param name="body">Der Antwortkörper.</param>
    /// <param name="expected">Der erwartete Fehlergrund.</param>
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "{}", RoutingFailure.KeyRejected)]
    [InlineData(HttpStatusCode.Forbidden, "{\"error\":\"Access to this API has been disallowed\"}", RoutingFailure.KeyRejected)]
    [InlineData(HttpStatusCode.NotFound, "{\"error\":{\"code\":2010,\"message\":\"Could not find routable point\"}}", RoutingFailure.RouteNotFound)]
    [InlineData(HttpStatusCode.NotFound, "{\"error\":{\"code\":2009,\"message\":\"Route could not be found\"}}", RoutingFailure.RouteNotFound)]
    [InlineData(HttpStatusCode.NotFound, "kein json", RoutingFailure.RouteNotFound)]
    [InlineData(HttpStatusCode.BadRequest, "{\"error\":{\"code\":2004,\"message\":\"Request parameters exceed the server configuration limits\"}}", RoutingFailure.RouteTooLong)]
    [InlineData(HttpStatusCode.BadRequest, "{\"error\":{\"code\":2003,\"message\":\"x\"}}", RoutingFailure.Rejected)]
    [InlineData(HttpStatusCode.BadRequest, "{}", RoutingFailure.Rejected)]
    [InlineData(HttpStatusCode.RequestEntityTooLarge, "{}", RoutingFailure.Rejected)]
    public async Task GetRouteAsync_ClientError_MapsWithoutRetry(HttpStatusCode status, string body, RoutingFailure expected)
    {
        Handler.RespondWith(status, body);

        var result = await CreateClient().GetRouteAsync(Start, Destination);

        Assert.Equal(expected, result.Failure);
        Assert.Null(result.Route);
        Assert.Single(Handler.Requests);
        Assert.Empty(Delay.Delays);
    }

    /// <summary>
    /// Prüft, dass ein dauerhafter Serverfehler nach den Versuchen als nicht erreichbar gemeldet wird.
    /// </summary>
    [Fact]
    public async Task GetRouteAsync_PersistentServerError_ReturnsUnreachable()
    {
        Handler.RespondWith(HttpStatusCode.ServiceUnavailable, "{}");

        var result = await CreateClient().GetRouteAsync(Start, Destination);

        Assert.Equal(RoutingFailure.Unreachable, result.Failure);
        Assert.Equal(3, Handler.Requests.Count);
    }

    /// <summary>
    /// Prüft, dass ein dauerhafter Verbindungsfehler als nicht erreichbar gemeldet wird.
    /// </summary>
    [Fact]
    public async Task GetRouteAsync_ConnectionFailure_ReturnsUnreachable()
    {
        Handler.Throw(new HttpRequestException("kein Netz"));

        var result = await CreateClient().GetRouteAsync(Start, Destination);

        Assert.Equal(RoutingFailure.Unreachable, result.Failure);
        Assert.Equal(3, Handler.Requests.Count);
    }

    /// <summary>
    /// Prüft, dass ein dauerhaft ausgelasteter Dienst (429) als Kontingent erschöpft gemeldet wird.
    /// </summary>
    [Fact]
    public async Task GetRouteAsync_PersistentTooManyRequests_ReturnsQuotaExceeded()
    {
        Handler.RespondWith(HttpStatusCode.TooManyRequests, "{\"error\":{\"code\":2003}}");

        var result = await CreateClient().GetRouteAsync(Start, Destination);

        Assert.Equal(RoutingFailure.QuotaExceeded, result.Failure);
        Assert.Equal(3, Handler.Requests.Count);
    }

    /// <summary>
    /// Prüft, dass eine Abbruchanforderung des Aufrufers als Abbruch und nicht als Fehlergrund ankommt.
    /// </summary>
    [Fact]
    public async Task GetRouteAsync_CancelledByCaller_ThrowsOperationCanceled()
    {
        Handler.Hang();
        using var cancellation = new CancellationTokenSource();
        var task = CreateClient().GetRouteAsync(Start, Destination, cancellation.Token);
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }
}

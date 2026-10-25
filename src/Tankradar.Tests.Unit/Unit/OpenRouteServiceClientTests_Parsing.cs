using System.Net;
using Tankradar.MAUI.Models.Routing;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Auswertung der Antwort des Routingdienstes.
/// </summary>
public class OpenRouteServiceClientTests_Parsing : OpenRouteServiceClientTestBase
{
    /// <summary>
    /// Prüft, dass eine gültige Antwort zu einer Route mit Länge aus den Punkten führt.
    /// </summary>
    [Fact]
    public async Task GetRouteAsync_ValidResponse_ReturnsRoute()
    {
        Handler.RespondWith(HttpStatusCode.OK, RouteBody());

        var result = await CreateClient().GetRouteAsync(Start, Destination);

        Assert.Equal(RoutingFailure.None, result.Failure);
        Assert.NotNull(result.Route);
        Assert.Equal(3, result.Route.Points.Count);
        Assert.InRange(result.Route.LengthKm, 39.9, 40.3);
    }

    /// <summary>
    /// Prüft, dass der Aufruf an den Pfad des Profils Auto per POST geht.
    /// </summary>
    [Fact]
    public async Task GetRouteAsync_SendsPostToDrivingCarProfile()
    {
        Handler.RespondWith(HttpStatusCode.OK, RouteBody());

        await CreateClient().GetRouteAsync(Start, Destination);

        Assert.Equal("https://routing.example.test/v2/directions/driving-car", Handler.Requests.Single().ToString());
    }

    /// <summary>
    /// Prüft, dass unbrauchbare Antworten als ungültige Antwort gemeldet werden.
    /// </summary>
    /// <param name="body">Der Antwortkörper.</param>
    [Theory]
    [InlineData("kein json")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"routes\":[]}")]
    [InlineData("{\"routes\":[{}]}")]
    [InlineData("{\"routes\":[{\"geometry\":5}]}")]
    [InlineData("{\"routes\":[{\"geometry\":\"\"}]}")]
    [InlineData("{\"routes\":[{\"geometry\":\"_p~iF~ps|U\"}]}")]
    public async Task GetRouteAsync_UnusableBody_ReturnsInvalidResponse(string body)
    {
        Handler.RespondWith(HttpStatusCode.OK, body);

        var result = await CreateClient().GetRouteAsync(Start, Destination);

        Assert.Equal(RoutingFailure.InvalidResponse, result.Failure);
        Assert.Null(result.Route);
        Assert.Single(Handler.Requests);
    }

    /// <summary>
    /// Prüft, dass eine Route mit Länge 0 (alle Punkte gleich) abgelehnt wird.
    /// </summary>
    [Fact]
    public async Task GetRouteAsync_ZeroLengthRoute_ReturnsInvalidResponse()
    {
        Handler.RespondWith(HttpStatusCode.OK, RouteBody((52.52, 13.405), (52.52, 13.405)));

        var result = await CreateClient().GetRouteAsync(Start, Destination);

        Assert.Equal(RoutingFailure.InvalidResponse, result.Failure);
    }
}

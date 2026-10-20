using System.Net;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Services.Routing;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Wiederholung mit zunehmender Wartezeit, Zeitlimit und Drosselung des Routing-Clients.
/// </summary>
public class OpenRouteServiceClientTests_Retry : OpenRouteServiceClientTestBase
{
    /// <summary>
    /// Prüft, dass Serverfehler mit verdoppelter Wartezeit (1 s, 2 s) wiederholt werden und der dritte Versuch gelingt.
    /// </summary>
    [Fact]
    public async Task GetRouteAsync_ServerErrors_AreRetriedWithIncreasingDelay()
    {
        Handler.RespondWith(HttpStatusCode.ServiceUnavailable, "{}")
            .RespondWith(HttpStatusCode.BadGateway, "{}")
            .RespondWith(HttpStatusCode.OK, RouteBody());

        var result = await CreateClient().GetRouteAsync(Start, Destination);

        Assert.Equal(RoutingFailure.None, result.Failure);
        Assert.Equal(3, Handler.Requests.Count);
        Assert.Equal([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)], Delay.Delays);
    }

    /// <summary>
    /// Prüft, dass zu viele Anfragen (429) wiederholt werden.
    /// </summary>
    [Fact]
    public async Task GetRouteAsync_TooManyRequests_IsRetried()
    {
        Handler.RespondWith(HttpStatusCode.TooManyRequests, "{}").RespondWith(HttpStatusCode.OK, RouteBody());

        var result = await CreateClient().GetRouteAsync(Start, Destination);

        Assert.Equal(RoutingFailure.None, result.Failure);
        Assert.Equal(2, Handler.Requests.Count);
    }

    /// <summary>
    /// Prüft das Zeitlimit: Eine nicht antwortende Gegenstelle führt nach den Versuchen zu „nicht erreichbar“.
    /// </summary>
    [Fact]
    public async Task GetRouteAsync_NoResponse_TimesOutAndReportsUnreachable()
    {
        Handler.Hang();
        var options = new RoutingOptions
        {
            BaseUrl = new Uri("https://routing.example.test/"),
            RequestTimeout = TimeSpan.FromMilliseconds(30),
            AllowLoopbackHttp = true,
            MinRequestInterval = TimeSpan.Zero,
        };

        var result = await CreateClient(options).GetRouteAsync(Start, Destination);

        Assert.Equal(RoutingFailure.Unreachable, result.Failure);
        Assert.Equal(3, Handler.Requests.Count);
        Assert.Equal(2, Delay.Delays.Count);
    }

    /// <summary>
    /// Prüft, dass die Zahl der Versuche der Einstellung folgt.
    /// </summary>
    [Fact]
    public async Task GetRouteAsync_MaxAttemptsOne_DoesNotRetry()
    {
        Handler.RespondWith(HttpStatusCode.BadGateway, "{}");
        var options = new RoutingOptions { BaseUrl = new Uri("https://routing.example.test/"), MaxAttempts = 1, AllowLoopbackHttp = true, MinRequestInterval = TimeSpan.Zero };

        var result = await CreateClient(options).GetRouteAsync(Start, Destination);

        Assert.Equal(RoutingFailure.Unreachable, result.Failure);
        Assert.Single(Handler.Requests);
        Assert.Empty(Delay.Delays);
    }

    /// <summary>
    /// Prüft die Drosselung: Zwei Aufrufe unmittelbar nacheinander werden um den Mindestabstand verzögert.
    /// </summary>
    [Fact]
    public async Task GetRouteAsync_TwoCalls_AreSpacedByMinimumInterval()
    {
        Handler.RespondWith(HttpStatusCode.OK, RouteBody());
        var client = CreateClient(new RoutingOptions { BaseUrl = new Uri("https://routing.example.test/"), MinRequestInterval = TimeSpan.FromSeconds(1) });

        await client.GetRouteAsync(Start, Destination);
        await client.GetRouteAsync(Start, Destination);

        Assert.Equal([TimeSpan.FromSeconds(1)], Delay.Delays);
    }
}

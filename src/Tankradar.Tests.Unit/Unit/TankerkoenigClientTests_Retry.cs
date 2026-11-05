using System.Net;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Pricing;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Wiederholungsstrategie, Zeitlimit und Fehlerabbildung des Tankerkönig-Clients.
/// </summary>
public class TankerkoenigClientTests_Retry : TankerkoenigClientTestBase
{
    /// <summary>
    /// Prüft, dass Serverfehler mit verdoppelter Wartezeit wiederholt werden und der dritte Versuch gelingt.
    /// </summary>
    [Fact]
    public async Task SearchAsync_ServerErrors_AreRetriedWithIncreasingDelay()
    {
        Handler.RespondWith(HttpStatusCode.ServiceUnavailable, "{}")
            .RespondWith(HttpStatusCode.BadGateway, "{}")
            .RespondWith(HttpStatusCode.OK, ListBody);

        var stations = await CreateClient().SearchAsync(52.52, 13.405, 5, CancellationToken.None);

        Assert.Equal(2, stations.Count);
        Assert.Equal(3, Handler.Requests.Count);
        Assert.Equal([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)], Delay.Delays);
    }

    /// <summary>
    /// Prüft, dass nach allen Versuchen „nicht erreichbar“ gemeldet wird.
    /// </summary>
    [Fact]
    public async Task SearchAsync_AlwaysFailing_ThrowsUnreachableAfterMaxAttempts()
    {
        Handler.Throw(new HttpRequestException("kein Netz"));

        var ex = await Assert.ThrowsAsync<PriceApiException>(() => CreateClient().SearchAsync(52.52, 13.405, 5, CancellationToken.None));

        Assert.Equal(PriceFailure.Unreachable, ex.Failure);
        Assert.Equal(3, Handler.Requests.Count);
        Assert.Equal(2, Delay.Delays.Count);
    }

    /// <summary>
    /// Prüft, dass Clientfehler (außer 429) sofort und ohne Wiederholung als abgelehnt gemeldet werden.
    /// </summary>
    /// <param name="status">Der Statuscode.</param>
    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task SearchAsync_ClientErrors_AreNotRetried(HttpStatusCode status)
    {
        Handler.RespondWith(status, "{}");

        var ex = await Assert.ThrowsAsync<PriceApiException>(() => CreateClient().SearchAsync(52.52, 13.405, 5, CancellationToken.None));

        Assert.Equal(PriceFailure.Rejected, ex.Failure);
        Assert.Single(Handler.Requests);
        Assert.Empty(Delay.Delays);
    }

    /// <summary>
    /// Prüft, dass zu viele Anfragen (429) wiederholt werden.
    /// </summary>
    [Fact]
    public async Task SearchAsync_TooManyRequests_IsRetried()
    {
        Handler.RespondWith(HttpStatusCode.TooManyRequests, "{}").RespondWith(HttpStatusCode.OK, ListBody);

        await CreateClient().SearchAsync(52.52, 13.405, 5, CancellationToken.None);

        Assert.Equal(2, Handler.Requests.Count);
    }

    /// <summary>
    /// Prüft das Zeitlimit: Eine nicht antwortende Gegenstelle führt nach den Versuchen zu „nicht erreichbar“.
    /// </summary>
    [Fact]
    public async Task SearchAsync_NoResponse_TimesOutAndReportsUnreachable()
    {
        Handler.Hang();
        var options = new PriceApiOptions
        {
            BaseUrl = new Uri("https://prices.example.test/json/"),
            RequestTimeout = TimeSpan.FromMilliseconds(40),
            MaxAttempts = 2,
            MinRequestInterval = TimeSpan.Zero,
        };

        var ex = await Assert.ThrowsAsync<PriceApiException>(() => CreateClient(options).SearchAsync(52.52, 13.405, 5, CancellationToken.None));

        Assert.Equal(PriceFailure.Unreachable, ex.Failure);
        Assert.IsType<TimeoutException>(ex.InnerException);
        Assert.Equal(2, Handler.Requests.Count);
    }

    /// <summary>
    /// Prüft, dass ein Abbruch durch den Aufrufer nicht als Netzfehler wiederholt wird.
    /// </summary>
    [Fact]
    public async Task SearchAsync_CallerCancellation_Propagates()
    {
        Handler.Hang();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(40));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateClient().SearchAsync(52.52, 13.405, 5, cts.Token));

        Assert.Single(Handler.Requests);
    }

    /// <summary>
    /// Prüft, dass ohne API-Schlüssel kein Abruf erfolgt.
    /// </summary>
    [Fact]
    public async Task SearchAsync_WithoutKey_FailsWithoutRequest()
    {
        Keys.Value = null;

        var ex = await Assert.ThrowsAsync<PriceApiException>(() => CreateClient().SearchAsync(52.52, 13.405, 5, CancellationToken.None));

        Assert.Equal(PriceFailure.ApiKeyMissing, ex.Failure);
        Assert.Empty(Handler.Requests);
    }

    /// <summary>
    /// Prüft, dass weder Meldung noch Ausnahmetext den Schlüssel enthalten.
    /// </summary>
    [Fact]
    public async Task Failures_NeverContainTheKey()
    {
        Keys.Value = "geheimer-wert-123";
        Handler.RespondWith(HttpStatusCode.Unauthorized, "{}");

        var ex = await Assert.ThrowsAsync<PriceApiException>(() => CreateClient().SearchAsync(52.52, 13.405, 5, CancellationToken.None));

        Assert.DoesNotContain("geheimer-wert-123", ex.ToString());
    }

    /// <summary>
    /// Prüft, dass auch Netzwerkfehler, deren Meldung die Anfrageadresse samt Schlüssel enthält, den Schlüssel nicht weitergeben.
    /// </summary>
    [Fact]
    public async Task NetworkErrors_NeverLeakUrlOrKey()
    {
        Keys.Value = "geheimer-wert-456";
        Handler.Throw(new HttpRequestException("Fehler bei https://prices.example.test/json/list.php?" + "apikey" + "=geheimer-wert-456"));

        var ex = await Assert.ThrowsAsync<PriceApiException>(() => CreateClient().SearchAsync(52.52, 13.405, 5, CancellationToken.None));

        Assert.DoesNotContain("geheimer-wert-456", ex.ToString());
        Assert.DoesNotContain("apikey", ex.ToString());
    }

    /// <summary>
    /// Prüft, dass unsichere Einstellungen den Client nicht entstehen lassen.
    /// </summary>
    [Fact]
    public void Constructor_HttpOptions_Throws()
    {
        var options = new PriceApiOptions { BaseUrl = new Uri("http://prices.example.test/json/") };

        Assert.Throws<InvalidOperationException>(() => CreateClient(options));
    }
}

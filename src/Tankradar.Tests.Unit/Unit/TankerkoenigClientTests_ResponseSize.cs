using System.Net;
using System.Text;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Pricing;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Größenbegrenzung der Antworten des Tankerkönig-Clients: Ablehnung übergroßer Antworten ohne Wiederholung (mit und ohne Längenangabe)
/// sowie die Einstufung eines Körperabbruchs beim Lesen als wiederholbaren Verbindungsfehler.
/// </summary>
public class TankerkoenigClientTests_ResponseSize : TankerkoenigClientTestBase
{
    private const int Limit = 1024;
    private const string StationId = "11111111-1111-4111-8111-111111111111";

    private static PriceApiOptions LimitedOptions()
    {
        return new PriceApiOptions
        {
            BaseUrl = new Uri("https://prices.example.test/json/"),
            MinRequestInterval = TimeSpan.Zero,
            MaxResponseBytes = Limit,
        };
    }

    private static byte[] OversizedBody()
    {
        return Encoding.UTF8.GetBytes(ListBody + new string(' ', Limit));
    }

    /// <summary>
    /// Prüft, dass eine Antwort über dem Limit als unerwartete Antwort ohne Wiederholung abgelehnt wird.
    /// </summary>
    [Fact]
    public async Task SearchAsync_OversizedResponse_ThrowsInvalidResponseWithoutRetry()
    {
        Handler.RespondWithBytes(HttpStatusCode.OK, OversizedBody());

        var ex = await Assert.ThrowsAsync<PriceApiException>(() => CreateClient(LimitedOptions()).SearchAsync(52.52, 13.405, 5, CancellationToken.None));

        Assert.Equal(PriceFailure.InvalidResponse, ex.Failure);
        Assert.Single(Handler.Requests);
        Assert.Empty(Delay.Delays);
    }

    /// <summary>
    /// Prüft, dass das Limit auch ohne <c>Content-Length</c>-Angabe beim Lesen des Stroms greift.
    /// </summary>
    [Fact]
    public async Task SearchAsync_OversizedWithoutContentLength_ThrowsInvalidResponse()
    {
        Handler.RespondWithoutContentLength(HttpStatusCode.OK, OversizedBody());

        var ex = await Assert.ThrowsAsync<PriceApiException>(() => CreateClient(LimitedOptions()).SearchAsync(52.52, 13.405, 5, CancellationToken.None));

        Assert.Equal(PriceFailure.InvalidResponse, ex.Failure);
        Assert.Single(Handler.Requests);
        Assert.Empty(Delay.Delays);
    }

    /// <summary>
    /// Prüft, dass eine Antwort unter dem Limit wie bisher geparst wird.
    /// </summary>
    [Fact]
    public async Task SearchAsync_ResponseWithinLimit_IsParsed()
    {
        Handler.RespondWith(HttpStatusCode.OK, ListBody);

        var stations = await CreateClient(LimitedOptions()).SearchAsync(52.52, 13.405, 5, CancellationToken.None);

        Assert.Equal(2, stations.Count);
    }

    /// <summary>
    /// Prüft den Grenzfall: Eine Antwort genau am Limit wird noch angenommen.
    /// </summary>
    [Fact]
    public async Task SearchAsync_ResponseAtExactLimit_IsParsed()
    {
        var body = Encoding.UTF8.GetBytes(ListBody + new string(' ', Limit - Encoding.UTF8.GetByteCount(ListBody)));
        Assert.Equal(Limit, body.Length);
        Handler.RespondWithBytes(HttpStatusCode.OK, body);

        var stations = await CreateClient(LimitedOptions()).SearchAsync(52.52, 13.405, 5, CancellationToken.None);

        Assert.Equal(2, stations.Count);
        Assert.Single(Handler.Requests);
    }

    /// <summary>
    /// Prüft, dass eine übergroße Detailantwort ohne Wiederholung abgelehnt wird.
    /// </summary>
    [Fact]
    public async Task GetDetailAsync_OversizedResponse_ThrowsInvalidResponse()
    {
        Handler.RespondWithBytes(HttpStatusCode.OK, OversizedBody());

        var ex = await Assert.ThrowsAsync<PriceApiException>(() => CreateClient(LimitedOptions()).GetDetailAsync(StationId, CancellationToken.None));

        Assert.Equal(PriceFailure.InvalidResponse, ex.Failure);
        Assert.Single(Handler.Requests);
        Assert.Empty(Delay.Delays);
    }

    /// <summary>
    /// Prüft, dass eine übergroße Preisantwort ohne Wiederholung abgelehnt wird.
    /// </summary>
    [Fact]
    public async Task GetPricesAsync_OversizedResponse_ThrowsInvalidResponse()
    {
        Handler.RespondWithBytes(HttpStatusCode.OK, OversizedBody());

        var ex = await Assert.ThrowsAsync<PriceApiException>(() => CreateClient(LimitedOptions()).GetPricesAsync([StationId], CancellationToken.None));

        Assert.Equal(PriceFailure.InvalidResponse, ex.Failure);
        Assert.Single(Handler.Requests);
        Assert.Empty(Delay.Delays);
    }

    /// <summary>
    /// Prüft, dass die Ablehnung übergroßer Antworten weder Anfrageadresse noch Schlüssel enthält.
    /// </summary>
    [Fact]
    public async Task OversizedError_NeverLeaksUrlOrKey()
    {
        Keys.Value = "geheimer-wert-789";
        Handler.RespondWithBytes(HttpStatusCode.OK, OversizedBody());

        var ex = await Assert.ThrowsAsync<PriceApiException>(() => CreateClient(LimitedOptions()).SearchAsync(52.52, 13.405, 5, CancellationToken.None));

        Assert.DoesNotContain("geheimer-wert-789", ex.ToString());
        Assert.DoesNotContain("prices.example.test", ex.ToString());
        Assert.DoesNotContain("apikey", ex.ToString());
    }

    /// <summary>
    /// Prüft, dass ein Abbruch des Antwortstroms beim Lesen als wiederholbarer Verbindungsfehler nach den Versuchen „nicht erreichbar“ meldet.
    /// </summary>
    [Fact]
    public async Task SearchAsync_BodyReadFails_RetriesThenUnreachable()
    {
        Handler.Respond(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new FailingStream()) });

        var ex = await Assert.ThrowsAsync<PriceApiException>(() => CreateClient().SearchAsync(52.52, 13.405, 5, CancellationToken.None));

        Assert.Equal(PriceFailure.Unreachable, ex.Failure);
        Assert.Equal(3, Handler.Requests.Count);
        Assert.Equal(2, Delay.Delays.Count);
    }

    /// <summary>
    /// Prüft, dass nach einem Körperabbruch der erneute Versuch regulär gelingt.
    /// </summary>
    [Fact]
    public async Task SearchAsync_BodyReadFailsOnce_SucceedsOnRetry()
    {
        Handler.Respond(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new FailingStream()) })
            .RespondWith(HttpStatusCode.OK, ListBody);

        var stations = await CreateClient().SearchAsync(52.52, 13.405, 5, CancellationToken.None);

        Assert.Equal(2, stations.Count);
        Assert.Equal(2, Handler.Requests.Count);
    }

    private sealed class FailingStream : MemoryStream
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromException<int>(new IOException("Verbindung abgebrochen"));
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return Task.FromException(new IOException("Verbindung abgebrochen"));
        }
    }
}

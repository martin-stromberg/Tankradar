using System.Net;
using System.Text;
using System.Text.Json;
using Tankradar.MAUI.Services.Routing;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Mock-Server des Routingdienstes: Schlüsselprüfung, Route aus Start und Ziel oder fest vorgegeben, Fehlerfolgen und Protokoll der Anfragen.
/// </summary>
public class MockOpenRouteServiceServerTests : BaseTest
{
    private const string Body = "{\"coordinates\":[[13.405,52.52],[13.405,52.88]],\"instructions\":false}";

    private readonly MockOpenRouteServiceServer _server = new();
    private readonly HttpClient _http = new();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _http.Dispose();
            _server.Dispose();
        }

        base.Dispose(disposing);
    }

    private async Task<HttpResponseMessage> PostAsync(string? key = MockOpenRouteServiceServer.AcceptedKey, string path = "v2/directions/driving-car", string body = Body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_server.BaseUrl, path))
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        if (key is not null)
        {
            request.Headers.TryAddWithoutValidation("Authorization", key);
        }

        request.Headers.UserAgent.ParseAdd("Test/1.0");
        return await _http.SendAsync(request);
    }

    /// <summary>
    /// Prüft, dass ohne gültigen Schlüssel 403 geliefert wird.
    /// </summary>
    /// <param name="key">Der Schlüssel.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("falsch")]
    public async Task Post_WithoutValidKey_Returns403(string? key)
    {
        using var response = await PostAsync(key);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(1, _server.Requests);
    }

    /// <summary>
    /// Prüft, dass ohne Vorgabe die Route Start und Ziel verbindet und als kodierte Polylinie geliefert wird.
    /// </summary>
    [Fact]
    public async Task Post_WithoutSetRoute_ConnectsStartAndDestination()
    {
        using var response = await PostAsync();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var geometry = document.RootElement.GetProperty("routes")[0].GetProperty("geometry").GetString();

        Assert.True(PolylineDecoder.TryDecode(geometry, 5, 1000, out var points));
        Assert.Equal(52.52, points[0].Latitude, 5);
        Assert.Equal(52.88, points[^1].Latitude, 5);
        Assert.Equal(21, points.Count);
    }

    /// <summary>
    /// Prüft, dass eine vorgegebene Route unverändert geliefert wird.
    /// </summary>
    [Fact]
    public async Task Post_WithSetRoute_ReturnsConfiguredRoute()
    {
        _server.SetRoute([(52.0, 13.0), (52.1, 13.2), (52.3, 13.1)]);

        using var response = await PostAsync();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var geometry = document.RootElement.GetProperty("routes")[0].GetProperty("geometry").GetString();

        Assert.True(PolylineDecoder.TryDecode(geometry, 5, 1000, out var points));
        Assert.Equal(3, points.Count);
        Assert.Equal(13.2, points[1].Longitude, 5);
    }

    /// <summary>
    /// Prüft die erzwungenen Fehler in Reihenfolge, danach wieder normale Antworten.
    /// </summary>
    [Fact]
    public async Task EnqueueStatuses_AreServedInOrderThenNormalAnswers()
    {
        _server.EnqueueStatuses(429, 503);

        using var first = await PostAsync();
        using var second = await PostAsync();
        using var third = await PostAsync();

        Assert.Equal(HttpStatusCode.TooManyRequests, first.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, second.StatusCode);
        Assert.Equal(HttpStatusCode.OK, third.StatusCode);
    }

    /// <summary>
    /// Prüft den Fehler im Format von OpenRouteService mit Code.
    /// </summary>
    [Fact]
    public async Task EnqueueError_ReturnsOrsErrorFormat()
    {
        _server.EnqueueError(404, 2010);

        using var response = await PostAsync();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(2010, document.RootElement.GetProperty("error").GetProperty("code").GetInt32());
    }

    /// <summary>
    /// Prüft das Protokoll: Anzahl, Zeitpunkte, Inhalt, Kopf, Adresse und Kennung der letzten Anfrage.
    /// </summary>
    [Fact]
    public async Task Requests_AreRecorded()
    {
        using var first = await PostAsync();
        using var second = await PostAsync();

        Assert.Equal(2, _server.Requests);
        Assert.Equal(2, _server.RequestTimes.Count);
        Assert.Equal(Body, _server.LastRequestBody);
        Assert.Equal(MockOpenRouteServiceServer.AcceptedKey, _server.LastAuthorization);
        Assert.Equal("/v2/directions/driving-car", _server.LastRawRequest);
        Assert.Equal("Test/1.0", _server.LastUserAgent);
    }

    /// <summary>
    /// Prüft, dass andere Pfade nicht gezählt und mit 404 beantwortet werden.
    /// </summary>
    [Fact]
    public async Task Post_UnknownPath_Returns404AndIsNotCounted()
    {
        using var response = await PostAsync(path: "v2/directions/foot-walking");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, _server.Requests);
    }

    /// <summary>
    /// Prüft, dass eine Verzögerung die Antwort verspätet.
    /// </summary>
    [Fact]
    public async Task ResponseDelay_DelaysAnswer()
    {
        _server.ResponseDelay = TimeSpan.FromMilliseconds(200);
        var start = DateTime.UtcNow;

        using var response = await PostAsync();

        Assert.True(DateTime.UtcNow - start >= TimeSpan.FromMilliseconds(150));
    }

    /// <summary>
    /// Prüft, dass eine fehlende Koordinatenangabe mit einem Fehlercode beantwortet wird.
    /// </summary>
    [Fact]
    public async Task Post_WithoutCoordinates_ReturnsOrsError()
    {
        using var response = await PostAsync(body: "{}");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.True(document.RootElement.TryGetProperty("error", out _));
    }
}

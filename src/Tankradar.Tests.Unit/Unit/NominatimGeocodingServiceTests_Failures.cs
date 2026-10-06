using System.Net;
using Tankradar.MAUI.Services.Geocoding;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Fehlerabbildung der Adressauflösung (Status statt Ausnahme, keine Wiederholung) und dass Eingabe und Treffer nie protokolliert werden.
/// </summary>
public class NominatimGeocodingServiceTests_Failures : NominatimGeocodingServiceTestBase
{
    /// <summary>
    /// Prüft die Abbildung der HTTP-Statuscodes.
    /// </summary>
    /// <param name="status">Der Statuscode.</param>
    /// <param name="expected">Der erwartete Ausgang.</param>
    [Theory]
    [InlineData(HttpStatusCode.Forbidden, GeocodingStatus.Rejected)]
    [InlineData(HttpStatusCode.BadRequest, GeocodingStatus.Rejected)]
    [InlineData(HttpStatusCode.Redirect, GeocodingStatus.Rejected)]
    [InlineData(HttpStatusCode.TooManyRequests, GeocodingStatus.Unavailable)]
    [InlineData(HttpStatusCode.InternalServerError, GeocodingStatus.Unavailable)]
    [InlineData(HttpStatusCode.ServiceUnavailable, GeocodingStatus.Unavailable)]
    public async Task Resolve_HttpError_MapsToStatus(HttpStatusCode status, GeocodingStatus expected)
    {
        Handler.RespondWith(status, "{}");

        var result = await CreateService().ResolveAsync("Frankfurt");

        Assert.Equal(expected, result.Status);
        Assert.Null(result.Position);
    }

    /// <summary>
    /// Prüft, dass nach einem Fehler nicht automatisch wiederholt wird (eine Anfrage je Absenden).
    /// </summary>
    [Fact]
    public async Task Resolve_ServerError_IsNotRetried()
    {
        Handler.RespondWith(HttpStatusCode.InternalServerError, "{}");

        await CreateService().ResolveAsync("Frankfurt");

        Assert.Single(Handler.Requests);
        Assert.Empty(Delay.Delays);
    }

    /// <summary>
    /// Prüft, dass Verbindungsfehler als „nicht erreichbar“ gemeldet werden.
    /// </summary>
    [Fact]
    public async Task Resolve_ConnectionError_IsUnavailable()
    {
        Handler.Throw(new HttpRequestException("Verbindung abgelehnt"));

        var result = await CreateService().ResolveAsync("Frankfurt");

        Assert.Equal(GeocodingStatus.Unavailable, result.Status);
    }

    /// <summary>
    /// Prüft, dass eine Zeitüberschreitung als „nicht erreichbar“ gemeldet wird.
    /// </summary>
    [Fact]
    public async Task Resolve_Timeout_IsUnavailable()
    {
        Handler.Hang();
        var service = CreateService(new GeocodingOptions { BaseUrl = new Uri("https://geo.example.test/"), RequestTimeout = TimeSpan.FromMilliseconds(50) });

        var result = await service.ResolveAsync("Frankfurt");

        Assert.Equal(GeocodingStatus.Unavailable, result.Status);
    }

    /// <summary>
    /// Prüft, dass ein Abbruch durch den Aufrufer als Abbruch weitergegeben wird und nicht als Fehlerstatus.
    /// </summary>
    [Fact]
    public async Task Resolve_CancelledByCaller_Throws()
    {
        Handler.Hang();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateService().ResolveAsync("Frankfurt", cancellation.Token));
    }

    /// <summary>
    /// Prüft die Antworten, die nicht auswertbar sind.
    /// </summary>
    /// <param name="body">Der Antwortinhalt.</param>
    [Theory]
    [InlineData("kein json")]
    [InlineData("{\"lat\":\"1\",\"lon\":\"2\"}")]
    [InlineData("[{\"lat\":\"abc\",\"lon\":\"8.6\"}]")]
    [InlineData("[{\"lat\":\"50.1\"}]")]
    [InlineData("[{\"lat\":\"95.0\",\"lon\":\"8.6\"}]")]
    [InlineData("[{\"lat\":\"50.1\",\"lon\":\"NaN\"}]")]
    [InlineData("[\"text\"]")]
    [InlineData("[{\"lat\":true,\"lon\":false}]")]
    public async Task Resolve_UnusableBody_IsInvalidResponse(string body)
    {
        Handler.RespondWith(HttpStatusCode.OK, body);

        var result = await CreateService().ResolveAsync("Frankfurt");

        Assert.Equal(GeocodingStatus.InvalidResponse, result.Status);
    }

    /// <summary>
    /// Prüft, dass weder die Eingabe noch der gefundene Ort oder die Koordinaten im Protokoll auftauchen, weder bei Erfolg noch bei Fehlern.
    /// </summary>
    [Fact]
    public async Task Resolve_NeverLogsInputOrResult()
    {
        var service = CreateService(new GeocodingOptions { BaseUrl = new Uri("https://geo.example.test/"), MinRequestInterval = TimeSpan.FromSeconds(1) });
        Handler.RespondWith(HttpStatusCode.OK, FoundBody)
            .RespondWith(HttpStatusCode.InternalServerError, "{}")
            .Throw(new HttpRequestException("Fehler bei https://geo.example.test/search?q=Geheimstrasse"));

        await service.ResolveAsync("Geheimstrasse 7");
        await service.ResolveAsync("Geheimstrasse 7");
        await service.ResolveAsync("Geheimstrasse 7");

        var log = Logger.AllText;
        Assert.DoesNotContain("Geheimstrasse", log, StringComparison.Ordinal);
        Assert.DoesNotContain("Frankfurt", log, StringComparison.Ordinal);
        Assert.DoesNotContain("50.11", log, StringComparison.Ordinal);
        Assert.DoesNotContain("8.68", log, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass jeder Ausgang über das Ergebnis erreichbar ist (Abdeckung aller Statuswerte).
    /// </summary>
    [Fact]
    public async Task Resolve_CoversAllStatuses()
    {
        var reached = new HashSet<GeocodingStatus>();
        foreach (var (status, body) in new (HttpStatusCode, string)[]
        {
            (HttpStatusCode.OK, FoundBody),
            (HttpStatusCode.OK, "[]"),
            (HttpStatusCode.Forbidden, "{}"),
            (HttpStatusCode.InternalServerError, "{}"),
            (HttpStatusCode.OK, "x"),
        })
        {
            var handlerService = new FakeHttpMessageHandler().RespondWith(status, body);
            var options = new GeocodingOptions { BaseUrl = new Uri("https://geo.example.test/") };
            var service = new NominatimGeocodingService(new HttpClient(handlerService), options, new RequestThrottle(options.MinRequestInterval, Delay, Clock), Logger);
            reached.Add((await service.ResolveAsync("Frankfurt")).Status);
        }

        reached.Add((await CreateService().ResolveAsync("a")).Status);
        reached.Add((await CreateService(new GeocodingOptions { EndpointNotConfigured = true }).ResolveAsync("Frankfurt")).Status);

        Assert.Equal(Enum.GetValues<GeocodingStatus>().ToHashSet(), reached);
    }
}

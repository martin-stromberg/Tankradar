using System.Net;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.MAUI.Services.Routing;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Schlüsselprüfung per Testanfrage: Rückmeldung je Antwort, Schlüssel nur im Kopf, feste Koordinaten, neue Adresse, keine Wiederholung, keine Schlüssel in Protokollen.
/// </summary>
public class OpenRouteServiceKeyCheckerTests : BaseTest
{
    private const string Key = "ZZPLATZHALTER-PRUEFSCHLUESSEL-ZZ";

    private readonly CapturingHandler _handler = new();
    private readonly RecordingLogger<OpenRouteServiceKeyChecker> _logger = new();

    private OpenRouteServiceKeyChecker Create(RoutingOptions? options = null)
    {
        options ??= new RoutingOptions { RequestTimeout = TimeSpan.FromMilliseconds(300) };
        return new OpenRouteServiceKeyChecker(new HttpClient(_handler), options, new RequestThrottle(TimeSpan.Zero, new RecordingDelay(), TimeProvider.System), _logger);
    }

    /// <summary>
    /// Prüft die Abbildung der Antworten auf die Rückmeldung.
    /// </summary>
    /// <param name="status">Der Statuscode.</param>
    /// <param name="expected">Das erwartete Ergebnis.</param>
    [Theory]
    [InlineData(HttpStatusCode.OK, RoutingKeyCheckResult.Valid)]
    [InlineData(HttpStatusCode.Unauthorized, RoutingKeyCheckResult.Invalid)]
    [InlineData(HttpStatusCode.Forbidden, RoutingKeyCheckResult.Invalid)]
    [InlineData(HttpStatusCode.TooManyRequests, RoutingKeyCheckResult.QuotaExceeded)]
    [InlineData(HttpStatusCode.InternalServerError, RoutingKeyCheckResult.Unreachable)]
    [InlineData(HttpStatusCode.ServiceUnavailable, RoutingKeyCheckResult.Unreachable)]
    [InlineData(HttpStatusCode.BadRequest, RoutingKeyCheckResult.Unavailable)]
    [InlineData(HttpStatusCode.NotFound, RoutingKeyCheckResult.Unavailable)]
    [InlineData(HttpStatusCode.Redirect, RoutingKeyCheckResult.Unavailable)]
    public async Task CheckAsync_MapsStatusToResult(HttpStatusCode status, RoutingKeyCheckResult expected)
    {
        _handler.Status = status;

        Assert.Equal(expected, await Create().CheckAsync(Key));
        Assert.Equal(1, _handler.Calls);
    }

    /// <summary>
    /// Prüft Adresse, Kopf und Inhalt: neue Basisadresse, Schlüssel nur im Kopf Authorization, feste Koordinaten, genau eine Anfrage ohne Wiederholung.
    /// </summary>
    [Fact]
    public async Task CheckAsync_SendsOneRequestToNewAddressWithKeyOnlyInHeader()
    {
        _handler.Status = HttpStatusCode.ServiceUnavailable;

        await Create(new RoutingOptions()).CheckAsync("  " + Key + "  ");

        Assert.Equal(1, _handler.Calls);
        Assert.Equal("https://api.heigit.org/openrouteservice/v2/directions/driving-car", _handler.LastUri);
        Assert.Equal(Key, _handler.LastAuthorization);
        Assert.DoesNotContain(Key, _handler.LastUri, StringComparison.Ordinal);
        Assert.Contains("\"coordinates\":[[13.405,52.52],[13.415,52.525]]", _handler.LastBody, StringComparison.Ordinal);
        Assert.DoesNotContain(Key, _handler.LastBody, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(_handler.LastUserAgent));
    }

    /// <summary>
    /// Prüft, dass Zeitüberschreitung und Verbindungsfehler als „nicht erreichbar“ gemeldet werden und nichts vom Schlüssel oder der Ausnahme ins Protokoll gelangt.
    /// </summary>
    [Fact]
    public async Task CheckAsync_TimeoutAndConnectionErrors_AreUnreachableAndLogNoSecrets()
    {
        _handler.Hang = true;
        Assert.Equal(RoutingKeyCheckResult.Unreachable, await Create().CheckAsync(Key));

        _handler.Hang = false;
        _handler.Failure = new HttpRequestException("Verbindung zu https://api.heigit.org/?k=" + Key + " fehlgeschlagen");
        Assert.Equal(RoutingKeyCheckResult.Unreachable, await Create().CheckAsync(Key));

        Assert.DoesNotContain(Key, _logger.AllText, StringComparison.Ordinal);
        Assert.DoesNotContain("?k=", _logger.AllText, StringComparison.Ordinal);
        Assert.Contains("HttpRequestException", _logger.AllText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass der Abbruch durch den Aufrufer weitergereicht wird und nicht als „nicht erreichbar“ gilt.
    /// </summary>
    [Fact]
    public async Task CheckAsync_CallerCancellation_Propagates()
    {
        _handler.Hang = true;
        using var cancellation = new CancellationTokenSource();
        cancellation.CancelAfter(50);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Create(new RoutingOptions { RequestTimeout = TimeSpan.FromSeconds(30) }).CheckAsync(Key, cancellation.Token));
    }

    /// <summary>
    /// Prüft, dass ohne Endpunkt (Testmodus) oder mit unzulässiger Schreibweise keine Anfrage gesendet wird.
    /// </summary>
    [Fact]
    public async Task CheckAsync_NoEndpointOrInvalidKey_SendsNothing()
    {
        Assert.Equal(RoutingKeyCheckResult.Unavailable, await Create(new RoutingOptions { EndpointNotConfigured = true }).CheckAsync(Key));
        Assert.Equal(RoutingKeyCheckResult.Unavailable, await Create().CheckAsync("mit leerzeichen"));
        Assert.Equal(RoutingKeyCheckResult.Unavailable, await Create().CheckAsync(string.Empty));

        Assert.Equal(0, _handler.Calls);
    }

    /// <summary>
    /// Prüft gegen den Mock-Server (neuer Pfad): Der akzeptierte Schlüssel ist gültig, ein anderer wird abgelehnt.
    /// </summary>
    [Fact]
    public async Task CheckAsync_AgainstMock_ValidAndInvalidKey()
    {
        using var server = new MockOpenRouteServiceServer();
        using var http = new HttpClient();
        var options = new RoutingOptions { BaseUrl = server.BaseUrl, AllowLoopbackHttp = true, MinRequestInterval = TimeSpan.Zero };
        var checker = new OpenRouteServiceKeyChecker(http, options, new RequestThrottle(TimeSpan.Zero, new RecordingDelay(), TimeProvider.System), _logger);

        Assert.Equal(RoutingKeyCheckResult.Valid, await checker.CheckAsync(MockOpenRouteServiceServer.AcceptedKey));
        Assert.Equal(RoutingKeyCheckResult.Invalid, await checker.CheckAsync("falscher-schluessel"));

        Assert.Equal(2, server.Requests);
        Assert.Equal(MockOpenRouteServiceServer.DirectionsPath, server.LastRawRequest);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

        public bool Hang { get; set; }

        public Exception? Failure { get; set; }

        public int Calls { get; private set; }

        public string? LastUri { get; private set; }

        public string? LastAuthorization { get; private set; }

        public string? LastBody { get; private set; }

        public string? LastUserAgent { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastUri = request.RequestUri?.ToString();
            LastAuthorization = request.Headers.TryGetValues("Authorization", out var values) ? values.Single() : null;
            LastUserAgent = request.Headers.UserAgent.ToString();
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            if (Failure is not null)
            {
                throw Failure;
            }

            if (Hang)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            return new HttpResponseMessage(Status);
        }
    }
}

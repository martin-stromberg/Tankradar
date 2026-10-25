using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.MAUI.Services.Routing;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass der Schlüssel nur im Anfragekopf steht, nie in der Adresse, dass ein fehlender Schlüssel vor jedem Aufruf erkannt wird
/// und dass der Testmodus ohne Endpunkt den Abruf verweigert.
/// </summary>
public class OpenRouteServiceClientTests_Key : OpenRouteServiceClientTestBase
{
    /// <summary>
    /// Prüft, dass ein fehlender Schlüssel ohne Netzzugriff gemeldet wird.
    /// </summary>
    /// <param name="key">Der Schlüssel.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetRouteAsync_MissingKey_ReturnsApiKeyMissingWithoutRequest(string? key)
    {
        Keys.Value = key;

        var result = await CreateClient().GetRouteAsync(Start, Destination);

        Assert.Equal(RoutingFailure.ApiKeyMissing, result.Failure);
        Assert.Empty(Handler.Requests);
    }

    /// <summary>
    /// Prüft die Verfügbarkeitsprüfung ohne Netzzugriff.
    /// </summary>
    [Fact]
    public async Task IsAvailableAsync_DependsOnKeyAndEndpoint_WithoutRequest()
    {
        Assert.True(await CreateClient().IsAvailableAsync());

        Keys.Value = null;
        Assert.False(await CreateClient().IsAvailableAsync());

        Keys.Value = "x";
        var notConfigured = new RoutingOptions { EndpointNotConfigured = true };
        Assert.False(await CreateClient(notConfigured).IsAvailableAsync());
        Assert.Empty(Handler.Requests);
    }

    /// <summary>
    /// Prüft, dass der Testmodus ohne Endpunkt den Abruf verweigert und nichts sendet.
    /// </summary>
    [Fact]
    public async Task GetRouteAsync_EndpointNotConfigured_ReturnsFailureWithoutRequest()
    {
        var options = new RoutingOptions { EndpointNotConfigured = true };

        var result = await CreateClient(options).GetRouteAsync(Start, Destination);

        Assert.Equal(RoutingFailure.EndpointNotConfigured, result.Failure);
        Assert.Empty(Handler.Requests);
    }

    /// <summary>
    /// Prüft gegen den Mock-Server: Der Schlüssel steht im Kopf Authorization, nie in der Adresse; Koordinaten stehen als Länge/Breite im Inhalt.
    /// </summary>
    [Fact]
    public async Task GetRouteAsync_AgainstMock_SendsKeyOnlyInHeader()
    {
        using var server = new MockOpenRouteServiceServer();
        Keys.Value = MockOpenRouteServiceServer.AcceptedKey;
        var options = new RoutingOptions { BaseUrl = server.BaseUrl, AllowLoopbackHttp = true, MinRequestInterval = TimeSpan.Zero };
        using var http = new HttpClient();
        var client = new OpenRouteServiceClient(http, Keys, options, new TaskDelay(), new RequestThrottle(TimeSpan.Zero, new TaskDelay(), Clock), NullLogger<OpenRouteServiceClient>.Instance);

        var result = await client.GetRouteAsync(Start, Destination);

        Assert.Equal(RoutingFailure.None, result.Failure);
        Assert.Equal(MockOpenRouteServiceServer.AcceptedKey, server.LastAuthorization);
        Assert.DoesNotContain(MockOpenRouteServiceServer.AcceptedKey, server.LastRawRequest, StringComparison.Ordinal);
        Assert.DoesNotContain("api_key", server.LastRawRequest, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"coordinates\":[[13.405,52.52],[13.405,52.88]]", server.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("\"instructions\":false", server.LastRequestBody, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(server.LastUserAgent));
    }

    /// <summary>
    /// Prüft gegen den Mock-Server: Ein falscher Schlüssel wird als abgelehnt gemeldet, ohne Wiederholung und ohne den Schlüssel in Folgetexten.
    /// </summary>
    [Fact]
    public async Task GetRouteAsync_WrongKey_ReturnsKeyRejectedWithoutRetry()
    {
        using var server = new MockOpenRouteServiceServer();
        Keys.Value = "falscher-schluessel";
        var options = new RoutingOptions { BaseUrl = server.BaseUrl, AllowLoopbackHttp = true, MinRequestInterval = TimeSpan.Zero };
        using var http = new HttpClient();
        var client = new OpenRouteServiceClient(http, Keys, options, new TaskDelay(), new RequestThrottle(TimeSpan.Zero, new TaskDelay(), Clock), NullLogger<OpenRouteServiceClient>.Instance);

        var result = await client.GetRouteAsync(Start, Destination);

        Assert.Equal(RoutingFailure.KeyRejected, result.Failure);
        Assert.Equal(1, server.Requests);
        Assert.DoesNotContain("falscher-schluessel", result.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass der Client keine Weiterleitung mit dem Schlüssel an einen anderen Host verfolgt (Antwort 302 ist ein Clientfehler).
    /// </summary>
    [Fact]
    public async Task GetRouteAsync_Redirect_IsNotFollowed()
    {
        Handler.RespondWith(HttpStatusCode.Redirect, "{}");

        var result = await CreateClient().GetRouteAsync(Start, Destination);

        Assert.Equal(RoutingFailure.Rejected, result.Failure);
        Assert.Single(Handler.Requests);
    }
}

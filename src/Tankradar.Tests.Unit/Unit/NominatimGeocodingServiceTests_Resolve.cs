using System.Net;
using Tankradar.MAUI.Services.Geocoding;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Adressauflösung: gesendete Anfrage (Adresse, Parameter, Kennung), Auswertung der Antwort und Verzicht auf Anfragen bei ungültiger Eingabe.
/// </summary>
public class NominatimGeocodingServiceTests_Resolve : NominatimGeocodingServiceTestBase
{
    /// <summary>
    /// Prüft, dass ein Treffer in Position und Anzeigenamen umgewandelt wird.
    /// </summary>
    [Fact]
    public async Task Resolve_Found_ReturnsPositionAndName()
    {
        Handler.RespondWith(HttpStatusCode.OK, FoundBody);

        var result = await CreateService().ResolveAsync("Frankfurt");

        Assert.Equal(GeocodingStatus.Found, result.Status);
        Assert.Equal(50.1109221, result.Position!.Latitude, 6);
        Assert.Equal(8.6821267, result.Position.Longitude, 6);
        Assert.Equal("Frankfurt am Main, Hessen, Deutschland", result.PlaceName);
    }

    /// <summary>
    /// Prüft die Anfrageadresse: Suchbegriff maskiert, ein Treffer, Beschränkung auf Deutschland, JSON-Format und deutsche Sprache.
    /// </summary>
    [Fact]
    public async Task Resolve_SendsEscapedQueryWithPolicyParameters()
    {
        Handler.RespondWith(HttpStatusCode.OK, FoundBody);

        await CreateService().ResolveAsync("  Hauptstraße 1,   10115 Berlin ");

        var uri = Assert.Single(Handler.Requests);
        Assert.Equal("geo.example.test", uri.Host);
        Assert.Equal("/search", uri.AbsolutePath);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        Assert.Equal("Hauptstraße 1, 10115 Berlin", query["q"]);
        Assert.Equal("1", query["limit"]);
        Assert.Equal("de", query["countrycodes"]);
        Assert.Equal("jsonv2", query["format"]);
        Assert.Equal("de", query["accept-language"]);
    }

    /// <summary>
    /// Prüft, dass typografische Zeichen der iOS-Tastatur (Apostroph, Gedankenstrich) normalisiert in der Anfrage stehen und die Eingabe nicht abgelehnt wird.
    /// </summary>
    [Fact]
    public async Task Resolve_TypographicCharacters_AreSentNormalized()
    {
        Handler.RespondWith(HttpStatusCode.OK, FoundBody);

        var result = await CreateService().ResolveAsync("Up’n Kamp 5 – Hamburg");

        Assert.Equal(GeocodingStatus.Found, result.Status);
        var uri = Assert.Single(Handler.Requests);
        Assert.Equal("Up'n Kamp 5 - Hamburg", System.Web.HttpUtility.ParseQueryString(uri.Query)["q"]);
    }

    /// <summary>
    /// Prüft, dass die Anfrage die Kennung der App trägt (Nutzungsrichtlinie).
    /// </summary>
    [Fact]
    public async Task Resolve_SendsIdentifyingUserAgent()
    {
        Handler.RespondWith(HttpStatusCode.OK, FoundBody);

        await CreateService().ResolveAsync("Frankfurt");

        var userAgent = Assert.Single(Handler.UserAgents);
        Assert.Contains("Tankatlas", userAgent, StringComparison.Ordinal);
        Assert.Contains("de.martinstromberg.tankradar", userAgent, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass ungültige Eingaben keine Anfrage auslösen.
    /// </summary>
    /// <param name="input">Die Eingabe.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("Berlin<script>")]
    public async Task Resolve_InvalidInput_SendsNoRequest(string? input)
    {
        var result = await CreateService().ResolveAsync(input);

        Assert.Equal(GeocodingStatus.InvalidInput, result.Status);
        Assert.Empty(Handler.Requests);
    }

    /// <summary>
    /// Prüft, dass im Testmodus ohne Endpunkt keine Anfrage gesendet wird.
    /// </summary>
    [Fact]
    public async Task Resolve_EndpointNotConfigured_SendsNoRequest()
    {
        var service = CreateService(new GeocodingOptions { EndpointNotConfigured = true });

        var result = await service.ResolveAsync("Frankfurt");

        Assert.Equal(GeocodingStatus.EndpointNotConfigured, result.Status);
        Assert.Empty(Handler.Requests);
    }

    /// <summary>
    /// Prüft, dass eine leere Trefferliste als „nicht gefunden“ gilt.
    /// </summary>
    [Fact]
    public async Task Resolve_EmptyList_IsNotFound()
    {
        Handler.RespondWith(HttpStatusCode.OK, "[]");

        var result = await CreateService().ResolveAsync("Nirgendwoburg");

        Assert.Equal(GeocodingStatus.NotFound, result.Status);
        Assert.Null(result.Position);
        Assert.Null(result.PlaceName);
    }

    /// <summary>
    /// Prüft, dass Koordinaten auch als JSON-Zahlen gelesen werden und ein fehlender Anzeigename erlaubt ist.
    /// </summary>
    [Fact]
    public async Task Resolve_NumericCoordinatesWithoutName_AreAccepted()
    {
        Handler.RespondWith(HttpStatusCode.OK, """[{"lat":48.1,"lon":11.5}]""");

        var result = await CreateService().ResolveAsync("München");

        Assert.Equal(GeocodingStatus.Found, result.Status);
        Assert.Equal(48.1, result.Position!.Latitude, 6);
        Assert.Null(result.PlaceName);
    }

    /// <summary>
    /// Prüft, dass überlange Anzeigenamen gekürzt und Steuerzeichen entfernt werden.
    /// </summary>
    [Fact]
    public async Task Resolve_LongDisplayName_IsTruncatedAndSanitized()
    {
        var name = "A\\u0007" + new string('b', 400);
        Handler.RespondWith(HttpStatusCode.OK, "[{\"lat\":\"48.1\",\"lon\":\"11.5\",\"display_name\":\"" + name + "\"}]");

        var result = await CreateService().ResolveAsync("München");

        Assert.Equal(200, result.PlaceName!.Length);
        Assert.DoesNotContain('\u0007', result.PlaceName);
    }

    /// <summary>
    /// Prüft, dass die Ergebnistypen nur den Status als Textdarstellung liefern (kein Ort, keine Position).
    /// </summary>
    [Fact]
    public async Task Result_ToString_ContainsOnlyStatus()
    {
        Handler.RespondWith(HttpStatusCode.OK, FoundBody);

        var result = await CreateService().ResolveAsync("Frankfurt");

        Assert.Equal("Found", result.ToString());
        Assert.Throws<ArgumentException>(() => GeocodingResult.Failure(GeocodingStatus.Found));
    }
}

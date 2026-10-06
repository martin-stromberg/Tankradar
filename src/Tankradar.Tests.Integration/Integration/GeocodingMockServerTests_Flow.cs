using Tankradar.MAUI.Services.Geocoding;
using Tankradar.Tests.Integration.Integration.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft die Adressauflösung des echten Ortssuchdienstes gegen den lokalen Nominatim-Mock: Anfrage (Kennung, Parameter), Ergebnisse, Fehler und die Einhaltung der Anfragebegrenzung in Echtzeit.
/// Es werden nie produktive Endpunkte angesprochen.
/// </summary>
public class GeocodingMockServerTests_Flow : SearchMockServerTestBase
{
    /// <summary>
    /// Prüft die Auflösung der bekannten Testorte in ihre Positionen.
    /// </summary>
    /// <param name="query">Der Suchbegriff.</param>
    /// <param name="latitude">Der erwartete Breitengrad.</param>
    /// <param name="longitude">Der erwartete Längengrad.</param>
    [Theory]
    [InlineData(MockNominatimServer.QueryBerlin, 52.52, 13.405)]
    [InlineData("10115", 52.52, 13.405)]
    [InlineData(MockNominatimServer.QueryOranienburg, 52.88, 13.405)]
    [InlineData(MockNominatimServer.QueryEberswalde, 52.52, 13.73)]
    public async Task Resolve_KnownPlace_ReturnsPosition(string query, double latitude, double longitude)
    {
        var result = await CreateGeocoding().ResolveAsync(query);

        Assert.Equal(GeocodingStatus.Found, result.Status);
        Assert.Equal(latitude, result.Position!.Latitude, 4);
        Assert.Equal(longitude, result.Position.Longitude, 4);
        Assert.False(string.IsNullOrWhiteSpace(result.PlaceName));
    }

    /// <summary>
    /// Prüft, dass die Anfrage Kennung, Suchbegriff und Parameter der Nutzungsrichtlinie trägt (der Mock lehnt Anfragen ohne Kennung ab).
    /// </summary>
    [Fact]
    public async Task Resolve_SendsIdentifyingUserAgentAndPolicyParameters()
    {
        await CreateGeocoding().ResolveAsync("  Hauptstraße 1,  10115 Berlin ");

        Assert.Equal(1, Nominatim.Requests);
        Assert.Equal("Hauptstraße 1, 10115 Berlin", Nominatim.LastQuery);
        Assert.Contains("Tankatlas", Nominatim.LastUserAgent, StringComparison.Ordinal);
        Assert.Contains("limit=1", Nominatim.LastRawRequest, StringComparison.Ordinal);
        Assert.Contains("countrycodes=de", Nominatim.LastRawRequest, StringComparison.Ordinal);
        Assert.StartsWith("/search?", Nominatim.LastRawRequest, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft die Gegenprobe: Der Mock lehnt Anfragen ohne identifizierende Kennung wie das Original ab.
    /// </summary>
    [Fact]
    public async Task Mock_RequestWithoutUserAgent_IsForbidden()
    {
        using var http = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(Nominatim.BaseUrl, "search?q=Berlin&format=jsonv2"));
        request.Headers.UserAgent.Clear();

        using var response = await http.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// Prüft, dass ein unbekannter Ort als „nicht gefunden“ gilt.
    /// </summary>
    [Fact]
    public async Task Resolve_UnknownPlace_IsNotFound()
    {
        var result = await CreateGeocoding().ResolveAsync(MockNominatimServer.QueryUnknown);

        Assert.Equal(GeocodingStatus.NotFound, result.Status);
    }

    /// <summary>
    /// Prüft, dass ungültige Eingaben den Mock nie erreichen.
    /// </summary>
    /// <param name="input">Die Eingabe.</param>
    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("Berlin<script>")]
    public async Task Resolve_InvalidInput_NeverReachesServer(string input)
    {
        var result = await CreateGeocoding().ResolveAsync(input);

        Assert.Equal(GeocodingStatus.InvalidInput, result.Status);
        Assert.Equal(0, Nominatim.Requests);
    }

    /// <summary>
    /// Prüft die Abbildung von Serverfehlern und unbrauchbaren Antworten, jeweils mit genau einer Anfrage (keine Wiederholung).
    /// </summary>
    [Fact]
    public async Task Resolve_ServerFailures_AreMappedWithoutRetry()
    {
        var service = CreateGeocoding();
        Nominatim.EnqueueStatuses(503);
        var unavailable = await service.ResolveAsync("Berlin");
        Nominatim.EnqueueStatuses(403);
        var rejected = await service.ResolveAsync("Berlin");
        Nominatim.EnqueueBody("<html>Fehler</html>");
        var invalid = await service.ResolveAsync("Berlin");

        Assert.Equal(GeocodingStatus.Unavailable, unavailable.Status);
        Assert.Equal(GeocodingStatus.Rejected, rejected.Status);
        Assert.Equal(GeocodingStatus.InvalidResponse, invalid.Status);
        Assert.Equal(3, Nominatim.Requests);
    }

    /// <summary>
    /// Prüft, dass ein nicht erreichbarer Dienst als solcher gemeldet wird.
    /// </summary>
    [Fact]
    public async Task Resolve_UnreachableServer_IsUnavailable()
    {
        using var stopped = new MockNominatimServer();
        var url = stopped.BaseUrl;
        stopped.Dispose();

        var result = await CreateGeocoding(url).ResolveAsync("Berlin");

        Assert.Equal(GeocodingStatus.Unavailable, result.Status);
    }

    /// <summary>
    /// Prüft in Echtzeit, dass zwei unmittelbar aufeinanderfolgende Auflösungen mindestens eine Sekunde Abstand haben (Nutzungsrichtlinie).
    /// </summary>
    [Fact]
    public async Task Resolve_TwoQuickRequests_AreAtLeastOneSecondApart()
    {
        var service = CreateGeocoding();

        await service.ResolveAsync("Berlin");
        await service.ResolveAsync("Oranienburg");

        var times = Nominatim.RequestTimes;
        Assert.Equal(2, times.Count);
        Assert.True(times[1] - times[0] >= TimeSpan.FromMilliseconds(950), $"Abstand nur {(times[1] - times[0]).TotalMilliseconds} ms.");
    }
}

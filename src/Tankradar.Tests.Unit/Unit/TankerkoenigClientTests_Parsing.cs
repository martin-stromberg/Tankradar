using System.Net;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Pricing;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Anfrageaufbau und Auswertung der Antworten des Tankerkönig-Clients.
/// </summary>
public class TankerkoenigClientTests_Parsing : TankerkoenigClientTestBase
{
    /// <summary>
    /// Prüft, dass Stationen und Preise mit Abrufzeitstempel übernommen und fehlende Angaben nicht erfunden werden.
    /// </summary>
    [Fact]
    public async Task SearchAsync_ParsesStationsAndPrices()
    {
        Handler.RespondWith(HttpStatusCode.OK, ListBody);

        var stations = await CreateClient().SearchAsync(52.52, 13.405, 5, CancellationToken.None);

        Assert.Equal(2, stations.Count);
        var alpha = stations[0];
        Assert.Equal("Alpha", alpha.Name);
        Assert.Equal("Weg", alpha.Street);
        Assert.Equal(0.1, alpha.DistanceKm);
        Assert.True(alpha.IsOpen);
        Assert.Equal(
            [(FuelType.SuperE5, 1.859m), (FuelType.SuperE10, 1.799m), (FuelType.Diesel, 1.699m)],
            alpha.Prices.Select(p => (p.FuelType, p.Price)));
        Assert.All(alpha.Prices, p => Assert.Equal(Clock.UtcNow, p.RetrievedUtc));
        var beta = stations[1];
        Assert.Single(beta.Prices);
        Assert.Null(beta.Brand);
        Assert.Null(beta.WholeDay);
        Assert.Empty(beta.OpeningTimes);
    }

    /// <summary>
    /// Prüft den Aufbau der Anfrage: HTTPS, Invariante Zahlenformate, Radius, Schlüssel nur als Parameter.
    /// </summary>
    [Fact]
    public async Task SearchAsync_BuildsHttpsRequestWithInvariantNumbers()
    {
        Handler.RespondWith(HttpStatusCode.OK, ListBody);
        Keys.Value = "abc def";

        await CreateClient().SearchAsync(52.52, 13.405, 7, CancellationToken.None);

        var uri = Assert.Single(Handler.Requests);
        Assert.Equal(Uri.UriSchemeHttps, uri.Scheme);
        Assert.Contains("lat=52.52&lng=13.405&rad=7", uri.Query);
        Assert.Contains("type=all", uri.Query);
        Assert.Contains("abc%20def", uri.Query);
    }

    /// <summary>
    /// Prüft die Auswertung der Details einer Tankstelle einschließlich Öffnungszeiten.
    /// </summary>
    [Fact]
    public async Task GetDetailAsync_ParsesOpeningTimesAndWholeDay()
    {
        Handler.RespondWith(HttpStatusCode.OK, """
            {"ok":true,"station":{"id":"11111111-1111-4111-8111-111111111111","name":"Alpha","lat":52.5,"lng":13.4,"wholeDay":false,
             "openingTimes":[{"text":"Mo-Fr","start":"06:00:00","end":"22:00:00"},{"text":"kaputt"}],"e5":1.9,"diesel":false}}
            """);

        var station = await CreateClient().GetDetailAsync("11111111-1111-4111-8111-111111111111", CancellationToken.None);

        Assert.False(station.WholeDay);
        Assert.Equal(new OpeningTimeEntry("Mo-Fr", "06:00:00", "22:00:00"), Assert.Single(station.OpeningTimes));
        Assert.Equal(Clock.UtcNow, station.DetailsUpdatedUtc);
        Assert.Contains("id=11111111-1111-4111-8111-111111111111", Handler.Requests[0].Query);
    }

    /// <summary>
    /// Prüft, dass eine ungültige Kennung vor dem Abruf abgewiesen wird.
    /// </summary>
    [Fact]
    public async Task GetDetailAsync_InvalidId_ThrowsBeforeRequest()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => CreateClient().GetDetailAsync("1&x=2", CancellationToken.None));

        Assert.Empty(Handler.Requests);
    }

    /// <summary>
    /// Prüft, dass ungültiges JSON, ein fehlendes Stationsfeld und ein abgelehnter Aufruf eindeutig gemeldet werden.
    /// </summary>
    /// <param name="body">Der Antwortkörper.</param>
    /// <param name="expected">Der erwartete Fehlergrund.</param>
    [Theory]
    [InlineData("<html>", PriceFailure.InvalidResponse)]
    [InlineData("[1,2]", PriceFailure.InvalidResponse)]
    [InlineData("{\"ok\":true}", PriceFailure.InvalidResponse)]
    [InlineData("{\"ok\":false,\"message\":\"apikey falsch\"}", PriceFailure.Rejected)]
    public async Task SearchAsync_BadBodies_AreClassified(string body, PriceFailure expected)
    {
        Handler.RespondWith(HttpStatusCode.OK, body);

        var ex = await Assert.ThrowsAsync<PriceApiException>(() => CreateClient().SearchAsync(52.5, 13.4, 5, CancellationToken.None));

        Assert.Equal(expected, ex.Failure);
        Assert.Single(Handler.Requests);
    }
}

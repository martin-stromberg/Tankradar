using System.Net;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Pricing;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Anfrage und Auswertung des Preisabrufs nach Kennungen (<c>prices.php</c>).
/// </summary>
public class TankerkoenigClientTests_Prices : TankerkoenigClientTestBase
{
    private const string Alpha = "11111111-1111-4111-8111-111111111111";
    private const string Beta = "22222222-2222-4222-8222-222222222222";

    /// <summary>
    /// Prüft, dass Sortenpreise übernommen und <c>false</c>-Werte ausgelassen werden.
    /// </summary>
    [Fact]
    public async Task GetPricesAsync_ParsesPricesAndFalseValues()
    {
        Handler.RespondWith(
            HttpStatusCode.OK,
            "{\"ok\":true,\"prices\":{\"" + Alpha + "\":{\"status\":\"open\",\"e5\":1.859,\"e10\":1.799,\"diesel\":1.699},"
            + "\"" + Beta + "\":{\"status\":\"open\",\"e5\":1.879,\"e10\":false,\"diesel\":0}}}");

        var result = await CreateClient().GetPricesAsync([Alpha, Beta], CancellationToken.None);

        Assert.Equal(
            [(FuelType.SuperE5, 1.859m), (FuelType.SuperE10, 1.799m), (FuelType.Diesel, 1.699m)],
            result[Alpha].Select(p => (p.FuelType, p.Price)));
        Assert.Equal([(FuelType.SuperE5, 1.879m)], result[Beta].Select(p => (p.FuelType, p.Price)));
        Assert.All(result[Alpha], p => Assert.Equal(Clock.UtcNow, p.RetrievedUtc));
    }

    /// <summary>
    /// Prüft den Aufbau der Anfrage: HTTPS, Kennungen kommagetrennt, Schlüssel nur als Parameter.
    /// </summary>
    [Fact]
    public async Task GetPricesAsync_BuildsRequestWithIdsAndInvariantFormat()
    {
        Handler.RespondWith(HttpStatusCode.OK, "{\"ok\":true,\"prices\":{}}");
        Keys.Value = "abc def";

        await CreateClient().GetPricesAsync([Alpha.ToUpperInvariant(), Beta], CancellationToken.None);

        var uri = Assert.Single(Handler.Requests);
        Assert.Equal(Uri.UriSchemeHttps, uri.Scheme);
        Assert.EndsWith("/prices.php", uri.AbsolutePath);
        Assert.Contains($"ids={Alpha},{Beta}", uri.Query);
        Assert.Contains("abc%20def", uri.Query);
    }

    /// <summary>
    /// Prüft, dass Kennungen ohne Preise eine leere Zeilenliste ergeben.
    /// </summary>
    [Fact]
    public async Task GetPricesAsync_NoPricesStatus_YieldsNoLines()
    {
        Handler.RespondWith(HttpStatusCode.OK, "{\"ok\":true,\"prices\":{\"" + Alpha + "\":{\"status\":\"no prices\"}}}");

        var result = await CreateClient().GetPricesAsync([Alpha], CancellationToken.None);

        Assert.Empty(result[Alpha]);
    }

    /// <summary>
    /// Prüft, dass ungültige Kennungen und unzulässige Mengen vor der Anfrage abgewiesen werden.
    /// </summary>
    /// <param name="count">Anzahl der Kennungen.</param>
    /// <param name="invalid">Ob eine Kennung ungültig ist.</param>
    [Theory]
    [InlineData(0, false)]
    [InlineData(11, false)]
    [InlineData(2, true)]
    public async Task GetPricesAsync_InvalidIdOrMoreThanTen_ThrowsBeforeRequest(int count, bool invalid)
    {
        var ids = Enumerable.Range(1, count).Select(i => new Guid(i, 0, 0, [0, 0, 0, 0, 0, 0, 0, 1]).ToString("D")).ToList();
        if (invalid)
        {
            ids[1] = "1&x=2";
        }

        await Assert.ThrowsAsync<ArgumentException>(() => CreateClient().GetPricesAsync(ids, CancellationToken.None));

        Assert.Empty(Handler.Requests);
    }

    /// <summary>
    /// Prüft, dass ungültige Antworten wie bei der Umkreissuche eindeutig gemeldet werden.
    /// </summary>
    /// <param name="body">Der Antwortkörper.</param>
    /// <param name="expected">Der erwartete Fehlergrund.</param>
    [Theory]
    [InlineData("<html>", PriceFailure.InvalidResponse)]
    [InlineData("{\"ok\":true}", PriceFailure.InvalidResponse)]
    [InlineData("{\"ok\":true,\"prices\":[]}", PriceFailure.InvalidResponse)]
    [InlineData("{\"ok\":false,\"message\":\"apikey falsch\"}", PriceFailure.Rejected)]
    public async Task GetPricesAsync_BadBodies_AreClassified(string body, PriceFailure expected)
    {
        Handler.RespondWith(HttpStatusCode.OK, body);

        var ex = await Assert.ThrowsAsync<PriceApiException>(() => CreateClient().GetPricesAsync([Alpha], CancellationToken.None));

        Assert.Equal(expected, ex.Failure);
        Assert.Single(Handler.Requests);
    }
}

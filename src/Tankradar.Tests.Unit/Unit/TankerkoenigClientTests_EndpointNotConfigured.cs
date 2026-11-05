using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Pricing;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass der Client ohne konfigurierten Test-Endpunkt nichts abruft.
/// </summary>
public class TankerkoenigClientTests_EndpointNotConfigured : TankerkoenigClientTestBase
{
    /// <summary>
    /// Prüft, dass die Suche verweigert wird und keine Anfrage hinausgeht.
    /// </summary>
    [Fact]
    public async Task SearchAsync_EndpointNotConfigured_RefusesWithoutRequest()
    {
        var client = CreateClient(new PriceApiOptions { EndpointNotConfigured = true });

        var ex = await Assert.ThrowsAsync<PriceApiException>(() => client.SearchAsync(52.5, 13.4, 5, CancellationToken.None));

        Assert.Equal(PriceFailure.EndpointNotConfigured, ex.Failure);
        Assert.Empty(Handler.Requests);
    }

    /// <summary>
    /// Prüft, dass auch der Detailabruf verweigert wird.
    /// </summary>
    [Fact]
    public async Task GetDetailAsync_EndpointNotConfigured_RefusesWithoutRequest()
    {
        var client = CreateClient(new PriceApiOptions { EndpointNotConfigured = true });

        var ex = await Assert.ThrowsAsync<PriceApiException>(() => client.GetDetailAsync("11111111-1111-4111-8111-111111111111", CancellationToken.None));

        Assert.Equal(PriceFailure.EndpointNotConfigured, ex.Failure);
        Assert.Empty(Handler.Requests);
    }
}

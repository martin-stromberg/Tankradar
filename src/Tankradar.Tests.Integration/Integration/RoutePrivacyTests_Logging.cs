using Tankradar.TestSupport;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft, dass die Dienste der Routensuche bei Erfolg und bei Fehlern weder Start- und Zieltext noch Koordinaten, Route oder Schlüssel protokollieren.
/// </summary>
public class RoutePrivacyTests_Logging : RouteSearchMockServerTestBase
{
    private const string UniqueStart = "10115 Teststadt-Geheimviertel";
    private const string UniqueDestination = "Oranienburg Heimlichgasse";

    private static readonly string[] Forbidden =
    [
        "Teststadt",
        "Geheimviertel",
        "Heimlichgasse",
        "Oranienburg",
        "52.52",
        "13.405",
        "52,52",
        "13,405",
        "52.88",
        "52,88",
        "coordinates",
        "Authorization",
        "v2/directions",
        "driving-car",
        MockOpenRouteServiceServer.AcceptedKey,
        MockTankerkoenigServer.AcceptedKey,
    ];

    private void AssertLogIsClean()
    {
        var log = LogText;
        foreach (var fragment in Forbidden)
        {
            Assert.DoesNotContain(fragment, log, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain(PolylineEncoder.Encode([(52.52, 13.405), (52.88, 13.405)])[..8], log, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft Erfolg und mehrere Fehlerfälle über die komplette Kette.
    /// </summary>
    [Fact]
    public async Task RouteSearchSuccessAndFailures_LogNothingSensitive()
    {
        var viewModel = await CreateRouteViewModelAsync();
        viewModel.StartInput = UniqueStart;
        viewModel.DestinationInput = UniqueDestination;
        await viewModel.SearchAsync();
        Assert.NotEmpty(viewModel.Stations);

        Routing.EnqueueStatuses(503, 503, 503);
        await viewModel.SearchAsync();
        Routing.EnqueueStatuses(429, 429, 429);
        await viewModel.SearchAsync();
        Routing.EnqueueError(404, 2010);
        await viewModel.SearchAsync();
        Nominatim.EnqueueStatuses(500);
        await viewModel.SearchAsync();
        Server.EnqueueStatuses(503, 503, 503);
        Clock.Advance(TimeSpan.FromMinutes(10));
        await viewModel.SearchAsync();

        var service = CreateService();
        var rejected = await CreateRouteViewModelAsync(service, CreateRouteSearch(service, key: "falscher-schluessel"));
        rejected.StartInput = UniqueStart;
        rejected.DestinationInput = UniqueDestination;
        await rejected.SearchAsync();

        AssertLogIsClean();
        foreach (var message in new[] { viewModel.StatusMessage, rejected.StatusMessage, viewModel.RouteSummary })
        {
            foreach (var fragment in new[] { "52.52", "13.405", "52,52", "13,405", "falscher-schluessel", "Authorization" })
            {
                Assert.DoesNotContain(fragment, message ?? string.Empty, StringComparison.Ordinal);
            }
        }
    }
}

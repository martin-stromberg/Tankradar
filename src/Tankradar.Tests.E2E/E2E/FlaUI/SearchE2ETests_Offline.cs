using System.Text.RegularExpressions;
using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Ohne erreichbaren Preisdienst zeigt die Suche die zuletzt bekannten Preise mit Alter und einem Offline-Hinweis in der Kopfzeile.
/// </summary>
public class SearchE2ETests_Offline : SearchE2ETestBase
{
    /// <summary>
    /// Prüft, dass nach einer erfolgreichen Suche und einem Neustart mit nicht erreichbarem Preisdienst die gespeicherten Preise erscheinen.
    /// </summary>
    [Fact]
    public void UnreachableService_ShowsLastKnownPricesWithAgeAndBanner()
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();
            Submit();
            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
            Assert.False(Exists("Search.OfflineBanner"));
            var requestsBeforeRestart = Server.TotalRequests;

            RestartApplication(CreateEnvironment(ClosedPortUrl(), TestLocation, MockTankerkoenigServer.AcceptedKey));
            OpenSearch();
            Submit();

            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
            Assert.Equal("Offline: keine Verbindung zum Preisdienst.", WaitForAutomationId("Search.OfflineBanner").Name);
            Assert.Equal("Es werden die zuletzt bekannten Preise angezeigt.", WaitForAutomationId("Search.SourceNote").Name);
            Assert.Equal(["1,859 €", "1,879 €"], ReadTexts("Search.Price.SuperE5"));
            Assert.All(ReadTexts("Search.Age.SuperE5"), text => Assert.Matches(new Regex(@"^vor \d+ Min\.$"), text));
            Assert.Equal(requestsBeforeRestart, Server.TotalRequests);
        });
    }

    /// <summary>
    /// Prüft, dass ohne Preise im Speicher und ohne Preisdienst eine Fehlermeldung statt einer leeren Liste erscheint und der Hinweis in der Kopfzeile bleibt.
    /// </summary>
    [Fact]
    public void UnreachableServiceWithoutCache_ShowsFailureNotEmptyState()
    {
        RunWithDiagnostics(() =>
        {
            RestartApplication(CreateEnvironment(ClosedPortUrl(), TestLocation, MockTankerkoenigServer.AcceptedKey));
            OpenSearch();

            Submit();

            WaitForStatusMessage("Der Preisdienst ist nicht erreichbar.");
            Assert.False(Exists("Search.EmptyState"));
            Assert.False(Exists("Search.Station.Name"));
            WaitForAutomationId("Search.OfflineBanner");
        });
    }
}

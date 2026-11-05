using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Ohne erreichbaren Preisdienst zeigt die Startseite die zuletzt bekannten Preise mit Alter und einen Offline-Hinweis.
/// </summary>
public class HomeE2ETests_Offline : HomeE2ETestBase
{
    /// <summary>
    /// Prüft Preise aus dem lokalen Stand, Altersangabe und Hinweis nach einem Neustart mit nicht erreichbarem Preisdienst.
    /// </summary>
    [Fact]
    public void UnreachableService_ShowsLastKnownPricesAndBanner()
    {
        RunWithDiagnostics(() =>
        {
            CreateGroupWithStation(0, "Alpha Tankstelle", "Arbeitsweg");

            RestartApplication(CreateEnvironment(ClosedPortUrl(), TestLocation, MockTankerkoenigServer.AcceptedKey));
            OpenHome("Arbeitsweg");

            Assert.Equal("Offline: keine Verbindung zum Preisdienst.", WaitForAutomationId("Favorites.OfflineBanner").Name);
            WaitForText("Favorites.Station.Price.SuperE5", "1,859 €/L");
            Assert.All(SafeTexts("Favorites.Station.Age.SuperE5"), text => Assert.Matches(AgePattern, text));
        });
    }
}

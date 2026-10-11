namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Bei der Standortnutzung „Nie“ fehlt der Bereich „In der Nähe“ trotz eingeschaltetem Schalter; es gibt weder Entfernungen noch Umkreisabruf.
/// </summary>
public class HomeE2ETests_NearbyGpsNever : HomeE2ETestBase
{
    /// <summary>
    /// Prüft, dass Bereich und Entfernungen fehlen und der Mock keine Umkreisanfrage erhält.
    /// </summary>
    [Fact]
    public void GpsNever_HidesNearbyAndSendsNoRadiusRequest()
    {
        RunWithDiagnostics(() =>
        {
            CreateGroupWithStation(0, "Alpha Tankstelle", "Arbeitsweg");
            var listRequests = Server.ListRequests;

            EnableNearby();
            SelectRadio("Settings.Gps.Never");
            OpenHome("Arbeitsweg");
            WaitForText("Favorites.Station.Name", "Alpha Tankstelle");
            WaitForRefreshDone();

            Assert.False(Exists("Nearby.Heading"));
            Assert.False(Exists("Favorites.Station.Distance"));
            Assert.Equal(listRequests, Server.ListRequests);
        });
    }
}

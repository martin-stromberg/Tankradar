using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Die Startseite ruft die Preise der Favoriten beim Öffnen und auf Wunsch ab.
/// </summary>
public class HomeE2ETests_Refresh : HomeE2ETestBase
{
    /// <summary>
    /// Prüft den Abruf beim Öffnen und, dass „Preise aktualisieren“ geänderte Preise zeigt und einen weiteren Abruf auslöst.
    /// </summary>
    [Fact]
    public void OpenAndRefresh_FetchPricesByIds()
    {
        RunWithDiagnostics(() =>
        {
            CreateGroupWithStation(0, "Alpha Tankstelle", "Arbeitsweg");
            var before = Server.PricesRequests;

            OpenHome("Arbeitsweg");
            WaitUntil(() => Server.PricesRequests > before, "Beim Öffnen wurde kein Preisabruf gesendet.");
            WaitForRefreshDone();
            WaitForText("Favorites.Station.Price.SuperE5", "1,859 €/L");
            var afterOpen = Server.PricesRequests;
            Assert.Contains(MockTankerkoenigServer.StationAlpha, Server.LastPricesIds ?? string.Empty, StringComparison.Ordinal);

            Server.SetPrices(MockTankerkoenigServer.StationAlpha, 1.999m, 1.799m, 1.699m);
            Press("Favorites.Refresh");

            WaitForText("Favorites.Station.Price.SuperE5", "1,999 €/L");
            Assert.True(Server.PricesRequests > afterOpen);
        });
    }
}

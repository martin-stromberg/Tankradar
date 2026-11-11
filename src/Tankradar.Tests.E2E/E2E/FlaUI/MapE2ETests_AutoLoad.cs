using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Beim ersten Anzeigen der Karte lädt die App die Tankstellen am aktuellen Standort automatisch (Radius 5 km).
/// Späteres Anzeigen (Reiterwechsel, Rückkehr aus der Detailansicht) behält das Ergebnis ohne neue Anfrage;
/// bei der Standortnutzung „Nie“ unterbleibt die Startsuche still, und ein leeres Gebiet zeigt den Leerzustand ohne Wiederholung.
/// </summary>
public class MapE2ETests_AutoLoad : MapE2ETestBase
{
    /// <summary>
    /// Prüft, dass das erste Öffnen der Karte ohne Absenden Alpha und Beta lädt, die Anfrage den Radius 5 km sendet und der eigene Standort markiert ist.
    /// </summary>
    [Fact]
    public void OpeningMap_LoadsNearbyStationsWithoutSubmit()
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();

            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
            Assert.Equal("Mein Standort", WaitForAutomationId("Map.Origin").Name);
            Assert.True(Exists(AlphaMarker));
            Assert.True(Exists(BetaMarker));
            WaitForMapCount("2 von 2 Stationen sichtbar");
            Assert.Equal(1, Server.ListRequests);
            Assert.Equal(5, Server.LastListRadius);
            Assert.False(Exists("Search.EmptyState"));
        });
    }

    /// <summary>
    /// Prüft, dass der Wechsel auf einen anderen Reiter und zurück das geladene Ergebnis unverändert zeigt und keine neue Anfrage auslöst.
    /// </summary>
    [Fact]
    public void TabSwitch_PreservesResultWithoutNewRequest()
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();
            WaitForAutoSearch();
            var requests = Server.ListRequests;

            NavigateToTab("Tankbuch");
            WaitForPageTitle("Tankbuch");
            NavigateToTab("Karte");
            WaitForPageTitle("Karte");

            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
            Assert.Equal("2 Tankstellen", ReadPanelCount());
            Assert.Equal(requests, Server.ListRequests);
        });
    }

    /// <summary>
    /// Prüft, dass die Rückkehr aus der Detailansicht zur Karte das Ergebnis der Startsuche unverändert zeigt und keine neue Umkreisanfrage auslöst.
    /// </summary>
    [Fact]
    public void DetailReturn_PreservesResultWithoutNewRequest()
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();
            WaitForAutoSearch();
            var requests = Server.ListRequests;

            FindAllOrdered("Search.Station.Open")[1].Patterns.Invoke.Pattern.Invoke();
            WaitUntil(() => FindByAutomationId("Detail.Name")?.Name == "Beta Tankstelle", "Die Detailansicht von Beta wurde nicht geöffnet.");
            WaitForAutomationId("NavigationViewBackButton").Patterns.Invoke.Pattern.Invoke();

            WaitForPageTitle("Karte");
            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
            Assert.Equal(requests, Server.ListRequests);
        });
    }

    /// <summary>
    /// Prüft, dass bei der Standortnutzung „Nie“ das Öffnen der Karte weder eine Anfrage noch eine Meldung, einen Leerzustand oder Ergebnisse zeigt.
    /// </summary>
    [Fact]
    public void GpsNever_SkipsAutoSearchSilently()
    {
        RunWithDiagnostics(() =>
        {
            OpenSettings();
            SelectRadio("Settings.Gps.Never");
            OpenSearch();

            // Kurz warten, damit eine etwaig dennoch laufende Abfrage sichtbar würde; die Startsuche unterbleibt aber vollständig.
            Thread.Sleep(TimeSpan.FromSeconds(2));
            Assert.Equal(0, Server.TotalRequests);
            Assert.False(Exists("Search.Station.Name"));
            Assert.False(Exists("Search.EmptyState"));
            Assert.False(Exists("Search.StatusMessage"));
        });
    }

    /// <summary>
    /// Prüft, dass ein Gebiet ohne Tankstellen beim ersten Öffnen den Leerzustand zeigt und das erneute Anzeigen keine weitere Anfrage sendet.
    /// </summary>
    [Fact]
    public void EmptyArea_ShowsEmptyStateWithoutRepeatedRequest()
    {
        RunWithDiagnostics(() =>
        {
            RestartApplication(CreateEnvironment(
                Server.BaseUrl,
                "47.2,13.405",
                MockTankerkoenigServer.AcceptedKey,
                Geocoding.BaseUrl,
                Tiles.BaseUrl,
                Routing.BaseUrl,
                MockOpenRouteServiceServer.AcceptedKey));
            OpenSearch();

            WaitForAutomationId("Search.EmptyState");
            Assert.False(Exists("Search.Station.Name"));
            Assert.Equal(1, Server.ListRequests);

            NavigateToTab("Tankbuch");
            WaitForPageTitle("Tankbuch");
            NavigateToTab("Karte");
            WaitForPageTitle("Karte");

            WaitForAutomationId("Search.EmptyState");
            Assert.Equal(1, Server.ListRequests);
        });
    }
}

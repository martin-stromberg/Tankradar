namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Bei der Standortnutzung „Nie“ wird kein Standort verwendet; die Suche zeigt einen Hinweis und sendet keine Anfrage.
/// </summary>
public class SearchE2ETests_GpsNever : SearchE2ETestBase
{
    /// <summary>
    /// Prüft Hinweis, leere Liste und fehlende Anfrage an den Preisdienst.
    /// </summary>
    [Fact]
    public void GpsNever_ShowsHintAndSendsNoRequest()
    {
        RunWithDiagnostics(() =>
        {
            OpenSettings();
            SelectRadio("Settings.Gps.Never");
            OpenSearch();

            Submit();

            WaitForStatusMessage("Die Standortnutzung ist ausgeschaltet. Du kannst sie unter „Optionen“ ändern.");
            Assert.False(Exists("Search.Station.Name"));
            Assert.False(Exists("Search.EmptyState"));
            Assert.Equal(0, Server.TotalRequests);
        });
    }

    /// <summary>
    /// Prüft, dass nach dem Wechsel zurück auf „Nur bei Nutzung“ die Suche wieder funktioniert und der Hinweis verschwindet.
    /// </summary>
    [Fact]
    public void SwitchingBackToWhileInUse_SearchesAgain()
    {
        RunWithDiagnostics(() =>
        {
            OpenSettings();
            SelectRadio("Settings.Gps.Never");
            OpenSearch();
            Submit();
            WaitForStatusMessage("Die Standortnutzung ist ausgeschaltet. Du kannst sie unter „Optionen“ ändern.");

            SetGpsUsage("WhileInUse");
            Submit();

            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
            WaitUntil(() => !Exists("Search.StatusMessage"), "Der Hinweis zur Standortnutzung wurde nicht entfernt.");
        });
    }
}

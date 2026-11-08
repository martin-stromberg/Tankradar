namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Die Detailansicht bietet „Navigation starten“; im Testmodus wird nie eine externe Karten-App gestartet.
/// </summary>
public class DetailE2ETests_Navigate : SearchE2ETestBase
{
    /// <summary>
    /// Prüft, dass die Schaltfläche „Navigation starten“ sichtbar und bedienbar ist und der Klick die App auf der Detailseite belässt.
    /// </summary>
    [Fact]
    public void NavigateButton_VisibleAndTappable_StaysOnDetailPage()
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();
            Submit();
            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
            FindAllOrdered("Search.Station.Open")[0].Patterns.Invoke.Pattern.Invoke();
            WaitForAutomationId("Detail.Address");

            var button = WaitForAutomationId("Detail.Navigate");
            Assert.True(button.IsEnabled);

            button.Patterns.Invoke.Pattern.Invoke();

            // Testmodus: Es wird nie eine externe Karten-App gestartet — die Detailansicht bleibt unverändert sichtbar.
            Assert.Equal("Alpha Tankstelle", WaitForAutomationId("Detail.Name").Name);
            Assert.True(Exists("Detail.Navigate"));
        });
    }
}

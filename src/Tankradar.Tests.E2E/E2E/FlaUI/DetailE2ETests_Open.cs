using System.Text.RegularExpressions;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Aus der Ergebnisliste lässt sich die Detailansicht einer Tankstelle öffnen; fehlende Angaben der Quelle werden weggelassen.
/// </summary>
public class DetailE2ETests_Open : SearchE2ETestBase
{
    private void SearchAndOpen(int index, string expectedName)
    {
        OpenSearch();
        Submit();
        WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
        FindAllOrdered("Search.Station.Open")[index].Patterns.Invoke.Pattern.Invoke();
        WaitUntil(() => MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("Detail.Name"))?.Name == expectedName, $"Die Detailansicht zeigt nicht '{expectedName}'.");
    }

    /// <summary>
    /// Prüft Name, Adresse, Entfernung, Preise mit Alter, Hinweis „Automatentankstelle“ und Öffnungszeiten der ersten Tankstelle (Details vom Mock-Preisdienst).
    /// </summary>
    [Fact]
    public void OpenDetail_FromList_ShowsStationDetails()
    {
        RunWithDiagnostics(() =>
        {
            SearchAndOpen(0, "Alpha Tankstelle");

            WaitUntil(() => Exists("Detail.OpeningHours"), "Die Öffnungszeiten erscheinen nicht.");
            Assert.Equal("Hauptstraße 1, 10115 Berlin", WaitForAutomationId("Detail.Address").Name);
            Assert.EndsWith(" km", WaitForAutomationId("Detail.Distance").Name, StringComparison.Ordinal);
            Assert.Equal("1,859 Euro pro Liter", WaitForAutomationId("Detail.Price.SuperE5").Name);
            Assert.Equal("1,799 Euro pro Liter", WaitForAutomationId("Detail.Price.SuperE10").Name);
            Assert.Equal("1,699 Euro pro Liter", WaitForAutomationId("Detail.Price.Diesel").Name);
            Assert.Matches(new Regex(@"^vor \d+ Min\.$"), WaitForAutomationId("Detail.Age.SuperE5").Name);
            Assert.Equal("Automat 24/7", WaitForAutomationId("Detail.Hint.Automated").Name);
            Assert.Equal("Live-Preise", WaitForAutomationId("Detail.PriceStatus").Name);
            Assert.False(Exists("Detail.Back"));
            Assert.Equal("Mo-So: 00:00 – 24:00 Uhr", WaitForAutomationId("Detail.OpeningHours").Name);
            Assert.Matches(new Regex(@"^Stand: vor \d+ Min\.$"), WaitForAutomationId("Detail.OpeningHoursAge").Name);
            Assert.False(Exists("Detail.OfflineBanner"));
            Assert.Equal(1, Server.DetailRequests);
        });
    }

    /// <summary>
    /// Prüft, dass Angaben, die die Quelle nicht liefert (kein Diesel-Preis bei Beta), weggelassen werden, und dass der Zurück-Pfeil der Kopfleiste zur Ergebnisliste führt.
    /// </summary>
    [Fact]
    public void OpenDetail_MissingSourceData_IsOmittedAndBackReturnsToList()
    {
        RunWithDiagnostics(() =>
        {
            SearchAndOpen(1, "Beta Tankstelle");

            WaitForAutomationId("Detail.Price.SuperE5");
            Assert.False(Exists("Detail.Price.Diesel"));
            Assert.False(Exists("Detail.Hint.Automated"));
            Assert.Equal("Mo-Fr: 06:00 – 22:00 Uhr", WaitForAutomationId("Detail.OpeningHours").Name);

            // Die Rückkehr bietet die Kopfleiste der Shell (Zurück-Pfeil); die Seite hat keine eigene Schaltfläche.
            WaitForAutomationId("NavigationViewBackButton").Patterns.Invoke.Pattern.Invoke();

            WaitForAutomationId("MapPage.Headline");
            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
        });
    }
}

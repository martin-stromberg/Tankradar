namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test der Barrierefreiheit des Bereichs „Export“: Der UIA-Name von Chips und Schaltfläche entspricht dem sichtbaren Text, die Statusmeldung ist erreichbar.
/// </summary>
public class TankbookExportE2ETests_Accessibility : TankbookExportE2ETestBase
{
    /// <summary>
    /// Prüft Namen (sichtbarer Text), Hinweis der Schaltfläche, Überschrift und die erreichbare Statusmeldung nach dem Export.
    /// </summary>
    [Fact]
    public void ExportArea_ElementsHaveVisibleTextAsNameAndReachableStatus()
    {
        RunWithDiagnostics(() =>
        {
            OpenEntriesOfNewVehicle("Golf");
            Tankbook.CreateEntry("Alpha Tankstelle", "40", "1,859", string.Empty, "10500");

            Assert.Equal("Export", WaitForAutomationId("TankbookEntries.Export.Heading").Name);
            var expected = new Dictionary<string, string>
            {
                ["Format.csv"] = "CSV",
                ["Format.pdf"] = "PDF",
                ["Range.all"] = "Alle Vorgänge",
                ["Range.thisYear"] = "Dieses Jahr",
                ["Range.lastYear"] = "Letztes Jahr",
                ["Range.last12Months"] = "Letzte 12 Monate",
                ["Range.custom"] = "Eigener Zeitraum",
                ["Start"] = "Exportieren",
            };
            foreach (var (key, name) in expected)
            {
                Assert.Equal(name, WaitForAutomationId($"TankbookEntries.Export.{key}").Name);
            }

            Assert.Equal("Fahrzeug „Golf“, CSV, Alle Vorgänge", WaitForAutomationId("TankbookEntries.Export.Start").Properties.HelpText.ValueOrDefault);
            Assert.True(IsChosen("TankbookEntries.Export.Format.csv"));
            Assert.False(IsChosen("TankbookEntries.Export.Format.pdf"));

            ExportAndWaitForFile();

            var status = WaitForAutomationId("TankbookEntries.Export.Status");
            Assert.StartsWith(ReadyPrefix, status.Name, StringComparison.Ordinal);
        });
    }
}

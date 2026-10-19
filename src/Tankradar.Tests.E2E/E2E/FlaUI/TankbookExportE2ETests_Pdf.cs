using System.Text;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test des PDF-Exports: Der Bericht entsteht unter dem Windows-Build und enthält Fahrzeug, Kennzahlen und Einträge.
/// </summary>
public class TankbookExportE2ETests_Pdf : TankbookExportE2ETestBase
{
    /// <summary>
    /// Prüft Dateiaufbau und Inhalt des PDF-Berichts (Kopf, Abschluss, Fahrzeugname, Durchschnittsverbrauch, Gesamtkosten, Einträge, Seitenzahl).
    /// </summary>
    [Fact]
    public void Export_Pdf_ContainsVehicleKeyFiguresAndEntries()
    {
        RunWithDiagnostics(() =>
        {
            OpenEntriesOfNewVehicle("Golf");
            Tankbook.CreateEntry("Müller Süd", "40", "1,859", string.Empty, "10500");
            Tankbook.CreateEntry("Alpha Tankstelle", "30", "1,859", string.Empty, "11000");

            ChooseFormat("pdf");
            Assert.False(IsChosen("TankbookEntries.Export.Format.csv"));
            var path = ExportAndWaitForFile();

            Assert.Matches(@"^Tankbuch_Golf_\d{4}-\d{2}-\d{2}\.pdf$", Path.GetFileName(path));
            var text = Encoding.Latin1.GetString(ReadFile(path));
            Assert.StartsWith("%PDF-", text, StringComparison.Ordinal);
            Assert.EndsWith("%%EOF\n", text, StringComparison.Ordinal);
            Assert.Contains("/Title (Tankbuch Golf)", text, StringComparison.Ordinal);
            Assert.Contains("(Golf ", text, StringComparison.Ordinal);
            Assert.Contains("(6,00 L/100 km)", text, StringComparison.Ordinal);
            Assert.Contains("(130,13 \\200)", text, StringComparison.Ordinal);
            Assert.Contains("(M\\374ller S\\374d)", text, StringComparison.Ordinal);
            Assert.Contains("(Alpha Tankstelle)", text, StringComparison.Ordinal);
            Assert.Contains("(Seite 1 von 1)", text, StringComparison.Ordinal);
            Assert.StartsWith(ReadyPrefix + "Golf_", ReadStatus(), StringComparison.Ordinal);
            Assert.EndsWith(".pdf.", ReadStatus(), StringComparison.Ordinal);
        });
    }
}

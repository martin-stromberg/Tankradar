using System.Text.RegularExpressions;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test des CSV-Exports: Er entsteht nur auf Veranlassung, legt im Testmodus eine Datei statt eines Teilen-Dialogs ab und ist für deutsche Tabellenkalkulationen aufgebaut.
/// </summary>
public class TankbookExportE2ETests_Csv : TankbookExportE2ETestBase
{
    /// <summary>
    /// Prüft, dass das Öffnen und Verlassen der Seite keine Datei erzeugt, dass CSV und alle Vorgänge voreingestellt sind und dass <b>Exportieren</b> eine Datei mit
    /// BOM, Kopfzeile, Semikolon, Dezimalkomma, Umlauten und Einheit Liter ablegt.
    /// </summary>
    [Fact]
    public void Export_Csv_OnlyOnRequest_WritesGermanSpreadsheetFile()
    {
        RunWithDiagnostics(() =>
        {
            OpenEntriesOfNewVehicle("Golf");
            Tankbook.CreateEntry("Müller Süd", "40", "1,859", string.Empty, "10500");
            Tankbook.CreateEntry("Alpha Tankstelle", "30", "1,859", string.Empty, "11000");

            ReopenEntries("Golf");
            Assert.Empty(ExportFiles());
            Assert.False(Directory.Exists(SharedDirectory));
            Assert.True(IsChosen("TankbookEntries.Export.Format.csv"));
            Assert.True(IsChosen("TankbookEntries.Export.Range.all"));
            Assert.False(Exists("TankbookEntries.Export.From"));
            Assert.Equal(string.Empty, ReadStatus());

            var path = ExportAndWaitForFile();

            var lines = ReadCsvLines(path);
            Assert.Matches(@"^Tankbuch_Golf_\d{4}-\d{2}-\d{2}\.csv$", Path.GetFileName(path));
            Assert.Equal(CsvHeader, lines[0]);
            Assert.Equal(3, lines.Length);
            Assert.Matches(new Regex(@"^\d{2}\.\d{2}\.\d{4};\d{2}:\d{2};Müller Süd;Kraftstoff;40,00;Liter;1,859;74,36;10500;Nein;500;8,00;L/100 km;ab Startstand;$"), lines[1]);
            Assert.Matches(new Regex(@"^\d{2}\.\d{2}\.\d{4};\d{2}:\d{2};Alpha Tankstelle;Kraftstoff;30,00;Liter;1,859;55,77;11000;Nein;500;6,00;L/100 km;;$"), lines[2]);
            Assert.StartsWith(ReadyPrefix + "Golf_", ReadStatus(), StringComparison.Ordinal);
            Assert.EndsWith(".csv.", ReadStatus(), StringComparison.Ordinal);
            WaitForAutomationId("TankbookEntries.Export.Start");
        });
    }
}

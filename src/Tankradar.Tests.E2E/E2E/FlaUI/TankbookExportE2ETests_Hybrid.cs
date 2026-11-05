namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test des Exports eines Hybridfahrzeugs: Kraftstoff- und Stromvorgänge tragen ihre Einheit Liter bzw. kWh.
/// </summary>
public class TankbookExportE2ETests_Hybrid : TankbookExportE2ETestBase
{
    /// <summary>
    /// Prüft, dass die CSV-Datei Zeilen mit „Liter“ und Zeilen mit „kWh“ enthält, jede mit ihrer Art und Verbrauchseinheit.
    /// </summary>
    [Fact]
    public void Export_Hybrid_CsvContainsLiterAndKwhRows()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Plug-in", "Hybrid", "1000", string.Empty);
            var start = DateTime.Now.AddDays(-10);
            TankbookTestDatabase.AddEntry(DatabasePath, "Plug-in", start, 1500, 40m, 74.36m);
            TankbookTestDatabase.AddEntry(DatabasePath, "Plug-in", start.AddDays(1), 1600, 20m, 8m, kind: "Electricity");
            TankbookTestDatabase.AddEntry(DatabasePath, "Plug-in", start.AddDays(5), 2000, 30m, 55.77m);
            TankbookTestDatabase.AddEntry(DatabasePath, "Plug-in", start.AddDays(6), 2100, 10m, 4m, kind: "Electricity");
            Tankbook.OpenEntries("Plug-in");

            var lines = ReadCsvLines(ExportAndWaitForFile());

            Assert.Equal(5, lines.Length);
            Assert.Equal(2, lines.Count(line => line.Contains(";Kraftstoff;", StringComparison.Ordinal) && line.Contains(";Liter;", StringComparison.Ordinal)));
            Assert.Equal(2, lines.Count(line => line.Contains(";Strom;", StringComparison.Ordinal) && line.Contains(";kWh;", StringComparison.Ordinal)));
            Assert.Contains(lines, line => line.Contains(";Strom;10,00;kWh;0,40;4,00;2100;Nein;500;2,00;kWh/100 km;;", StringComparison.Ordinal));
            Assert.StartsWith(ReadyPrefix + "Plug-in_", ReadStatus(), StringComparison.Ordinal);
        });
    }
}

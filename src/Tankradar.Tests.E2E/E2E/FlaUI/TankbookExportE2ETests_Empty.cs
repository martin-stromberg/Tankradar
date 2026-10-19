namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test des Exports ohne Vorgänge: verständliche Rückmeldung, keine Datei.
/// </summary>
public class TankbookExportE2ETests_Empty : TankbookExportE2ETestBase
{
    /// <summary>
    /// Prüft, dass ein Fahrzeug ohne Vorgänge die Rückmeldung „noch keine Vorgänge“ zeigt und keine Datei entsteht (weder CSV noch PDF).
    /// </summary>
    [Fact]
    public void Export_VehicleWithoutEntries_ShowsMessageAndCreatesNoFile()
    {
        RunWithDiagnostics(() =>
        {
            OpenEntriesOfNewVehicle("Golf");

            StartExport();
            WaitForExportStatus("Für dieses Fahrzeug gibt es noch keine Vorgänge zum Exportieren.");
            ChooseFormat("pdf");
            StartExport();
            WaitForExportStatus("Für dieses Fahrzeug gibt es noch keine Vorgänge zum Exportieren.");

            Assert.Empty(ExportFiles());
            Assert.False(Directory.Exists(SharedDirectory));
        });
    }

    /// <summary>
    /// Prüft, dass ein Zeitraum ohne Vorgänge die Rückmeldung „Im gewählten Zeitraum …“ zeigt und keine Datei entsteht.
    /// </summary>
    [Fact]
    public void Export_RangeWithoutEntries_ShowsRangeMessageAndCreatesNoFile()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "10000", string.Empty);
            TankbookTestDatabase.AddEntry(DatabasePath, "Golf", new DateTime(DateTime.Now.Year - 3, 5, 5, 8, 0, 0), 10500, 40m, 74.36m);
            Tankbook.OpenEntries("Golf");

            ChooseRange("lastYear");
            StartExport();

            WaitForExportStatus("Im gewählten Zeitraum gibt es keine Vorgänge.");
            Assert.Empty(ExportFiles());
        });
    }
}

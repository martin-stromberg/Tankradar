namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test der Auswertung eines Hybrid-Fahrzeugs: Kraftstoff und Strom haben getrennte Blöcke mit eigenen Einheiten.
/// </summary>
public class TankbookStatisticsE2ETests_Hybrid : TankbookE2ETestBase
{
    /// <summary>
    /// Prüft zwei Blöcke: 30 L auf 500 km (6,00 L/100 km) und 20 kWh auf 100 km (20,00 kWh/100 km), je mit eigenen Kosten.
    /// </summary>
    [Fact]
    public void HybridVehicle_ShowsSeparateBlocksPerKind()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Auris", "Hybrid", "10000", string.Empty);
            var start = DateTime.Now.AddDays(-5);
            TankbookTestDatabase.AddEntry(DatabasePath, "Auris", start, 10_500, 40m, 74.36m);
            TankbookTestDatabase.AddEntry(DatabasePath, "Auris", start.AddDays(1), 10_600, 15m, 8.85m, kind: "Electricity");
            TankbookTestDatabase.AddEntry(DatabasePath, "Auris", start.AddDays(2), 10_700, 20m, 11.80m, kind: "Electricity");
            TankbookTestDatabase.AddEntry(DatabasePath, "Auris", start.AddDays(3), 11_000, 30m, 55.77m);

            Tankbook.OpenEntries("Auris");

            Tankbook.WaitForStatistics("Fuel", "6,00 L/100 km", "11,15 € / 100 km", "130,13 €");
            Tankbook.WaitForStatistics("Electricity", "20,00 kWh/100 km", "11,80 € / 100 km", "20,65 €");
            Tankbook.WaitForStatistic("Fuel", "Heading", "Kraftstoff");
            Tankbook.WaitForStatistic("Electricity", "Heading", "Strom");
            Tankbook.WaitForStatistic("Electricity", "ConsumptionChartSummary", "Verlauf, 1 Wert, zuletzt 20,00 kWh/100 km, niedrigster 20,00, höchster 20,00");
        });
    }
}

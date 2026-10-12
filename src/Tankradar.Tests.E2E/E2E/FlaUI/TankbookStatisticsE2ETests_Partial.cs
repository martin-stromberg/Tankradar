namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test der Auswertung mit Teilbetankung: Menge und Kosten der Teilbetankung zwischen zwei Vollvorgängen fließen in Durchschnitt und Kosten pro 100 km ein.
/// </summary>
public class TankbookStatisticsE2ETests_Partial : TankbookE2ETestBase
{
    /// <summary>
    /// Prüft (20 L + 10 L) auf 500 km = 6,00 L/100 km und (37,18 € + 18,59 €) auf 500 km = 11,15 € pro 100 km; die Gesamtkosten enthalten alle drei Vorgänge.
    /// </summary>
    [Fact]
    public void PartialBetweenFullEntries_CountsQuantityAndCost()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Petrol", "10000", string.Empty);
            Tankbook.OpenEntries("Golf");

            Tankbook.CreateEntry("Alpha Tankstelle", "40", "1,859", string.Empty, "10500");
            Tankbook.CreateEntry("Alpha Tankstelle", "10", "1,859", string.Empty, "10800", isPartial: true);
            Tankbook.CreateEntry("Alpha Tankstelle", "20", "1,859", string.Empty, "11000");

            Tankbook.WaitForStatistics("Fuel", "6,00 L/100 km", "11,15 € / 100 km", "130,13 €");
            Tankbook.WaitForStatistic("Fuel", "ConsumptionChartSummary", "Verlauf, 1 Wert, zuletzt 6,00 L/100 km, niedrigster 6,00, höchster 6,00");
        });
    }
}

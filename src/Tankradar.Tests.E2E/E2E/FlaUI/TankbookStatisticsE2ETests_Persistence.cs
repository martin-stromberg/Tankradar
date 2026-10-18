namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test der Auswertung: Sie wird beim Lesen berechnet und ist nach einem Neustart der App unverändert vorhanden.
/// </summary>
public class TankbookStatisticsE2ETests_Persistence : TankbookE2ETestBase
{
    /// <summary>
    /// Prüft Kennzahlen und Zusammenfassungen vor und nach dem Neustart.
    /// </summary>
    [Fact]
    public void Statistics_SurviveApplicationRestart()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "10000", string.Empty);
            Tankbook.OpenEntries("Golf");
            Tankbook.CreateEntry("Alpha Tankstelle", "40", "1,859", string.Empty, "10500");
            Tankbook.CreateEntry("Alpha Tankstelle", "30", "1,859", string.Empty, "11000");
            Tankbook.WaitForStatistics("Fuel", "6,00 L/100 km", "11,15 €", "130,13 €");

            RestartApplication();
            OpenTankbook();
            Tankbook.OpenEntries("Golf");

            Tankbook.WaitForStatistics("Fuel", "6,00 L/100 km", "11,15 €", "130,13 €");
            Tankbook.WaitForStatistic("Fuel", "ConsumptionChartSummary", "Verlauf, 2 Werte, zuletzt 6,00 L/100 km, niedrigster 6,00, höchster 8,00, ältester Wert ab Startstand berechnet");
            Tankbook.WaitForStatistic("Fuel", "CostChartSummary", "Monatskosten, 1 Wert, zuletzt 130,13 €, niedrigster 130,13 €, höchster 130,13 €");
        });
    }
}

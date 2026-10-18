using System.Globalization;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test der Auswertung: Zwei Vollvorgänge über das Formular ergeben Durchschnittsverbrauch, Kosten pro 100 km, Monatskarte, Monatskosten und Diagramm-Zusammenfassungen.
/// </summary>
public class TankbookStatisticsE2ETests_Flow : TankbookE2ETestBase
{
    /// <summary>
    /// Prüft die Kennzahlen nach zwei Vollvorgängen: 30 L auf 500 km ergeben 6,00 L/100 km und 11,15 € pro 100 km; die Monatskosten stehen im aktuellen Monat.
    /// </summary>
    [Fact]
    public void TwoFullEntries_ShowAverageCostAndMonth()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "10000", string.Empty);
            Tankbook.OpenEntries("Golf");
            Assert.Equal("Auswertung", WaitForAutomationId("TankbookEntries.Statistics.Heading").Name);
            Tankbook.WaitUntil(
                () => Tankbook.Find("TankbookEntries.Statistics.Notice")?.Name == "Noch keine Auswertung: Sie erscheint, sobald Tank- oder Ladevorgänge erfasst sind.",
                "Der Hinweis ohne Vorgänge fehlt.");

            Tankbook.CreateEntry("Alpha Tankstelle", "40", "1,859", string.Empty, "10500");
            Tankbook.CreateEntry("Alpha Tankstelle", "30", "1,859", string.Empty, "11000");

            Tankbook.WaitForStatistics("Fuel", "6,00 L/100 km", "11,15 €", "130,13 €");
            Tankbook.WaitForStatistic("Fuel", "Heading", "Kraftstoff");
            var monthName = DateTime.Now.ToString("MMMM", CultureInfo.GetCultureInfo("de-DE"));
            Tankbook.WaitForStatistic("Fuel", "MonthAnalysisHeading", $"Monatsanalyse {DateTime.Now.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("de-DE"))}");
            Tankbook.WaitForStatistic("Fuel", "MonthCostLabel", $"Gesamtkosten {monthName}");
            Tankbook.WaitForStatistic("Fuel", "MonthQuantity", "70,00 L getankt");
            Tankbook.WaitForStatistic("Fuel", "AverageSource", "Berechnet aus vollen Vorgängen");
            Tankbook.WaitForStatistic("Fuel", "ConsumptionChartSummary", "Verlauf, 2 Werte, zuletzt 6,00 L/100 km, niedrigster 6,00, höchster 8,00");
            Tankbook.WaitForStatistic("Fuel", "CostChartSummary", "Monatskosten, 1 Wert, zuletzt 130,13 €, niedrigster 130,13 €, höchster 130,13 €");
            var month = DateTime.Now.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("de-DE"));
            Tankbook.WaitForTexts("TankbookEntries.Statistics.Fuel.Month", $"{month}: 130,13 €");
            Assert.False(Exists("TankbookEntries.Statistics.Notice"));
            Assert.False(Exists("TankbookEntries.Statistics.Fuel.Notice"));
            Assert.False(Exists("TankbookEntries.Statistics.Electricity.Heading"));
            WaitForAutomationId("TankbookEntries.Statistics.Fuel.ConsumptionChart");
            WaitForAutomationId("TankbookEntries.Statistics.Fuel.CostChart");
        });
    }
}

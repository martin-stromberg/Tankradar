namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test der Auswertung bei zu wenigen Vollvorgängen: ohne Vorgänge, ohne vollen Vorgang (nur Teilbetankung) mit und ohne Startverbrauch, bei einem Hybrid,
/// sowie mit einem einzigen vollen Vorgang (Wert ab Startstand); ohne berechneten Verbrauch gibt es kein Verbrauchsdiagramm.
/// </summary>
public class TankbookStatisticsE2ETests_Fallback : TankbookE2ETestBase
{
    /// <summary>
    /// Prüft den Hinweis ohne Vorgänge, den Hinweis „mindestens ein voller Vorgang“, den gekennzeichneten Startverbrauch und den Wert ab Startstand.
    /// </summary>
    [Fact]
    public void TooFewEntries_ShowNoticeOrStartConsumption()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Eins", "Diesel", "10000", string.Empty);
            CreateVehicle("Leer", "Diesel", "10000", string.Empty);
            CreateVehicle("Start", "Diesel", "10000", "5,4");
            CreateVehicle("Plugin", "Hybrid", "10000", "5,4");
            CreateVehicle("AbStart", "Diesel", "10000", "5,4");
            var yesterday = DateTime.Now.AddDays(-1);
            TankbookTestDatabase.AddEntry(DatabasePath, "Eins", yesterday, 10_500, 40m, 74.36m, isPartial: true);
            TankbookTestDatabase.AddEntry(DatabasePath, "Start", yesterday, 10_500, 40m, 74.36m, isPartial: true);
            TankbookTestDatabase.AddEntry(DatabasePath, "AbStart", yesterday, 10_500, 40m, 74.36m);
            TankbookTestDatabase.AddEntry(DatabasePath, "Plugin", yesterday, 10_500, 20m, 11.80m, kind: "Electricity");

            Tankbook.OpenEntries("Leer");
            Tankbook.WaitUntil(
                () => Tankbook.Find("TankbookEntries.Statistics.Notice")?.Name == "Noch keine Auswertung: Sie erscheint, sobald Tank- oder Ladevorgänge erfasst sind.",
                "Der Hinweis ohne Vorgänge fehlt.");
            Assert.False(Exists("TankbookEntries.Statistics.Fuel.Average"));
            Tankbook.OpenEntries("Eins");
            Tankbook.WaitForStatistic("Fuel", "Average", "–");
            Tankbook.WaitForStatistic("Fuel", "CostPer100KmUnit", "noch nicht berechenbar");
            Assert.StartsWith("Für den Verbrauch wird mindestens ein voller Vorgang benötigt.", WaitForAutomationId("TankbookEntries.Statistics.Fuel.Notice").Name, StringComparison.Ordinal);
            Assert.Contains("Startverbrauch", WaitForAutomationId("TankbookEntries.Statistics.Fuel.Notice").Name, StringComparison.Ordinal);
            Assert.False(Exists("TankbookEntries.Statistics.Fuel.AverageSource"));
            Assert.False(Exists("TankbookEntries.Statistics.Fuel.ConsumptionChart"), "Ohne berechneten Verbrauch darf es kein (leeres) Verbrauchsdiagramm geben.");
            WaitForAutomationId("TankbookEntries.Statistics.Fuel.CostChart");
            WaitForAutomationId("TankbookEntries.Statistics.Fuel.ConsumptionChartSummary");
            Tankbook.OpenEntries("Start");
            Tankbook.WaitForStatistic("Fuel", "Average", "5,40");
            Tankbook.WaitForStatistic("Fuel", "AverageUnit", "L/100 km");
            Tankbook.WaitForStatistic("Fuel", "AverageSource", "Startverbrauch – noch keine Strecke aus vollen Vorgängen");
            Tankbook.WaitForStatistic("Fuel", "CostPer100KmUnit", "noch nicht berechenbar");
            Assert.False(Exists("TankbookEntries.Statistics.Fuel.Notice"));
            Assert.False(Exists("TankbookEntries.Statistics.Fuel.ConsumptionChart"));
            Tankbook.OpenEntries("AbStart");
            Tankbook.WaitForStatistic("Fuel", "Average", "8,00");
            Tankbook.WaitForStatistic("Fuel", "AverageUnit", "L/100 km");
            Tankbook.WaitForStatistic("Fuel", "AverageSource", "Ab Startstand – angenommen wird ein voller Tank beim Start");
            Tankbook.WaitForStatistic("Fuel", "CostPer100Km", "14,87");
            Assert.False(Exists("TankbookEntries.Statistics.Fuel.Notice"));
            WaitForAutomationId("TankbookEntries.Statistics.Fuel.ConsumptionChart");
            Tankbook.OpenEntries("Plugin");
            var notice = WaitForAutomationId("TankbookEntries.Statistics.Electricity.Notice").Name;
            Assert.Equal("Für den Verbrauch werden mindestens zwei volle Vorgänge benötigt.", notice);
            Assert.False(Exists("TankbookEntries.Statistics.Electricity.ConsumptionChart"));
        });
    }
}

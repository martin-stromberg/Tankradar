namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test der Auswertung bei zu wenigen Vollvorgängen: ohne Vorgänge, mit einem Vorgang ohne und mit Startverbrauch.
/// </summary>
public class TankbookStatisticsE2ETests_Fallback : TankbookE2ETestBase
{
    /// <summary>
    /// Prüft den Hinweis ohne Vorgänge, den Hinweis „mindestens zwei volle Vorgänge“ und den gekennzeichneten Startverbrauch.
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
            var yesterday = DateTime.Now.AddDays(-1);
            TankbookTestDatabase.AddEntry(DatabasePath, "Eins", yesterday, 10_500, 40m, 74.36m);
            TankbookTestDatabase.AddEntry(DatabasePath, "Start", yesterday, 10_500, 40m, 74.36m);

            Tankbook.OpenEntries("Leer");
            Tankbook.WaitUntil(
                () => Tankbook.Find("TankbookEntries.Statistics.Notice")?.Name == "Noch keine Auswertung: Sie erscheint, sobald Tank- oder Ladevorgänge erfasst sind.",
                "Der Hinweis ohne Vorgänge fehlt.");
            Assert.False(Exists("TankbookEntries.Statistics.Fuel.Average"));
            Tankbook.LeaveEntries();

            Tankbook.OpenEntries("Eins");
            Tankbook.WaitForStatistic("Fuel", "Average", "–");
            Tankbook.WaitForStatistic("Fuel", "CostPer100Km", "noch nicht berechenbar");
            Assert.StartsWith("Für den Verbrauch werden mindestens zwei volle Vorgänge benötigt.", WaitForAutomationId("TankbookEntries.Statistics.Fuel.Notice").Name, StringComparison.Ordinal);
            Assert.False(Exists("TankbookEntries.Statistics.Fuel.AverageSource"));
            Tankbook.LeaveEntries();

            Tankbook.OpenEntries("Start");
            Tankbook.WaitForStatistic("Fuel", "Average", "5,40 L/100 km");
            Tankbook.WaitForStatistic("Fuel", "AverageSource", "Startverbrauch – noch keine zwei vollen Vorgänge");
            Tankbook.WaitForStatistic("Fuel", "CostPer100Km", "noch nicht berechenbar");
            Assert.False(Exists("TankbookEntries.Statistics.Fuel.Notice"));
        });
    }
}

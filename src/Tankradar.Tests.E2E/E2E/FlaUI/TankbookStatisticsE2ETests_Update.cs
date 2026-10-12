namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test der Auswertung: Korrigieren und Löschen eines Vorgangs ändern die Werte; nach dem Löschen fällt die Auswertung auf den Hinweis zurück.
/// </summary>
public class TankbookStatisticsE2ETests_Update : TankbookE2ETestBase
{
    /// <summary>
    /// Prüft die Aktualisierung nach Korrigieren (1.000 km statt 500 km) und Löschen.
    /// </summary>
    [Fact]
    public void EditAndDeleteEntry_UpdatesStatistics()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "10000", string.Empty);
            Tankbook.OpenEntries("Golf");
            Tankbook.CreateEntry("Alpha Tankstelle", "40", "1,859", string.Empty, "10500");
            Tankbook.CreateEntry("Alpha Tankstelle", "30", "1,859", string.Empty, "11000");
            Tankbook.WaitForStatistics("Fuel", "6,00 L/100 km", "11,15 €", "130,13 €");

            Tankbook.PressForEntry("TankbookEntry.Item.Edit", 0);
            Type("TankbookEntries.Form.Odometer", "11500");
            Tankbook.SaveEntryForm();

            Tankbook.WaitForStatistics("Fuel", "3,00 L/100 km", "5,58 €", "130,13 €");

            Tankbook.PressForEntry("TankbookEntry.Item.Delete", 0);
            Press("TankbookEntry.Item.DeleteConfirm");

            Tankbook.WaitForStatistic("Fuel", "Average", "–");
            Tankbook.WaitForStatistic("Fuel", "CostPer100KmUnit", "noch nicht berechenbar");
            Tankbook.WaitForStatistic("Fuel", "MonthCost", "74,36 €");
            Assert.StartsWith("Für den Verbrauch werden mindestens zwei volle Vorgänge benötigt.", WaitForAutomationId("TankbookEntries.Statistics.Fuel.Notice").Name, StringComparison.Ordinal);
        });
    }
}

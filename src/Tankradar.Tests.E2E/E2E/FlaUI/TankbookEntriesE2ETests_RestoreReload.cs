namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test (H5): Eine geöffnete Seite „Tank- und Ladevorgänge“ zeigt nach einer Wiederherstellung in den Optionen den wiederhergestellten Stand,
/// ohne dass sie neu geöffnet werden muss.
/// </summary>
public class TankbookEntriesE2ETests_RestoreReload : BackupE2ETestBase
{
    /// <summary>
    /// Prüft, dass Liste und Auswertung der geöffneten Vorgangsseite nach „Sicherung wiederherstellen“ wieder dem Stand der Sicherung entsprechen.
    /// </summary>
    [Fact]
    public void Restore_WhileEntriesPageIsOpen_ShowsRestoredState()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "10000", string.Empty);
            Tankbook.OpenEntries("Golf");
            Tankbook.CreateEntry("Alpha Tankstelle", "40", "1,859", string.Empty, "10500");
            Tankbook.WaitForStatistic("Fuel", "Average", "8,00");

            OpenBackup();
            CreateBackup();

            NavigateToTab("Tankbuch", "TankbookEntries.Add");
            Tankbook.CreateEntry("Beta Tankstelle", "30", "1,859", string.Empty, "11000");
            Tankbook.WaitForTexts("TankbookEntry.Item.Station", "Beta Tankstelle", "Alpha Tankstelle");
            Tankbook.WaitForStatistic("Fuel", "Average", "6,00");

            OpenBackup();
            RestoreBackup(1);

            NavigateToTab("Tankbuch", "TankbookEntries.Add");
            Tankbook.WaitForTexts("TankbookEntry.Item.Station", "Alpha Tankstelle");
            Tankbook.WaitForStatistic("Fuel", "Average", "8,00");
        });
    }

    /// <summary>
    /// Prüft, dass die geöffnete Vorgangsseite schließt, wenn das Fahrzeug in der wiederhergestellten Sicherung fehlt (ADR 0013), obwohl beim Wiederherstellen
    /// der Reiter „Optionen“ aktiv war; die Fahrzeugliste zeigt danach nur die gesicherten Fahrzeuge.
    /// </summary>
    [Fact]
    public void Restore_WithoutOpenVehicle_ClosesEntriesPage()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "10000", string.Empty);

            OpenBackup();
            CreateBackup();

            NavigateToTab("Tankbuch", "Tankbook.AddVehicle");
            CreateVehicle("Neu nach Sicherung", "Diesel", "20000", string.Empty);
            Tankbook.OpenEntries("Neu nach Sicherung");
            WaitForAutomationId("TankbookEntries.Add");

            OpenBackup();
            RestoreBackup(1);

            NavigateToTab("Tankbuch", "Tankbook.AddVehicle");
            WaitUntil(() => !Exists("TankbookEntries.Add") && !Exists("TankbookEntries.Export.Start"), "Die Vorgangsseite des nicht mehr vorhandenen Fahrzeugs wurde nicht geschlossen.");
            WaitForTexts("Tankbook.Vehicle.Name", "Golf");
        });
    }
}

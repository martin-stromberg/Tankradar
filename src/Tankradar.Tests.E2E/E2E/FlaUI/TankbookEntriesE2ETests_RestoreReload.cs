namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test (H5): Der Tab „Tankbuch“ zeigt nach einer Wiederherstellung in den Optionen den wiederhergestellten Stand, ohne dass er neu geöffnet werden muss;
/// die gemerkte Fahrzeugauswahl wird dabei verworfen (erstes Fahrzeug nach Name).
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
    /// Prüft, dass der Tab nach der Wiederherstellung das gesicherte Fahrzeug zeigt, wenn das gewählte Fahrzeug in der Sicherung fehlt (ADR 0013), obwohl beim
    /// Wiederherstellen der Reiter „Optionen“ aktiv war; die Fahrzeugverwaltung zeigt danach nur die gesicherten Fahrzeuge.
    /// </summary>
    [Fact]
    public void Restore_WithoutSelectedVehicle_ShowsRestoredVehicle()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "10000", string.Empty);

            OpenBackup();
            CreateBackup();

            OpenTankbook();
            CreateVehicle("Neu nach Sicherung", "Diesel", "20000", string.Empty);
            Tankbook.OpenEntries("Neu nach Sicherung");

            OpenBackup();
            RestoreBackup(1);

            NavigateToTab("Tankbuch", "TankbookEntries.ManageVehicles");
            WaitForTexts("TankbookEntries.Vehicle", "Golf");
            Tankbook.ManageVehicles();
            WaitForTexts("Tankbook.Vehicle.Name", "Golf");
        });
    }

    /// <summary>
    /// Prüft, dass die Wiederherstellung die gemerkte Auswahl zurücksetzt: Obwohl das gewählte Fahrzeug „Polo“ in der Sicherung enthalten ist, zeigt der Tab
    /// danach das erste Fahrzeug nach Name.
    /// </summary>
    [Fact]
    public void Restore_ResetsSelection_ShowsFirstVehicleByName()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "10000", string.Empty);
            CreateVehicle("Polo", "Petrol", "5000", string.Empty);

            OpenBackup();
            CreateBackup();

            OpenTankbook();
            Tankbook.OpenEntries("Polo");

            OpenBackup();
            RestoreBackup(1);

            NavigateToTab("Tankbuch", "TankbookEntries.ManageVehicles");
            WaitForTexts("TankbookEntries.Vehicle", "Golf");
        });
    }
}

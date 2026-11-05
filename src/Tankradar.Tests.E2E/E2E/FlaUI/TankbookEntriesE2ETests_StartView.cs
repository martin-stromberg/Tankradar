namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test (Issue #20): Der Tab „Tankbuch“ zeigt direkt die Tank- und Ladevorgänge des Fahrzeugs; die Fahrzeugverwaltung ist über den Leerzustand bzw. die Fahrzeugliste im Kopf erreichbar.
/// </summary>
public class TankbookEntriesE2ETests_StartView : TankbookE2ETestBase
{
    /// <summary>
    /// Prüft, dass der Tab nach einem Wechsel des Reiters ohne weitere Auswahl die Vorgänge des einzigen Fahrzeugs zeigt (Kopf mit Name, Liste sichtbar).
    /// </summary>
    [Fact]
    public void Tab_ShowsEntriesOfOnlyVehicleDirectly()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "10000", string.Empty);
            Tankbook.OpenEntries("Golf");
            Tankbook.CreateEntry("Alpha Tankstelle", "40", "1,859", string.Empty, "10500");

            NavigateToTab("Favoriten", "Favorites.Refresh");
            NavigateToTab("Tankbuch", "TankbookEntries.Add");

            WaitForPageTitle("Tankbuch");
            WaitForTexts("TankbookEntries.Vehicle", "Golf");
            WaitForTexts("TankbookEntry.Item.Station", "Alpha Tankstelle");
            Assert.Equal("Golf", WaitForAutomationId("TankbookEntries.Vehicle").Name);
            WaitForAutomationId("TankbookEntries.Statistics.Heading");
            Assert.Equal("Export einblenden", WaitForAutomationId("TankbookEntries.Export.Toggle").Name);
            Assert.False(Exists("TankbookEntries.Export.Start"));
            Assert.False(Exists("Tankbook.AddVehicle"));
        });
    }

    /// <summary>
    /// Prüft, dass ohne Fahrzeug der Weg über „Fahrzeug hinzufügen“ die Verwaltung (Titel „Fahrzeuge“, Hinweis auf den Tab) öffnet und „Zurück“ wieder zur Tab-Seite führt.
    /// </summary>
    [Fact]
    public void ManageVehicles_OpensManagement_AndBackReturnsToTab()
    {
        RunWithDiagnostics(() =>
        {
            Tankbook.OpenTab();
            Assert.Equal("Fahrzeug hinzufügen", WaitForAutomationId("TankbookEntries.AddVehicle").Name);

            Tankbook.ManageVehicles();
            WaitForPageTitle("Fahrzeuge");
            WaitForAutomationId("Tankbook.ChooseVehicleIntro");
            WaitForAutomationId("Tankbook.Empty");

            Tankbook.LeaveVehicleManagement();
            WaitForPageTitle("Tankbuch");
            WaitForAutomationId("TankbookEntries.NoVehicles");
        });
    }
}

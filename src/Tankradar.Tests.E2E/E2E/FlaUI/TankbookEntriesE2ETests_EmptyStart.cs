namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test (Issue #20): Leerzustand des Tabs „Tankbuch“ ohne Fahrzeug mit direktem Weg zum Anlegen.
/// </summary>
public class TankbookEntriesE2ETests_EmptyStart : TankbookE2ETestBase
{
    /// <summary>
    /// Prüft, dass ohne Fahrzeug nur der Hinweis mit „Fahrzeug hinzufügen“ erscheint (Formular, Auswertung und Export ausgeblendet), die Schaltfläche das
    /// Formular „Neues Fahrzeug“ öffnet und das gespeicherte Fahrzeug danach im Tab gezeigt wird.
    /// </summary>
    [Fact]
    public void NoVehicle_ShowsHint_AndAddVehicleLeadsToTab()
    {
        RunWithDiagnostics(() =>
        {
            Tankbook.OpenTab();
            Assert.Equal(
                "Noch kein Fahrzeug angelegt. Lege über „Fahrzeug hinzufügen“ dein erstes Fahrzeug an, um Tank- und Ladevorgänge zu erfassen.",
                WaitForAutomationId("TankbookEntries.NoVehicles").Name);
            Assert.Equal("Fahrzeug hinzufügen", WaitForAutomationId("TankbookEntries.AddVehicle").Name);
            Assert.False(Exists("TankbookEntries.Add"));
            Assert.False(Exists("TankbookEntries.Statistics.Heading"));
            Assert.False(Exists("TankbookEntries.Export.Start"));
            Assert.False(Exists("TankbookEntries.SelectVehicle"));

            Tankbook.AddVehicleFromEmptyState();
            Assert.Equal("Neues Fahrzeug", WaitForAutomationId("Tankbook.Form.Title").Name);
            FillVehicleForm("Golf", "Diesel", "10000", string.Empty);
            SaveVehicleForm();
            WaitForTexts("Tankbook.Vehicle.Name", "Golf");

            Tankbook.LeaveVehicleManagement();
            WaitForTexts("TankbookEntries.Vehicle", "Golf");
            WaitForAutomationId("TankbookEntries.Add");
            WaitForAutomationId("TankbookEntries.Empty");
            Assert.False(Exists("TankbookEntries.NoVehicles"));
        });
    }
}

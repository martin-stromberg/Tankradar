namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test (Issue #20): Wird das gewählte Fahrzeug in der Verwaltung gelöscht, zeigt der Tab das nächste Fahrzeug nach Name, nach dem letzten den Leerzustand.
/// </summary>
public class TankbookEntriesE2ETests_DeleteSelected : TankbookE2ETestBase
{
    /// <summary>
    /// Prüft den Rückfall auf „Golf“ nach dem Löschen des gewählten „Polo“ und den Leerzustand nach dem Löschen des letzten Fahrzeugs.
    /// </summary>
    [Fact]
    public void DeleteSelectedVehicle_ShowsNextVehicle_ThenEmptyState()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "10000", string.Empty);
            CreateVehicle("Polo", "Petrol", "5000", string.Empty);
            Tankbook.OpenEntries("Polo");

            Tankbook.ManageVehicles();
            DeleteVehicle("Polo");
            Tankbook.LeaveVehicleManagement();
            WaitForTexts("TankbookEntries.Vehicle", "Golf");
            Assert.False(Exists("TankbookEntries.Status"));

            Tankbook.ManageVehicles();
            DeleteVehicle("Golf");
            WaitForAutomationId("Tankbook.Empty");
            Tankbook.LeaveVehicleManagement();
            WaitForAutomationId("TankbookEntries.NoVehicles");
            Assert.False(Exists("TankbookEntries.Add"));
        });
    }

    private void DeleteVehicle(string name)
    {
        PressForVehicle("Tankbook.Vehicle.Delete", name);
        Press("Tankbook.Vehicle.DeleteConfirm");
        Tankbook.WaitUntil(() => !ReadTexts("Tankbook.Vehicle.Name").Contains(name), $"Das Fahrzeug '{name}' wurde nicht gelöscht.");
    }
}

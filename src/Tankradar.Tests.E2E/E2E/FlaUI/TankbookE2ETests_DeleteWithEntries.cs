namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test der ausdrücklichen Rückfrage beim Löschen eines Fahrzeugs mit Tank- oder Ladevorgängen und des Mitlöschens der Vorgänge.
/// Die Vorgänge werden direkt in die isolierte Testdatenbank geschrieben, weil es in Schritt 12 keine Erfassung gibt.
/// </summary>
public class TankbookE2ETests_DeleteWithEntries : TankbookE2ETestBase
{
    /// <summary>
    /// Prüft, dass die Rückfrage die Zahl der Vorgänge und deren endgültiges Löschen nennt und nach Bestätigung Fahrzeug und Vorgänge entfernt sind.
    /// </summary>
    [Fact]
    public void DeleteVehicleWithEntries_AsksExplicitlyAndDeletesEntries()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "84350", "5,4");
            TankbookTestDatabase.AddEntries(DatabasePath, "Golf", 2);
            Assert.Equal(2, TankbookTestDatabase.CountEntries(DatabasePath));

            PressForVehicle("Tankbook.Vehicle.Delete", "Golf");

            Assert.Contains("2 zugehörigen Tank- und Ladevorgänge endgültig gelöscht", WaitForAutomationId("Tankbook.Vehicle.DeleteQuestion").Name, StringComparison.Ordinal);
            Assert.Equal("Fahrzeug und Vorgänge löschen", WaitForAutomationId("Tankbook.Vehicle.DeleteConfirm").Name);
            Press("Tankbook.Vehicle.DeleteConfirm");
            WaitForAutomationId("Tankbook.Empty");
            Assert.Equal(0, TankbookTestDatabase.CountEntries(DatabasePath));
        });
    }
}

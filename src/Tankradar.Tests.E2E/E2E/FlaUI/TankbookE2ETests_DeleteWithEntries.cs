namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test der ausdrücklichen Rückfrage beim Löschen eines Fahrzeugs mit Tank- oder Ladevorgängen und des Mitlöschens der Vorgänge.
/// Die Vorgänge werden direkt in die isolierte Testdatenbank geschrieben, damit auch „inzwischen hinzugekommene“ Vorgänge prüfbar sind.
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
            Assert.Contains("Erstelle vorher in den Optionen eine Sicherung, wenn du die Vorgänge behalten möchtest.", WaitForAutomationId("Tankbook.Vehicle.DeleteQuestion").Name, StringComparison.Ordinal);
            Assert.Equal("Fahrzeug und Vorgänge löschen", WaitForAutomationId("Tankbook.Vehicle.DeleteConfirm").Name);
            Assert.Equal("Fahrzeug „Golf“ und 2 Tank- und Ladevorgänge endgültig löschen", Tankbook.ReadHint("Tankbook.Vehicle.DeleteConfirm"));
            Press("Tankbook.Vehicle.DeleteConfirm");
            WaitForAutomationId("Tankbook.Empty");
            Assert.Equal(0, TankbookTestDatabase.CountEntries(DatabasePath));
        });
    }

    /// <summary>
    /// Prüft die Neuzählung beim Bestätigen: Kommt während der offenen Rückfrage ein Vorgang hinzu, bleibt das Fahrzeug erhalten und die Rückfrage nennt
    /// die neue Zahl; erst die erneute Bestätigung löscht Fahrzeug und alle Vorgänge.
    /// </summary>
    [Fact]
    public void EntryAddedBeforeConfirming_IsNamedBeforeDeleting()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "84350", string.Empty);
            TankbookTestDatabase.AddEntries(DatabasePath, "Golf", 2);
            PressForVehicle("Tankbook.Vehicle.Delete", "Golf");
            Assert.Contains("2 zugehörigen", WaitForAutomationId("Tankbook.Vehicle.DeleteQuestion").Name, StringComparison.Ordinal);

            TankbookTestDatabase.AddEntries(DatabasePath, "Golf", 1, firstOdometerKm: 90_000, firstOccurredAt: DateTime.Now.AddMinutes(-5));
            Press("Tankbook.Vehicle.DeleteConfirm");

            WaitUntil(
                () => FindByAutomationId("Tankbook.Vehicle.DeleteQuestion")?.Name.Contains("3 zugehörigen", StringComparison.Ordinal) == true,
                "Die Rückfrage nennt nach der Neuzählung nicht 3 Vorgänge.");
            WaitForTexts("Tankbook.Vehicle.Name", "Golf");
            Assert.Equal(3, TankbookTestDatabase.CountEntries(DatabasePath));
            Assert.Equal("Fahrzeug „Golf“ und 3 Tank- und Ladevorgänge endgültig löschen", Tankbook.ReadHint("Tankbook.Vehicle.DeleteConfirm"));

            Press("Tankbook.Vehicle.DeleteConfirm");
            WaitForAutomationId("Tankbook.Empty");
            Assert.Equal(0, TankbookTestDatabase.CountEntries(DatabasePath));
        });
    }
}

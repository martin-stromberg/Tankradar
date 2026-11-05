namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Der Ablauf „Sicherung erstellen – Daten ändern – aus der Liste wiederherstellen – Sicherung löschen“ mit allen Rückfragen und Abbrüchen.
/// </summary>
public class BackupE2ETests_Flow : BackupE2ETestBase
{
    /// <summary>
    /// Prüft den vollständigen Ablauf: Erstellen, Ändern, Abbrechen der Rückfrage (Daten unverändert), Wiederherstellen, Löschen mit Abbruch und Bestätigung.
    /// </summary>
    [Fact]
    public void CreateChangeRestoreDelete_FullFlow()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "84350", "5,4");

            OpenBackup();
            WaitForAutomationId("Settings.Backup.Empty");
            CreateBackup();
            WaitForAutomationId(ItemId(1, "Title"));
            Assert.Matches(@"^\d{2}\.\d{2}\.\d{4} \d{2}:\d{2}$", WaitForAutomationId(ItemId(1, "Title")).Name);
            Assert.Matches(@"^\d+([,.]\d+)? (B|KB|MB)$", WaitForAutomationId(ItemId(1, "Size")).Name);
            Assert.False(Exists("Settings.Backup.Empty"));
            WaitForBackupFileCount(1);

            OpenTankbook();
            CreateVehicle("Opel", "Petrol", "1000", string.Empty);
            OpenSettings();
            WaitForAutomationId("Settings.Nearby.Switch").Patterns.Toggle.Pattern.Toggle();
            WaitUntil(IsNearbyOn, "Der Schalter 'In der Nähe' wurde nicht eingeschaltet.");

            OpenBackup();
            Press(ItemId(1, "Restore"));
            Assert.Contains("1 Fahrzeug", WaitForAutomationId(ItemId(1, "RestoreQuestion")).Name, StringComparison.Ordinal);
            Assert.Contains("Die aktuellen Daten werden vollständig ersetzt", WaitForAutomationId(ItemId(1, "RestoreQuestion")).Name, StringComparison.Ordinal);
            Press(ItemId(1, "RestoreCancel"));
            WaitUntilGone(ItemId(1, "RestoreQuestion"));
            OpenTankbook();
            Tankbook.WaitForTexts("Tankbook.Vehicle.Name", "Golf", "Opel");
            OpenSettings();
            WaitUntil(IsNearbyOn, "Der Abbruch hat die Einstellung verändert.");

            OpenBackup();
            RestoreBackup(1);
            WaitUntil(() => !IsNearbyOn(), "Der Schalter 'In der Nähe' wurde nicht zurückgesetzt.");
            OpenTankbook();
            Tankbook.WaitForTexts("Tankbook.Vehicle.Name", "Golf");

            OpenBackup();
            Press(ItemId(1, "Delete"));
            Assert.Contains("endgültig gelöscht", WaitForAutomationId(ItemId(1, "DeleteQuestion")).Name, StringComparison.Ordinal);
            Press(ItemId(1, "DeleteCancel"));
            WaitUntilGone(ItemId(1, "DeleteQuestion"));
            Assert.Single(BackupFiles());

            Press(ItemId(1, "Delete"));
            WaitForAutomationId(ItemId(1, "DeleteQuestion"));
            Press(ItemId(1, "DeleteConfirm"));
            WaitForAutomationId("Settings.Backup.Empty");
            WaitForBackupFileCount(0);
        });
    }
}

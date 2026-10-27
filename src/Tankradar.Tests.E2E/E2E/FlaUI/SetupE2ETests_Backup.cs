namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Nach der Wiederherstellung einer Sicherung mit Einstellungen erscheint die Einrichtung beim nächsten Start nicht.
/// </summary>
public class SetupE2ETests_Backup : BackupE2ETestBase
{
    /// <inheritdoc />
    protected override bool CompletesSetupOnStart => false;

    /// <summary>
    /// Prüft, dass eine wiederhergestellte Sicherung die Einstellungen der Einrichtung mitbringt und beim nächsten Start keine Einrichtung zeigt.
    /// </summary>
    [Fact]
    public void RestoreBackupWithSettings_NextStart_ShowsNoSetup()
    {
        RunWithDiagnostics(() =>
        {
            WaitForSetup();
            ToggleSetupFuelType("SuperE10");
            ToggleSetupFuelType("Diesel");
            PressSetupButton(SetupSaveId);
            WaitForSetupClosed();

            OpenBackup();
            CreateBackup();
            WaitForBackupFileCount(1);
            ToggleFuelType("Diesel");
            WaitUntil(() => IsFuelTypeSelected("Diesel"), "Diesel wurde in den Optionen nicht ausgewählt.");
            RestoreBackup(1);

            RestartApplication();
            OpenSettings();

            Assert.False(Exists(SetupSaveId), "Die Einrichtung erschien nach der Wiederherstellung.");
            Assert.True(IsFuelTypeSelected("SuperE5"));
            Assert.False(IsFuelTypeSelected("SuperE10"));
            Assert.False(IsFuelTypeSelected("Diesel"));
        });
    }
}

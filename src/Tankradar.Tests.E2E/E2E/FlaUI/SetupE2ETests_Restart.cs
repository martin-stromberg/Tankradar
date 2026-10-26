namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Nach dem Speichern erscheint die Einrichtung bei einem Neustart nicht erneut.
/// </summary>
public class SetupE2ETests_Restart : SettingsE2ETestBase
{
    /// <inheritdoc />
    protected override bool CompletesSetupOnStart => false;

    /// <summary>
    /// Prüft, dass die Einrichtung nur einmal erscheint und die gespeicherte Auswahl einen Neustart übersteht.
    /// </summary>
    [Fact]
    public void AfterSave_Restart_ShowsNoSetup_AndKeepsSelection()
    {
        RunWithDiagnostics(() =>
        {
            WaitForSetup();
            ToggleSetupFuelType("SuperE5");
            ToggleSetupFuelType("SuperE10");
            PressSetupButton(SetupSaveId);
            WaitForSetupClosed();

            RestartApplication();
            OpenSettings();

            Assert.False(Exists(SetupSaveId), "Die Einrichtung erschien nach dem Neustart erneut.");
            Assert.False(IsFuelTypeSelected("SuperE5"));
            Assert.False(IsFuelTypeSelected("SuperE10"));
            Assert.True(IsFuelTypeSelected("Diesel"));
        });
    }
}

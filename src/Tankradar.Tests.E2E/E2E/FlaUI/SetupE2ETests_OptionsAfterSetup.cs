namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Nach der Einrichtung lässt sich die Auswahl in den Optionen ändern, ohne dass die Einrichtung erneut erscheint.
/// </summary>
public class SetupE2ETests_OptionsAfterSetup : SettingsE2ETestBase
{
    /// <inheritdoc />
    protected override bool CompletesSetupOnStart => false;

    /// <summary>
    /// Prüft, dass eine Änderung in den Optionen nach der Einrichtung gespeichert wird und nach einem Neustart gilt.
    /// </summary>
    [Fact]
    public void ChangeInOptions_AfterSetup_IsKept_AndSetupStaysHidden()
    {
        RunWithDiagnostics(() =>
        {
            WaitForSetup();
            ToggleSetupFuelType("SuperE10");
            PressSetupButton(SetupSaveId);
            WaitForSetupClosed();

            OpenSettings();
            ToggleFuelType("SuperE10");
            WaitUntil(() => IsFuelTypeSelected("SuperE10"), "Super E10 wurde in den Optionen nicht ausgewählt.");
            ToggleFuelType("Diesel");
            WaitUntil(() => !IsFuelTypeSelected("Diesel"), "Diesel wurde in den Optionen nicht abgewählt.");

            RestartApplication();
            OpenSettings();

            Assert.False(Exists(SetupSaveId), "Die Einrichtung erschien nach der Änderung in den Optionen erneut.");
            Assert.True(IsFuelTypeSelected("SuperE5"));
            Assert.True(IsFuelTypeSelected("SuperE10"));
            Assert.False(IsFuelTypeSelected("Diesel"));
        });
    }
}

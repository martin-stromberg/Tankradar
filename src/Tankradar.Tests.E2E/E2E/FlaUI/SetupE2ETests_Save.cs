namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Die in der Einrichtung gespeicherte Auswahl erscheint in den Optionen (gleiche Einstellung, keine zweite Quelle).
/// </summary>
public class SetupE2ETests_Save : SettingsE2ETestBase
{
    /// <inheritdoc />
    protected override bool CompletesSetupOnStart => false;

    /// <summary>
    /// Prüft, dass „Speichern“ die Einrichtung ausblendet und die Optionen die gewählte Auswahl zeigen.
    /// </summary>
    [Fact]
    public void Save_ClosesSetup_AndOptionsShowSelection()
    {
        RunWithDiagnostics(() =>
        {
            WaitForSetup();
            ToggleSetupFuelType("SuperE10");

            PressSetupButton(SetupSaveId);
            WaitForSetupClosed();
            OpenSettings();

            Assert.True(IsFuelTypeSelected("SuperE5"));
            Assert.False(IsFuelTypeSelected("SuperE10"));
            Assert.True(IsFuelTypeSelected("Diesel"));
            Assert.Equal(DefaultFuelTypeOrder, ReadFuelTypeOrder());
        });
    }
}

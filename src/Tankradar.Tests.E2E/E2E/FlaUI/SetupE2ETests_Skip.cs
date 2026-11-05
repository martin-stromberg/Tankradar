namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: „Überspringen“ beendet die Einrichtung mit dem Standardwert (alle Spritsorten).
/// </summary>
public class SetupE2ETests_Skip : SettingsE2ETestBase
{
    /// <inheritdoc />
    protected override bool CompletesSetupOnStart => false;

    /// <summary>
    /// Prüft, dass „Überspringen“ trotz geänderter Auswahl alle Sorten speichert und die Einrichtung nach einem Neustart ausbleibt.
    /// </summary>
    [Fact]
    public void Skip_ClosesSetup_AndOptionsShowAllFuelTypes()
    {
        RunWithDiagnostics(() =>
        {
            WaitForSetup();
            ToggleSetupFuelType("Diesel");

            PressSetupButton(SetupSkipId);
            WaitForSetupClosed();
            OpenSettings();

            foreach (var fuelType in DefaultFuelTypeOrder)
            {
                Assert.True(IsFuelTypeSelected(fuelType), $"Spritsorte {fuelType} sollte ausgewählt sein.");
            }

            RestartApplication();
            OpenSettings();
            Assert.False(Exists(SetupSaveId), "Die Einrichtung erschien nach dem Überspringen erneut.");
        });
    }
}

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Bei frischer Datenbank erscheint die Einrichtung mit Überschrift, Hinweis und allen Spritsorten vorausgewählt.
/// </summary>
public class SetupE2ETests_FirstStart : SettingsE2ETestBase
{
    /// <inheritdoc />
    protected override bool CompletesSetupOnStart => false;

    /// <summary>
    /// Prüft das Erscheinen der Einrichtung beim ersten Start und die Standardauswahl (E5, E10, Diesel) ohne Verschiebe-Schaltflächen.
    /// </summary>
    [Fact]
    public void FirstStart_ShowsSetupWithAllFuelTypesSelected()
    {
        RunWithDiagnostics(() =>
        {
            WaitForSetup();

            Assert.Equal("Willkommen bei Tankatlas", WaitForAutomationId(SetupHeadingId).Name);
            Assert.StartsWith("Wähle die Spritsorten aus", WaitForAutomationId(SetupHintId).Name, StringComparison.Ordinal);
            foreach (var fuelType in DefaultFuelTypeOrder)
            {
                Assert.True(IsSetupFuelTypeSelected(fuelType), $"Spritsorte {fuelType} sollte vorausgewählt sein.");
                Assert.False(Exists($"Setup.FuelType.{fuelType}.MoveUp"), $"Die Einrichtung zeigt eine Verschiebe-Schaltfläche für {fuelType}.");
                Assert.False(Exists($"Setup.FuelType.{fuelType}.MoveDown"), $"Die Einrichtung zeigt eine Verschiebe-Schaltfläche für {fuelType}.");
            }

            Assert.True(Exists(SetupSkipId), "Die Schaltfläche „Überspringen“ fehlt.");
            Assert.False(Exists(SetupFuelTypeSwitchId("Electric")));
            Assert.False(Exists(SetupStatusMessageId));
        });
    }
}

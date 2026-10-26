namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Ohne ausgewählte Spritsorte bleibt die Einrichtung offen und zeigt einen Hinweis.
/// </summary>
public class SetupE2ETests_Validation : SettingsE2ETestBase
{
    /// <inheritdoc />
    protected override bool CompletesSetupOnStart => false;

    /// <summary>
    /// Prüft, dass „Speichern“ ohne Auswahl die Meldung „mindestens eine“ zeigt und die Einrichtung sichtbar bleibt.
    /// </summary>
    [Fact]
    public void SaveWithoutFuelType_ShowsMessage_AndStaysOpen()
    {
        RunWithDiagnostics(() =>
        {
            WaitForSetup();
            foreach (var fuelType in DefaultFuelTypeOrder)
            {
                ToggleSetupFuelType(fuelType);
            }

            PressSetupButton(SetupSaveId);

            WaitUntil(() => Exists(SetupStatusMessageId), "Der Hinweis erschien nicht.");
            Assert.Equal("Wähle mindestens eine Spritsorte aus.", WaitForAutomationId(SetupStatusMessageId).Name);
            Assert.True(Exists(SetupSaveId), "Die Einrichtung wurde trotz fehlender Auswahl geschlossen.");
            Assert.True(Exists(SetupSkipId));
        });
    }
}

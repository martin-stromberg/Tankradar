namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Die Bedienelemente der Einrichtung liegen im UI-Automation-Baum (und damit in der Fokus- und Vorlesereihenfolge)
/// in der Reihenfolge Überschrift, Hinweis, Spritsorten-Schalter, „Überspringen“, „Speichern“.
/// </summary>
public class SetupE2ETests_Accessibility : SettingsE2ETestBase
{
    /// <inheritdoc />
    protected override bool CompletesSetupOnStart => false;

    /// <summary>
    /// Prüft die Reihenfolge der Elemente der Einrichtung im UI-Automation-Baum.
    /// </summary>
    [Fact]
    public void FirstStart_SetupFocusOrderFollowsLayout()
    {
        RunWithDiagnostics(() =>
        {
            WaitForSetup();
            string[] expectedOrder =
            [
                SetupHeadingId,
                SetupHintId,
                .. DefaultFuelTypeOrder.Select(SetupFuelTypeSwitchId),
                SetupSkipId,
                SetupSaveId,
            ];

            var actualOrder = MainWindow.FindAllDescendants()
                .Select(element => element.Properties.AutomationId.ValueOrDefault ?? string.Empty)
                .Where(expectedOrder.Contains)
                .Distinct()
                .ToList();

            Assert.Equal(expectedOrder, actualOrder);
        });
    }
}

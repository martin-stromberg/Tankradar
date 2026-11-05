namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Tests: Auswahl und Reihenfolge der Spritsorten sowie die Sperre der letzten ausgewählten Sorte.
/// </summary>
public class SettingsE2ETests_FuelTypes : SettingsE2ETestBase
{
    /// <summary>
    /// Prüft, dass eine abgewählte Sorte und eine verschobene Reihenfolge einen Neustart überstehen.
    /// </summary>
    [Fact]
    public void FuelTypeSelectionAndOrder_SurviveRestart()
    {
        RunWithDiagnostics(() =>
        {
            OpenSettings();
            ToggleFuelType("SuperE10");
            WaitUntil(() => !IsFuelTypeSelected("SuperE10"), "Super E10 wurde nicht abgewählt.");
            MoveFuelType("Diesel", "MoveUp");
            WaitUntil(() => ReadFuelTypeOrder().SequenceEqual(["SuperE5", "Diesel", "SuperE10"]), "Diesel wurde nicht nach oben verschoben.");
            MoveFuelType("Diesel", "MoveUp");
            WaitUntil(() => ReadFuelTypeOrder().SequenceEqual(["Diesel", "SuperE5", "SuperE10"]), "Diesel wurde nicht an die erste Stelle verschoben.");

            RestartApplication();
            OpenSettings();

            WaitUntil(() => ReadFuelTypeOrder().SequenceEqual(["Diesel", "SuperE5", "SuperE10"]), "Die Reihenfolge wurde nicht wiederhergestellt.");
            Assert.True(IsFuelTypeSelected("Diesel"));
            Assert.True(IsFuelTypeSelected("SuperE5"));
            Assert.False(IsFuelTypeSelected("SuperE10"));
        });
    }

    /// <summary>
    /// Prüft, dass die letzte ausgewählte Sorte nicht abgewählt werden kann und ein Hinweis erscheint.
    /// </summary>
    [Fact]
    public void LastSelectedFuelType_CannotBeDeselected_ShowsMessage()
    {
        RunWithDiagnostics(() =>
        {
            OpenSettings();
            ToggleFuelType("SuperE10");
            WaitUntil(() => !IsFuelTypeSelected("SuperE10"), "Super E10 wurde nicht abgewählt.");
            ToggleFuelType("Diesel");
            WaitUntil(() => !IsFuelTypeSelected("Diesel"), "Diesel wurde nicht abgewählt.");

            ToggleFuelType("SuperE5");

            WaitUntil(() => Exists("Settings.StatusMessage"), "Der Hinweis erschien nicht.");
            Assert.Equal("Mindestens eine Spritsorte muss ausgewählt bleiben.", WaitForAutomationId("Settings.StatusMessage").Name);
            WaitUntil(() => IsFuelTypeSelected("SuperE5"), "Die letzte Sorte wurde abgewählt statt gesperrt.");
        });
    }
}

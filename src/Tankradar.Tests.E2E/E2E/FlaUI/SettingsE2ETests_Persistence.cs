namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Geänderte Einstellungen werden sofort gespeichert und bleiben nach einem Neustart erhalten.
/// </summary>
public class SettingsE2ETests_Persistence : SettingsE2ETestBase
{
    /// <summary>
    /// Prüft, dass GPS, Ansicht und Sortierung nach einem Neustart ohne weitere Aktion unverändert angezeigt werden.
    /// </summary>
    [Fact]
    public void ChangedChoices_SurviveRestart()
    {
        RunWithDiagnostics(() =>
        {
            OpenSettings();
            SelectRadio("Settings.Gps.Never");
            SelectRadio("Settings.View.Map");
            SelectRadio("Settings.Sort.Distance");

            RestartApplication();
            OpenSettings();

            WaitUntil(() => IsRadioChecked("Settings.Gps.Never"), "GPS 'Nie' wurde nicht wiederhergestellt.");
            Assert.False(IsRadioChecked("Settings.Gps.WhileInUse"));
            WaitUntil(() => IsRadioChecked("Settings.View.Map"), "Ansicht 'Karte' wurde nicht wiederhergestellt.");
            WaitUntil(() => IsRadioChecked("Settings.Sort.Distance"), "Sortierung 'Entfernung' wurde nicht wiederhergestellt.");
        });
    }
}

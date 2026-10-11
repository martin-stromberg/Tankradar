using FlaUI.Core.Definitions;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Der Schalter „In der Nähe“ wird sofort gespeichert, bleibt nach einem Neustart erhalten, und der Hinweis erscheint bei der Standortnutzung „Nie“.
/// </summary>
public class SettingsE2ETests_NearbyPersistence : SettingsE2ETestBase
{
    /// <summary>
    /// Prüft, dass der eingeschaltete Schalter nach einem Neustart eingeschaltet bleibt.
    /// </summary>
    [Fact]
    public void NearbySwitch_SurvivesRestart()
    {
        RunWithDiagnostics(() =>
        {
            OpenSettings();
            WaitForAutomationId("Settings.Nearby.Switch").Patterns.Toggle.Pattern.Toggle();
            WaitUntil(() => IsNearbyOn(), "Der Schalter 'In der Nähe' wurde nicht eingeschaltet.");

            RestartApplication();
            OpenSettings();

            WaitUntil(() => IsNearbyOn(), "Der Schalter 'In der Nähe' wurde nicht wiederhergestellt.");
        });
    }

    /// <summary>
    /// Prüft, dass der Hinweis bei der Standortnutzung „Nie“ erscheint und bei einer anderen Wahl wieder verschwindet.
    /// </summary>
    [Fact]
    public void GpsNever_ShowsNearbyHint()
    {
        RunWithDiagnostics(() =>
        {
            OpenSettings();
            Assert.False(Exists("Settings.Nearby.GpsHint"));

            SelectRadio("Settings.Gps.Never");
            WaitUntil(() => Exists("Settings.Nearby.GpsHint"), "Der Hinweis zu 'In der Nähe' erschien nicht.");

            SelectRadio("Settings.Gps.Always");
            WaitUntil(() => !Exists("Settings.Nearby.GpsHint"), "Der Hinweis zu 'In der Nähe' verschwand nicht.");
        });
    }

    private bool IsNearbyOn()
    {
        return WaitForAutomationId("Settings.Nearby.Switch").Patterns.Toggle.Pattern.ToggleState.Value == ToggleState.On;
    }
}

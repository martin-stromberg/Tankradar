namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Tests: Beim ersten Start zeigen die Optionen die sicheren Standardwerte und genau die geforderten Bedienelemente.
/// </summary>
public class SettingsE2ETests_Defaults : SettingsE2ETestBase
{
    /// <summary>
    /// Prüft, dass ein frischer Start die Standardwerte anzeigt (GPS nur bei Nutzung, Liste, Preis, alle Sorten in der Reihenfolge E5, E10, Diesel).
    /// </summary>
    [Fact]
    public void FirstStart_ShowsDefaultValues()
    {
        RunWithDiagnostics(() =>
        {
            OpenSettings();

            Assert.True(IsRadioChecked("Settings.Gps.WhileInUse"));
            Assert.False(IsRadioChecked("Settings.Gps.Always"));
            Assert.False(IsRadioChecked("Settings.Gps.Never"));
            Assert.True(IsRadioChecked("Settings.View.List"));
            Assert.False(IsRadioChecked("Settings.View.Map"));
            Assert.True(IsRadioChecked("Settings.Sort.Price"));
            Assert.False(IsRadioChecked("Settings.Sort.Distance"));
            Assert.False(IsRadioChecked("Settings.Sort.Name"));
            foreach (var fuelType in DefaultFuelTypeOrder)
            {
                Assert.True(IsFuelTypeSelected(fuelType), $"Spritsorte {fuelType} sollte ausgewählt sein.");
            }

            Assert.Equal(DefaultFuelTypeOrder, ReadFuelTypeOrder());
        });
    }

    /// <summary>
    /// Prüft, dass alle Bedienelemente vorhanden sind und keine Strom- oder Ladetyp-Einstellung existiert.
    /// </summary>
    [Fact]
    public void Settings_ProvideAllControls_WithoutElectricOptions()
    {
        RunWithDiagnostics(() =>
        {
            OpenSettings();

            foreach (var fuelType in DefaultFuelTypeOrder)
            {
                Assert.True(Exists($"Settings.FuelType.{fuelType}.Switch"), $"Schalter {fuelType} fehlt.");
                Assert.True(Exists($"Settings.FuelType.{fuelType}.MoveUp"), $"Nach-oben-Schaltfläche {fuelType} fehlt.");
                Assert.True(Exists($"Settings.FuelType.{fuelType}.MoveDown"), $"Nach-unten-Schaltfläche {fuelType} fehlt.");
            }

            foreach (var id in new[] { "Gps.Always", "Gps.WhileInUse", "Gps.Never", "View.List", "View.Map", "Sort.Price", "Sort.Distance", "Sort.Name" })
            {
                Assert.True(Exists($"Settings.{id}"), $"Element Settings.{id} fehlt.");
            }

            Assert.False(Exists("Settings.FuelType.Electric.Switch"));
            Assert.False(Exists("Settings.ChargingType"));
        });
    }
}

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test des Benutzerflusses im Tankbuch: Fahrzeug anlegen, bearbeiten (Kraftstoffart mit Einheitenwechsel) und nach Rückfrage löschen.
/// </summary>
public class TankbookE2ETests_Flow : TankbookE2ETestBase
{
    /// <summary>
    /// Prüft Leerhinweis, Anlegen mit Angaben, vorbelegtes Bearbeiten mit Wechsel zu Elektro und Löschen mit Abbrechen und Bestätigen.
    /// </summary>
    [Fact]
    public void CreateEditDeleteVehicle_Flow()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            WaitForAutomationId("Tankbook.Empty");

            Press("Tankbook.AddVehicle");
            Assert.Equal("Neues Fahrzeug", WaitForAutomationId("Tankbook.Form.Title").Name);
            Assert.Equal("km", WaitForAutomationId("Tankbook.Form.OdometerUnit").Name);
            Assert.Equal("L/100 km", WaitForAutomationId("Tankbook.Form.ConsumptionUnit").Name);
            Assert.Equal("Ausgewählt", WaitForAutomationId("Tankbook.Form.FuelKind.Petrol").Properties.HelpText.ValueOrDefault);
            FillVehicleForm("Golf", "Diesel", "84350", "5,4");
            SaveVehicleForm();
            WaitForTexts("Tankbook.Vehicle.Name", "Golf");
            WaitForTexts("Tankbook.Vehicle.Details", "Diesel • Start 84.350 km • Startverbrauch 5,4 L/100 km");
            Assert.False(Exists("Tankbook.Empty"));

            PressForVehicle("Tankbook.Vehicle.Edit", "Golf");
            Assert.Equal("Fahrzeug bearbeiten", WaitForAutomationId("Tankbook.Form.Title").Name);
            Assert.Equal("Golf", WaitForAutomationId("Tankbook.Form.Name").Patterns.Value.Pattern.Value.Value);
            Assert.Equal("84350", WaitForAutomationId("Tankbook.Form.Odometer").Patterns.Value.Pattern.Value.Value);
            Assert.Equal("5,4", WaitForAutomationId("Tankbook.Form.Consumption").Patterns.Value.Pattern.Value.Value);
            Assert.Equal("Ausgewählt", WaitForAutomationId("Tankbook.Form.FuelKind.Diesel").Properties.HelpText.ValueOrDefault);
            Type("Tankbook.Form.Name", "Golf Variant");
            Press("Tankbook.Form.FuelKind.Electric");
            WaitUntil(() => FindByAutomationId("Tankbook.Form.ConsumptionUnit")?.Name == "kWh/100 km", "Die Einheit wechselt nicht zu kWh/100 km.");
            Type("Tankbook.Form.Consumption", "16,5");
            SaveVehicleForm();
            WaitForTexts("Tankbook.Vehicle.Name", "Golf Variant");
            WaitForTexts("Tankbook.Vehicle.Details", "Elektro • Start 84.350 km • Startverbrauch 16,5 kWh/100 km");

            PressForVehicle("Tankbook.Vehicle.Delete", "Golf Variant");
            Assert.Equal("Fahrzeug „Golf Variant“ löschen?", WaitForAutomationId("Tankbook.Vehicle.DeleteQuestion").Name);
            Assert.Equal("Ja, Fahrzeug „Golf Variant“ löschen", WaitForAutomationId("Tankbook.Vehicle.DeleteConfirm").Name);
            Press("Tankbook.Vehicle.DeleteCancel");
            WaitUntil(() => !Exists("Tankbook.Vehicle.DeleteQuestion"), "Die Rückfrage blieb stehen.");
            WaitForTexts("Tankbook.Vehicle.Name", "Golf Variant");

            PressForVehicle("Tankbook.Vehicle.Delete", "Golf Variant");
            Press("Tankbook.Vehicle.DeleteConfirm");
            WaitForAutomationId("Tankbook.Empty");
            Assert.False(Exists("Tankbook.Vehicle.Name"));
        });
    }
}

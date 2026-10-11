namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Tests der Eingabeprüfung im Tankbuch: feldbezogene Meldungen bei ungültigen Eingaben und bei einem schon vergebenen Namen.
/// </summary>
public class TankbookE2ETests_Validation : TankbookE2ETestBase
{
    /// <summary>
    /// Prüft, dass leerer Name, negativer Kilometerstand und ungültiger Verbrauch gemeldet und nicht gespeichert werden und die korrigierte Eingabe gespeichert wird.
    /// </summary>
    [Fact]
    public void InvalidInput_ShowsFieldErrorsAndSavesNothing()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            WaitForAutomationId("Tankbook.Empty");
            Press("Tankbook.AddVehicle");
            Type("Tankbook.Form.Odometer", "-5");
            Type("Tankbook.Form.Consumption", "abc");

            Press("Tankbook.Form.Save");

            Assert.Equal("Bitte gib einen Namen ein.", WaitForAutomationId("Tankbook.Form.NameError").Name);
            Assert.Equal("Der Kilometerstand darf nicht negativ sein.", WaitForAutomationId("Tankbook.Form.OdometerError").Name);
            Assert.Equal("Bitte gib den Startverbrauch als Zahl ein, z. B. 5,4.", WaitForAutomationId("Tankbook.Form.ConsumptionError").Name);
            Assert.True(Exists("Tankbook.Form.Save"));
            Assert.False(Exists("Tankbook.Vehicle.Name"));

            Type("Tankbook.Form.Name", "Golf");
            Type("Tankbook.Form.Odometer", "84.350");
            Type("Tankbook.Form.Consumption", "5,4");
            SaveVehicleForm();
            WaitForTexts("Tankbook.Vehicle.Name", "Golf");
            WaitForTexts("Tankbook.Vehicle.Details", "Benzin • Start 84.350 km • Startverbrauch 5,4 L/100 km");
        });
    }

    /// <summary>
    /// Prüft, dass ein zweites Fahrzeug mit gleichem Namen in anderer Schreibweise am Namen gemeldet wird.
    /// </summary>
    [Fact]
    public void DuplicateName_ShowsNameError()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "84350", string.Empty);

            Press("Tankbook.AddVehicle");
            FillVehicleForm("golf", "Petrol", "1000", string.Empty);
            Press("Tankbook.Form.Save");

            Assert.Equal("Ein Fahrzeug mit diesem Namen gibt es bereits.", WaitForAutomationId("Tankbook.Form.NameError").Name);
            WaitForTexts("Tankbook.Vehicle.Name", "Golf");
        });
    }
}

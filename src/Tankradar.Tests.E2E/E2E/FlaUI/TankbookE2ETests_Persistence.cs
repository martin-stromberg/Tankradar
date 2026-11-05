namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test der lokalen Speicherung: Fahrzeuge überstehen einen echten Neustart der App unverändert und in Namensordnung.
/// </summary>
public class TankbookE2ETests_Persistence : TankbookE2ETestBase
{
    /// <summary>
    /// Prüft, dass zwei angelegte Fahrzeuge nach dem Neustart in Namensordnung mit unveränderten Angaben erscheinen.
    /// </summary>
    [Fact]
    public void Vehicles_SurviveApplicationRestart()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Zoe", "Electric", "12000", string.Empty);
            CreateVehicle("Astra", "Petrol", "120000", string.Empty);
            WaitForTexts("Tankbook.Vehicle.Name", "Astra", "Zoe");

            RestartApplication();
            OpenTankbook();

            WaitForTexts("Tankbook.Vehicle.Name", "Astra", "Zoe");
            WaitForTexts("Tankbook.Vehicle.Details", "Benzin • Start 120.000 km", "Elektro • Start 12.000 km");
        });
    }
}

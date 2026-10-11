using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Meldungen der Feldprüfung und der Ergebnisse des Fahrzeugdienstes.
/// </summary>
public class TankbookTextsTests_Messages : BaseTest
{
    /// <summary>
    /// Prüft, dass jeder Feldfehler und jedes Ergebnis eine Meldung hat und <see cref="VehicleFieldError.None"/> sowie <see cref="VehicleResult.Ok"/> leer bleiben.
    /// </summary>
    [Fact]
    public void FieldAndResultMessages_CoverAllValues()
    {
        Assert.Equal(string.Empty, TankbookTexts.GetNameError(VehicleFieldError.None));
        Assert.Equal(string.Empty, TankbookTexts.GetOdometerError(VehicleFieldError.None));
        Assert.Equal(string.Empty, TankbookTexts.GetConsumptionError(VehicleFieldError.None));
        Assert.Equal("Bitte gib einen Namen ein.", TankbookTexts.GetNameError(VehicleFieldError.Required));
        Assert.Equal("Der Name darf höchstens 40 Zeichen lang sein.", TankbookTexts.GetNameError(VehicleFieldError.TooLong));
        Assert.Equal("Bitte gib den Start-Kilometerstand ein.", TankbookTexts.GetOdometerError(VehicleFieldError.Required));
        Assert.Equal("Bitte gib den Kilometerstand als ganze Zahl ein, z. B. 84350.", TankbookTexts.GetOdometerError(VehicleFieldError.NotANumber));
        Assert.Equal("Der Kilometerstand darf nicht negativ sein.", TankbookTexts.GetOdometerError(VehicleFieldError.Negative));
        Assert.Equal("Der Kilometerstand darf höchstens 9.999.999 km betragen.", TankbookTexts.GetOdometerError(VehicleFieldError.OutOfRange));
        Assert.Equal("Bitte gib den Startverbrauch als Zahl ein, z. B. 5,4.", TankbookTexts.GetConsumptionError(VehicleFieldError.NotANumber));
        Assert.Equal("Der Startverbrauch darf nicht negativ sein.", TankbookTexts.GetConsumptionError(VehicleFieldError.Negative));
        Assert.Equal("Der Startverbrauch muss größer als 0 und kleiner als 100 sein.", TankbookTexts.GetConsumptionError(VehicleFieldError.OutOfRange));
        Assert.Equal("Der Startverbrauch darf höchstens 2 Nachkommastellen haben.", TankbookTexts.GetConsumptionError(VehicleFieldError.TooManyDecimals));

        Assert.Equal(string.Empty, TankbookTexts.GetResultMessage(VehicleResult.Ok));
        Assert.Equal("Bitte gib einen Namen mit höchstens 40 Zeichen ein.", TankbookTexts.GetResultMessage(VehicleResult.InvalidName));
        Assert.Equal(TankbookTexts.DuplicateName, TankbookTexts.GetResultMessage(VehicleResult.DuplicateName));
        Assert.Equal("Bitte wähle eine Kraftstoffart.", TankbookTexts.GetResultMessage(VehicleResult.InvalidFuelKind));
        Assert.Equal("Der Start-Kilometerstand muss zwischen 0 und 9.999.999 km liegen.", TankbookTexts.GetResultMessage(VehicleResult.InvalidOdometer));
        Assert.Equal(
            "Der Startverbrauch muss größer als 0 und kleiner als 100 sein, mit höchstens 2 Nachkommastellen.",
            TankbookTexts.GetResultMessage(VehicleResult.InvalidConsumption));
        Assert.Equal("Das Fahrzeug gibt es nicht mehr.", TankbookTexts.GetResultMessage(VehicleResult.VehicleNotFound));
        foreach (var result in Enum.GetValues<VehicleResult>().Where(value => value != VehicleResult.Ok))
        {
            Assert.NotEmpty(TankbookTexts.GetResultMessage(result));
        }
    }
}

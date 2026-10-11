using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Beschriftungen und Formate des Tankbuchs: Kraftstoffarten mit Einheit, Angaben eines Fahrzeugs und Rückfrage vor dem Löschen.
/// </summary>
public class TankbookTextsTests_Labels : BaseTest
{
    /// <summary>
    /// Prüft Beschriftung, Einheit und Platzhalter jeder Kraftstoffart; kWh nur bei Elektro.
    /// </summary>
    /// <param name="fuelKind">Die Kraftstoffart.</param>
    /// <param name="label">Die erwartete Beschriftung.</param>
    /// <param name="unit">Die erwartete Einheit.</param>
    [Theory]
    [InlineData(VehicleFuelKind.Petrol, "Benzin", "L/100 km")]
    [InlineData(VehicleFuelKind.Diesel, "Diesel", "L/100 km")]
    [InlineData(VehicleFuelKind.Electric, "Elektro", "kWh/100 km")]
    [InlineData(VehicleFuelKind.Hybrid, "Hybrid", "L/100 km")]
    public void GetFuelKindLabel_AllValues(VehicleFuelKind fuelKind, string label, string unit)
    {
        Assert.Equal(label, TankbookTexts.GetFuelKindLabel(fuelKind));
        Assert.Equal(unit, TankbookTexts.GetConsumptionUnit(fuelKind));
        Assert.Equal($"Startverbrauch, optional, in {unit}", TankbookTexts.FormatConsumptionDescription(fuelKind));
        Assert.Equal(fuelKind == VehicleFuelKind.Electric ? "z. B. 16,5" : "z. B. 5,4", TankbookTexts.GetConsumptionPlaceholder(fuelKind));
    }

    /// <summary>
    /// Prüft, dass eine unbekannte Kraftstoffart abgelehnt wird.
    /// </summary>
    [Fact]
    public void GetFuelKindLabel_UnknownValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TankbookTexts.GetFuelKindLabel((VehicleFuelKind)99));
    }

    /// <summary>
    /// Prüft die Angaben eines Fahrzeugs mit und ohne Startverbrauch sowie die Werte für das Formular.
    /// </summary>
    [Fact]
    public void FormatVehicleDetails_WithAndWithoutConsumption()
    {
        Assert.Equal(
            "Diesel • Start 84.350 km • Startverbrauch 5,4 L/100 km",
            TankbookTexts.FormatVehicleDetails(new Vehicle(1, "Golf", VehicleFuelKind.Diesel, 84350, 5.4m)));
        Assert.Equal(
            "Elektro • Start 120.000 km",
            TankbookTexts.FormatVehicleDetails(new Vehicle(2, "Zoe", VehicleFuelKind.Electric, 120000, null)));
        Assert.Equal(
            "Elektro • Start 0 km • Startverbrauch 16,55 kWh/100 km",
            TankbookTexts.FormatVehicleDetails(new Vehicle(3, "Zoe", VehicleFuelKind.Electric, 0, 16.55m)));
        Assert.Equal("84350", TankbookTexts.FormatOdometerInput(84350));
        Assert.Equal("5,4", TankbookTexts.FormatConsumptionInput(5.40m));
        Assert.Equal(string.Empty, TankbookTexts.FormatConsumptionInput(null));
    }

    /// <summary>
    /// Prüft die Rückfrage vor dem Löschen: schlicht ohne Vorgänge, Einzahl und Mehrzahl mit ausdrücklichem Mitlöschen.
    /// </summary>
    /// <param name="count">Die Zahl der Vorgänge.</param>
    /// <param name="expected">Die erwartete Rückfrage.</param>
    [Theory]
    [InlineData(0, "Fahrzeug „Golf“ löschen?")]
    [InlineData(1, "Fahrzeug „Golf“ löschen? Dabei wird auch der zugehörige Tank- oder Ladevorgang endgültig gelöscht. Das lässt sich nicht rückgängig machen.")]
    [InlineData(3, "Fahrzeug „Golf“ löschen? Dabei werden auch die 3 zugehörigen Tank- und Ladevorgänge endgültig gelöscht. Das lässt sich nicht rückgängig machen.")]
    public void FormatDeleteQuestion_ByEntryCount(int count, string expected)
    {
        Assert.Equal(expected, TankbookTexts.FormatDeleteQuestion("Golf", count));
    }

    /// <summary>
    /// Prüft die Beschreibungen der Schaltflächen eines Eintrags für die Sprachausgabe: Aktion und Fahrzeugname sind hörbar.
    /// </summary>
    [Fact]
    public void ButtonDescriptions_NameActionAndVehicle()
    {
        Assert.Equal("Golf bearbeiten", TankbookTexts.FormatEditDescription("Golf"));
        Assert.Equal("Golf löschen", TankbookTexts.FormatDeleteDescription("Golf"));
        Assert.Equal("Ja, Fahrzeug „Golf“ löschen", TankbookTexts.FormatConfirmDeleteDescription("Golf", 0));
        Assert.Equal("Fahrzeug „Golf“ und Vorgänge löschen", TankbookTexts.FormatConfirmDeleteDescription("Golf", 2));
        Assert.Equal("Löschen von „Golf“ abbrechen", TankbookTexts.FormatCancelDeleteDescription("Golf"));
        Assert.Equal("Formular schließen, ohne zu speichern", TankbookTexts.CancelFormDescription);
    }
}

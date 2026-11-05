using System.Globalization;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Schutzprüfung des Dienstes für Fahrzeugeingaben.
/// </summary>
public class VehicleValidatorTests_Input : BaseTest
{
    /// <summary>
    /// Prüft das Ergebnis der Schutzprüfung für gültige und ungültige Eingaben.
    /// </summary>
    /// <param name="name">Der Name.</param>
    /// <param name="fuelKind">Die Kraftstoffart als Zahl (99 ist kein bekannter Wert).</param>
    /// <param name="odometer">Der Kilometerstand.</param>
    /// <param name="consumption">Der Verbrauch in invarianter Schreibweise oder <see langword="null"/>.</param>
    /// <param name="expected">Das erwartete Ergebnis.</param>
    [Theory]
    [InlineData(" ", (int)VehicleFuelKind.Petrol, 0, null, VehicleResult.InvalidName)]
    [InlineData("Golf", 99, 0, null, VehicleResult.InvalidFuelKind)]
    [InlineData("Golf", (int)VehicleFuelKind.Diesel, -1, null, VehicleResult.InvalidOdometer)]
    [InlineData("Golf", (int)VehicleFuelKind.Diesel, 10_000_000, null, VehicleResult.InvalidOdometer)]
    [InlineData("Golf", (int)VehicleFuelKind.Diesel, 0, "0", VehicleResult.InvalidConsumption)]
    [InlineData("Golf", (int)VehicleFuelKind.Diesel, 0, "100", VehicleResult.InvalidConsumption)]
    [InlineData("Golf", (int)VehicleFuelKind.Diesel, 0, "5.123", VehicleResult.InvalidConsumption)]
    [InlineData("Golf", (int)VehicleFuelKind.Hybrid, 9_999_999, "5.40", VehicleResult.Ok)]
    public void Validate_Input_ReturnsResult(string name, int fuelKind, int odometer, string? consumption, VehicleResult expected)
    {
        var input = new VehicleInput(
            name,
            (VehicleFuelKind)fuelKind,
            odometer,
            consumption is null ? null : decimal.Parse(consumption, CultureInfo.InvariantCulture));

        Assert.Equal(expected, VehicleValidator.Validate(input));
    }
}

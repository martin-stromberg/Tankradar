using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Erkennung des Start-Kilometerstands in deutscher Schreibweise (Tausenderpunkte, Leerzeichen am Rand) und die Fehlerarten.
/// </summary>
public class VehicleInputParserTests_Odometer : BaseTest
{
    /// <summary>
    /// Prüft gültige Kilometerstände.
    /// </summary>
    /// <param name="text">Die Eingabe.</param>
    /// <param name="expected">Der erwartete Wert.</param>
    [Theory]
    [InlineData("0", 0)]
    [InlineData("84350", 84350)]
    [InlineData("84.350", 84350)]
    [InlineData(" 120 ", 120)]
    [InlineData("9.999.999", 9_999_999)]
    public void ParseOdometer_ValidInput_ReturnsValue(string text, int expected)
    {
        var error = VehicleInputParser.ParseOdometer(text, out var value);

        Assert.Equal(VehicleFieldError.None, error);
        Assert.Equal(expected, value);
    }

    /// <summary>
    /// Prüft die Fehlerarten einschließlich Überlauf.
    /// </summary>
    /// <param name="text">Die Eingabe.</param>
    /// <param name="expected">Der erwartete Fehler.</param>
    [Theory]
    [InlineData("", VehicleFieldError.Required)]
    [InlineData("abc", VehicleFieldError.NotANumber)]
    [InlineData("12,5", VehicleFieldError.NotANumber)]
    [InlineData("-5", VehicleFieldError.Negative)]
    [InlineData("10000000", VehicleFieldError.OutOfRange)]
    [InlineData("99999999999", VehicleFieldError.OutOfRange)]
    public void ParseOdometer_InvalidInput_ReturnsError(string text, VehicleFieldError expected)
    {
        var error = VehicleInputParser.ParseOdometer(text, out var value);

        Assert.Equal(expected, error);
        Assert.Equal(0, value);
    }
}

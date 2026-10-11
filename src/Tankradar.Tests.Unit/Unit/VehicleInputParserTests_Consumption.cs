using System.Globalization;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Erkennung des optionalen Startverbrauchs (Dezimalkomma, einzelner Punkt, leer) und die Fehlerarten.
/// </summary>
public class VehicleInputParserTests_Consumption : BaseTest
{
    /// <summary>
    /// Prüft gültige Eingaben; leer ergibt <see langword="null"/>.
    /// </summary>
    /// <param name="text">Die Eingabe.</param>
    /// <param name="expected">Der erwartete Wert in invarianter Schreibweise oder <see langword="null"/>.</param>
    [Theory]
    [InlineData("", null)]
    [InlineData("5,4", "5.4")]
    [InlineData("5.4", "5.4")]
    [InlineData("16,55", "16.55")]
    [InlineData("0,1", "0.1")]
    [InlineData("99,99", "99.99")]
    public void ParseConsumption_ValidInput_ReturnsValue(string text, string? expected)
    {
        var error = VehicleInputParser.ParseConsumption(text, out var value);

        Assert.Equal(VehicleFieldError.None, error);
        Assert.Equal(expected is null ? null : decimal.Parse(expected, CultureInfo.InvariantCulture), value);
    }

    /// <summary>
    /// Prüft die Fehlerarten.
    /// </summary>
    /// <param name="text">Die Eingabe.</param>
    /// <param name="expected">Der erwartete Fehler.</param>
    [Theory]
    [InlineData("x", VehicleFieldError.NotANumber)]
    [InlineData("-1", VehicleFieldError.Negative)]
    [InlineData("0", VehicleFieldError.OutOfRange)]
    [InlineData("100", VehicleFieldError.OutOfRange)]
    [InlineData("5,123", VehicleFieldError.TooManyDecimals)]
    public void ParseConsumption_InvalidInput_ReturnsError(string text, VehicleFieldError expected)
    {
        var error = VehicleInputParser.ParseConsumption(text, out var value);

        Assert.Equal(expected, error);
        Assert.Null(value);
    }
}

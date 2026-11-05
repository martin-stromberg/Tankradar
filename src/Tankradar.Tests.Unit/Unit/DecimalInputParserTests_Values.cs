using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft <see cref="DecimalInputParser"/>: Dezimaleingaben mit Komma oder Punkt, Nachkommastellen, Bereich und Fehlerfälle.
/// </summary>
public class DecimalInputParserTests_Values : BaseTest
{
    /// <summary>
    /// Prüft gültige Eingaben einschließlich leerer Eingabe.
    /// </summary>
    /// <param name="text">Die Eingabe.</param>
    /// <param name="expected">Der erwartete Wert als Text (invariant) oder <see langword="null"/>.</param>
    [Theory]
    [InlineData("40", "40")]
    [InlineData("40,5", "40.5")]
    [InlineData("1.859", "1.859")]
    [InlineData(" 1,859 ", "1.859")]
    [InlineData(",5", "0.5")]
    [InlineData("12,340", "12.34")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void Parse_ValidInput_ReturnsValue(string? text, string? expected)
    {
        var error = DecimalInputParser.Parse(text, 3, 1000m, out var value);

        Assert.Equal(TankbookEntryFieldError.None, error);
        Assert.Equal(expected is null ? null : decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), value);
    }

    /// <summary>
    /// Prüft fehlerhafte Eingaben.
    /// </summary>
    /// <param name="text">Die Eingabe.</param>
    /// <param name="expected">Der erwartete Fehler.</param>
    [Theory]
    [InlineData("abc", TankbookEntryFieldError.NotANumber)]
    [InlineData("1.234,5", TankbookEntryFieldError.NotANumber)]
    [InlineData("1,2,3", TankbookEntryFieldError.NotANumber)]
    [InlineData("-5", TankbookEntryFieldError.Negative)]
    [InlineData("0", TankbookEntryFieldError.OutOfRange)]
    [InlineData("1000", TankbookEntryFieldError.OutOfRange)]
    [InlineData("1,2345", TankbookEntryFieldError.TooManyDecimals)]
    public void Parse_InvalidInput_ReturnsError(string text, TankbookEntryFieldError expected)
    {
        var error = DecimalInputParser.Parse(text, 3, 1000m, out var value);

        Assert.Equal(expected, error);
        Assert.Null(value);
    }
}

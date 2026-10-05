using Tankradar.MAUI.Models.Search;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Validierung des Suchradius (ganze Zahl von 1 bis 25, Standard 5).
/// </summary>
public class SearchRadiusTests_Validation : BaseTest
{
    /// <summary>
    /// Prüft die Konstanten des Radius.
    /// </summary>
    [Fact]
    public void Constants_AreOneToTwentyFiveWithDefaultFive()
    {
        Assert.Equal(1, SearchRadius.Min);
        Assert.Equal(25, SearchRadius.Max);
        Assert.Equal(5, SearchRadius.Default);
    }

    /// <summary>
    /// Prüft gültige Eingaben einschließlich der Grenzen.
    /// </summary>
    /// <param name="text">Die Eingabe.</param>
    /// <param name="expected">Der erwartete Radius.</param>
    [Theory]
    [InlineData("1", 1)]
    [InlineData("5", 5)]
    [InlineData("25", 25)]
    [InlineData(" 10 ", 10)]
    [InlineData("007", 7)]
    public void TryParse_ValidInput_ReturnsRadius(string text, int expected)
    {
        Assert.True(SearchRadius.TryParse(text, out var radius));
        Assert.Equal(expected, radius);
    }

    /// <summary>
    /// Prüft ungültige Eingaben: außerhalb des Bereichs, Dezimalzahlen, Text, leer, sehr groß, Vorzeichen.
    /// </summary>
    /// <param name="text">Die Eingabe.</param>
    [Theory]
    [InlineData("0")]
    [InlineData("26")]
    [InlineData("-1")]
    [InlineData("+5")]
    [InlineData("5.5")]
    [InlineData("5,5")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("99999999999999999999")]
    [InlineData("５")]
    public void TryParse_InvalidInput_ReturnsFalse(string? text)
    {
        Assert.False(SearchRadius.TryParse(text, out var radius));
        Assert.Equal(0, radius);
    }
}

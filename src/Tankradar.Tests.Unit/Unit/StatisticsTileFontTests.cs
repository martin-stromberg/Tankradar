using Tankradar.MAUI.Resources;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Schriftgröße der Kachelzahlen nach Textlänge.
/// </summary>
public class StatisticsTileFontTests : BaseTest
{
    /// <summary>
    /// Prüft die Stufen der Schriftgröße von 1 bis 12 Zeichen.
    /// </summary>
    /// <param name="length">Die Länge des Textes.</param>
    /// <param name="expected">Die erwartete Schriftgröße.</param>
    [Theory]
    [InlineData(1, 28)]
    [InlineData(4, 28)]
    [InlineData(6, 28)]
    [InlineData(7, 24)]
    [InlineData(8, 21)]
    [InlineData(9, 18)]
    [InlineData(12, 18)]
    public void SizeFor_ByLength(int length, double expected)
    {
        Assert.Equal(expected, StatisticsTileFont.SizeFor(new string('9', length)));
    }

    /// <summary>
    /// Prüft, dass die Schriftgröße mit der Länge nie wächst und nie unter das Minimum fällt.
    /// </summary>
    [Fact]
    public void SizeFor_IsMonotonic()
    {
        var previous = double.MaxValue;
        for (var length = 0; length <= 40; length++)
        {
            var size = StatisticsTileFont.SizeFor(new string('9', length));
            Assert.True(size <= previous);
            Assert.True(size >= StatisticsTileFont.MinimumSize);
            previous = size;
        }
    }

    /// <summary>
    /// Prüft, dass der Platzhalter „–“ und <see langword="null"/> die Standardgröße erhalten und „9.989,00“ verkleinert wird.
    /// </summary>
    [Fact]
    public void SizeFor_TypicalValues()
    {
        Assert.Equal(28, StatisticsTileFont.SizeFor("–"));
        Assert.Equal(28, StatisticsTileFont.SizeFor(null));
        Assert.Equal(28, StatisticsTileFont.SizeFor("6,00"));
        Assert.Equal(21, StatisticsTileFont.SizeFor("9.989,00"));
    }
}

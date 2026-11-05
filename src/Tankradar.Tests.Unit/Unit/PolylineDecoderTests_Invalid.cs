using Tankradar.MAUI.Services.Routing;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass fehlerhafte, abgeschnittene oder zu große kodierte Linien abgelehnt werden.
/// </summary>
public class PolylineDecoderTests_Invalid : BaseTest
{
    /// <summary>
    /// Prüft, dass leere Eingaben abgelehnt werden.
    /// </summary>
    /// <param name="encoded">Die Eingabe.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void TryDecode_EmptyInput_ReturnsFalse(string? encoded)
    {
        Assert.False(PolylineDecoder.TryDecode(encoded, 5, 100, out var points));
        Assert.Empty(points);
    }

    /// <summary>
    /// Prüft, dass eine abgeschnittene Eingabe (Wert nicht abgeschlossen) abgelehnt wird.
    /// </summary>
    [Fact]
    public void TryDecode_TruncatedValue_ReturnsFalse()
    {
        var encoded = PolylineEncoder.Encode([(52.52, 13.405), (52.6, 13.5)]);

        Assert.False(PolylineDecoder.TryDecode(encoded[..^1], 5, 100, out _));
    }

    /// <summary>
    /// Prüft, dass ein Punkt ohne Längengrad (ungerade Zahl von Werten) abgelehnt wird.
    /// </summary>
    [Fact]
    public void TryDecode_MissingLongitude_ReturnsFalse()
    {
        Assert.False(PolylineDecoder.TryDecode("_p~iF", 5, 100, out _));
    }

    /// <summary>
    /// Prüft, dass Zeichen außerhalb des erlaubten Bereichs abgelehnt werden.
    /// </summary>
    /// <param name="encoded">Die Eingabe.</param>
    [Theory]
    [InlineData("_p~iF ~ps|U")]
    [InlineData("_p~iFä~ps|U")]
    [InlineData(">>>>>>")]
    public void TryDecode_InvalidCharacters_ReturnsFalse(string encoded)
    {
        Assert.False(PolylineDecoder.TryDecode(encoded, 5, 100, out _));
    }

    /// <summary>
    /// Prüft, dass mehr Punkte als erlaubt abgelehnt werden.
    /// </summary>
    [Fact]
    public void TryDecode_MoreThanMaxPoints_ReturnsFalse()
    {
        var source = Enumerable.Range(0, 11).Select(i => (52.0 + (i * 0.001), 13.0)).ToList();

        Assert.False(PolylineDecoder.TryDecode(PolylineEncoder.Encode(source), 5, 10, out var points));
        Assert.Empty(points);
    }

    /// <summary>
    /// Prüft, dass ein Überlauf durch endlos fortgesetzte Werte nicht zu einem Ergebnis führt.
    /// </summary>
    [Fact]
    public void TryDecode_EndlessContinuation_ReturnsFalse()
    {
        Assert.False(PolylineDecoder.TryDecode(new string('~', 40), 5, 100, out _));
    }

    /// <summary>
    /// Prüft, dass Koordinaten außerhalb des gültigen Bereichs abgelehnt werden.
    /// </summary>
    [Fact]
    public void TryDecode_OutOfRangeCoordinate_ReturnsFalse()
    {
        Assert.False(PolylineDecoder.TryDecode(PolylineEncoder.Encode([(95.0, 13.0)]), 5, 100, out _));
    }

    /// <summary>
    /// Prüft, dass eine unzulässige Genauigkeit oder Obergrenze abgelehnt wird.
    /// </summary>
    [Fact]
    public void TryDecode_InvalidParameters_ReturnsFalse()
    {
        var encoded = PolylineEncoder.Encode([(52.52, 13.405)]);

        Assert.False(PolylineDecoder.TryDecode(encoded, 0, 100, out _));
        Assert.False(PolylineDecoder.TryDecode(encoded, 5, 0, out _));
    }
}

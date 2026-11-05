using Tankradar.MAUI.Services.Routing;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Dekodierung kodierter Linien (bekanntes Beispiel, negative Werte, Round-Trip mit dem Testkodierer).
/// </summary>
public class PolylineDecoderTests_Decode : BaseTest
{
    /// <summary>
    /// Prüft das Beispiel der Dokumentation des Verfahrens.
    /// </summary>
    [Fact]
    public void TryDecode_KnownExample_ReturnsExpectedPoints()
    {
        var ok = PolylineDecoder.TryDecode("_p~iF~ps|U_ulLnnqC_mqNvxq`@", 5, 100, out var points);

        Assert.True(ok);
        Assert.Equal(3, points.Count);
        Assert.Equal(38.5, points[0].Latitude, 5);
        Assert.Equal(-120.2, points[0].Longitude, 5);
        Assert.Equal(40.7, points[1].Latitude, 5);
        Assert.Equal(-120.95, points[1].Longitude, 5);
        Assert.Equal(43.252, points[2].Latitude, 5);
        Assert.Equal(-126.453, points[2].Longitude, 5);
    }

    /// <summary>
    /// Prüft den Round-Trip mit negativen Werten und Rückwärtsschritten.
    /// </summary>
    [Fact]
    public void TryDecode_RoundTripWithNegativeDeltas_RestoresPoints()
    {
        (double, double)[] source = [(52.52, 13.405), (52.51234, 13.39876), (-33.86785, 151.20732), (-33.9, 151.1)];

        var ok = PolylineDecoder.TryDecode(PolylineEncoder.Encode(source), 5, 100, out var points);

        Assert.True(ok);
        Assert.Equal(source.Length, points.Count);
        for (var i = 0; i < source.Length; i++)
        {
            Assert.Equal(source[i].Item1, points[i].Latitude, 5);
            Assert.Equal(source[i].Item2, points[i].Longitude, 5);
        }
    }

    /// <summary>
    /// Prüft die Genauigkeit 6.
    /// </summary>
    [Fact]
    public void TryDecode_PrecisionSix_RestoresPoints()
    {
        (double, double)[] source = [(52.520123, 13.405456), (52.521, 13.406)];

        var ok = PolylineDecoder.TryDecode(PolylineEncoder.Encode(source, 6), 6, 100, out var points);

        Assert.True(ok);
        Assert.Equal(52.520123, points[0].Latitude, 6);
        Assert.Equal(13.406, points[1].Longitude, 6);
    }

    /// <summary>
    /// Prüft, dass genau die erlaubte Punktzahl noch dekodiert wird.
    /// </summary>
    [Fact]
    public void TryDecode_ExactlyMaxPoints_IsAccepted()
    {
        var source = Enumerable.Range(0, 10).Select(i => (52.0 + (i * 0.001), 13.0)).ToList();

        Assert.True(PolylineDecoder.TryDecode(PolylineEncoder.Encode(source), 5, 10, out var points));
        Assert.Equal(10, points.Count);
    }
}

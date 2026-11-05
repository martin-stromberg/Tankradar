using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Testkodierer gegen das bekannte Beispiel des Verfahrens.
/// </summary>
public class PolylineEncoderTests : BaseTest
{
    /// <summary>
    /// Prüft das Beispiel der Dokumentation des Verfahrens.
    /// </summary>
    [Fact]
    public void Encode_KnownExample_MatchesReference()
    {
        var encoded = PolylineEncoder.Encode([(38.5, -120.2), (40.7, -120.95), (43.252, -126.453)]);

        Assert.Equal("_p~iF~ps|U_ulLnnqC_mqNvxq`@", encoded);
    }

    /// <summary>
    /// Prüft, dass eine leere Punktfolge eine leere Zeichenkette ergibt.
    /// </summary>
    [Fact]
    public void Encode_NoPoints_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, PolylineEncoder.Encode([]));
    }
}

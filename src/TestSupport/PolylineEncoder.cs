using System.Text;

namespace Tankradar.TestSupport;

/// <summary>
/// Kodiert Punktfolgen im Format der „Encoded Polyline Algorithm“ (Gegenstück zum Dekodierer der App); ausschließlich für Test-Mocks und Tests.
/// </summary>
public static class PolylineEncoder
{
    /// <summary>
    /// Kodiert die Punkte.
    /// </summary>
    /// <param name="points">Die Punkte als Breiten- und Längengrad.</param>
    /// <param name="precision">Die Anzahl der Nachkommastellen (5 bei OpenRouteService).</param>
    /// <returns>Die kodierte Linie.</returns>
    public static string Encode(IEnumerable<(double Latitude, double Longitude)> points, int precision = 5)
    {
        ArgumentNullException.ThrowIfNull(points);
        var factor = Math.Pow(10, precision);
        var builder = new StringBuilder();
        long previousLatitude = 0;
        long previousLongitude = 0;
        foreach (var (latitude, longitude) in points)
        {
            var scaledLatitude = (long)Math.Round(latitude * factor, MidpointRounding.AwayFromZero);
            var scaledLongitude = (long)Math.Round(longitude * factor, MidpointRounding.AwayFromZero);
            EncodeValue(builder, scaledLatitude - previousLatitude);
            EncodeValue(builder, scaledLongitude - previousLongitude);
            previousLatitude = scaledLatitude;
            previousLongitude = scaledLongitude;
        }

        return builder.ToString();
    }

    private static void EncodeValue(StringBuilder builder, long delta)
    {
        var value = delta < 0 ? ~(delta << 1) : delta << 1;
        while (value >= 0x20)
        {
            builder.Append((char)((0x20 | (int)(value & 0x1f)) + 63));
            value >>= 5;
        }

        builder.Append((char)(value + 63));
    }
}

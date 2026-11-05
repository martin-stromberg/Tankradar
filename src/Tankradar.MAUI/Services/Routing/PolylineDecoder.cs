using Tankradar.MAUI.Models.Search;

namespace Tankradar.MAUI.Services.Routing;

/// <summary>
/// Dekodiert Linien im Format der „Encoded Polyline Algorithm“ (Genauigkeit 5 oder 6). Fehlerhafte oder zu große Eingaben werden abgelehnt,
/// ohne dass es zu Überläufen oder unbegrenztem Speicherbedarf kommt.
/// </summary>
public static class PolylineDecoder
{
    private const int MaxCharsPerPoint = 20;
    private const int MaxChunksPerValue = 7;

    /// <summary>
    /// Versucht, eine kodierte Linie zu dekodieren.
    /// </summary>
    /// <param name="encoded">Die kodierte Linie.</param>
    /// <param name="precision">Die Anzahl der Nachkommastellen (1 bis 7).</param>
    /// <param name="maxPoints">Die höchste zulässige Punktzahl.</param>
    /// <param name="points">Die Punkte bei Erfolg, sonst eine leere Liste.</param>
    /// <returns><see langword="true"/>, wenn die Linie vollständig und gültig dekodiert wurde.</returns>
    public static bool TryDecode(string? encoded, int precision, int maxPoints, out IReadOnlyList<GeoPosition> points)
    {
        points = [];
        if (string.IsNullOrEmpty(encoded) || precision is < 1 or > 7 || maxPoints < 1 || encoded.Length > (long)maxPoints * MaxCharsPerPoint)
        {
            return false;
        }

        var factor = Math.Pow(10, precision);
        var result = new List<GeoPosition>();
        var index = 0;
        long latitude = 0;
        long longitude = 0;
        while (index < encoded.Length)
        {
            if (result.Count >= maxPoints
                || !TryReadValue(encoded, ref index, out var latitudeDelta)
                || !TryReadValue(encoded, ref index, out var longitudeDelta))
            {
                return false;
            }

            latitude += latitudeDelta;
            longitude += longitudeDelta;
            if (!GeoPosition.TryCreate(latitude / factor, longitude / factor, out var position) || position is null)
            {
                return false;
            }

            result.Add(position);
        }

        points = result;
        return result.Count > 0;
    }

    private static bool TryReadValue(string encoded, ref int index, out long value)
    {
        value = 0;
        long accumulated = 0;
        var shift = 0;
        var chunks = 0;
        int chunk;
        do
        {
            if (index >= encoded.Length || chunks >= MaxChunksPerValue)
            {
                return false;
            }

            chunk = encoded[index++] - 63;
            if (chunk is < 0 or > 63)
            {
                return false;
            }

            accumulated |= (long)(chunk & 0x1f) << shift;
            shift += 5;
            chunks++;
        }
        while (chunk >= 0x20);

        value = (accumulated & 1) != 0 ? ~(accumulated >> 1) : accumulated >> 1;
        return true;
    }
}

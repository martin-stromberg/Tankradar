namespace Tankradar.MAUI.Services.Map;

/// <summary>
/// Erkennt einen Doppeltipp (Doppelklick) aus Lage und Zeitpunkt aufeinanderfolgender Tipps. Plattformunabhängig; die Zeit wird übergeben, damit die Erkennung ohne Uhr prüfbar ist.
/// </summary>
public sealed class MapDoubleTapDetector
{
    /// <summary>
    /// Der größte Abstand zwischen zwei Tipps in Millisekunden.
    /// </summary>
    public const long MaxIntervalMs = 500;

    /// <summary>
    /// Der größte Abstand zwischen zwei Tipps in Pixeln.
    /// </summary>
    public const double MaxDistance = 4;

    private bool _hasPrevious;
    private double _previousX;
    private double _previousY;
    private long _previousTimestampMs;

    /// <summary>
    /// Meldet einen Tipp.
    /// </summary>
    /// <param name="x">Horizontale Lage in Pixeln.</param>
    /// <param name="y">Vertikale Lage in Pixeln.</param>
    /// <param name="timestampMs">Der Zeitpunkt in Millisekunden.</param>
    /// <returns><see langword="true"/>, wenn der Tipp einen Doppeltipp abschließt; danach beginnt eine neue Folge.</returns>
    public bool Register(double x, double y, long timestampMs)
    {
        var isDouble = _hasPrevious
            && timestampMs - _previousTimestampMs >= 0
            && timestampMs - _previousTimestampMs <= MaxIntervalMs
            && Math.Sqrt(((x - _previousX) * (x - _previousX)) + ((y - _previousY) * (y - _previousY))) <= MaxDistance;
        if (isDouble)
        {
            _hasPrevious = false;
            return true;
        }

        _hasPrevious = true;
        _previousX = x;
        _previousY = y;
        _previousTimestampMs = timestampMs;
        return false;
    }
}

using Tankradar.MAUI.Models.Map;

namespace Tankradar.MAUI.Services.Map;

/// <summary>
/// Übersetzt Mauseingaben (Ziehen mit gedrückter Taste, Mausrad) in Verschiebungen und Zoomschritte der Karte. Plattformunabhängig, damit die Logik ohne Oberfläche prüfbar ist;
/// die Windows-Oberfläche meldet nur die Zeigerereignisse.
/// </summary>
public sealed class MapPointerTracker
{
    /// <summary>
    /// Der Wert einer Mausradraste in der Einheit der Zeigerereignisse von Windows.
    /// </summary>
    public const int WheelNotch = 120;

    private double _lastX;
    private double _lastY;
    private int _wheelRemainder;

    /// <summary>
    /// Gibt an, ob gerade mit gedrückter Maustaste gezogen wird.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public bool IsDragging { get; private set; }

    /// <summary>
    /// Beginnt das Ziehen an der angegebenen Position.
    /// </summary>
    /// <param name="x">Horizontale Position in Pixeln.</param>
    /// <param name="y">Vertikale Position in Pixeln.</param>
    public void Press(double x, double y)
    {
        IsDragging = true;
        _lastX = x;
        _lastY = y;
    }

    /// <summary>
    /// Meldet eine Mausbewegung und liefert die Verschiebung der Karte seit der letzten Meldung.
    /// </summary>
    /// <param name="x">Horizontale Position in Pixeln.</param>
    /// <param name="y">Vertikale Position in Pixeln.</param>
    /// <returns>Die Verschiebung der Karte in Pixeln; <see langword="null"/>, wenn nicht gezogen wird.</returns>
    public ScreenPoint? Move(double x, double y)
    {
        if (!IsDragging)
        {
            return null;
        }

        var delta = new ScreenPoint(x - _lastX, y - _lastY);
        _lastX = x;
        _lastY = y;
        return delta;
    }

    /// <summary>
    /// Beendet das Ziehen (Taste losgelassen oder Zeiger verloren).
    /// </summary>
    public void Release()
    {
        IsDragging = false;
    }

    /// <summary>
    /// Meldet eine Drehung des Mausrads und liefert die daraus folgenden ganzzahligen Zoomstufen; Teilrasten (feinauflösende Mäuse und Touchpads) summieren sich.
    /// </summary>
    /// <param name="delta">Die Raddrehung (positiv: vom Anwender weg, vergrößert; eine Raste sind <see cref="WheelNotch"/>).</param>
    /// <returns>Die Änderung der Zoomstufe (positiv vergrößert, negativ verkleinert, 0 bei unvollständiger Raste).</returns>
    public int Wheel(int delta)
    {
        _wheelRemainder += delta;
        var steps = _wheelRemainder / WheelNotch;
        _wheelRemainder -= steps * WheelNotch;
        return steps;
    }
}

using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Charts;

/// <summary>
/// Ein berechneter Datenpunkt eines Diagramms mit Koordinaten.
/// </summary>
/// <param name="Source">Der Datenpunkt.</param>
/// <param name="Point">Der Punkt (Linie) bzw. die Mitte der Balkenoberkante (Balken).</param>
/// <param name="Bar">Das Rechteck des Balkens; bei einem Liniendiagramm leer.</param>
/// <param name="ShowValue">Gibt an, ob der Wert am Datenpunkt gezeichnet wird.</param>
/// <param name="ShowLabel">Gibt an, ob die Beschriftung der Achse gezeichnet wird (höchstens 4 je Diagramm).</param>
/// <returns>Der Wert.</returns>
public sealed record ChartItem(ChartPoint Source, PointF Point, RectF Bar, bool ShowValue, bool ShowLabel);

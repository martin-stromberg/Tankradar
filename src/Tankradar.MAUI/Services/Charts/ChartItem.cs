using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Charts;

/// <summary>
/// Ein berechneter Datenpunkt eines Diagramms mit Koordinaten.
/// </summary>
/// <param name="Source">Der Datenpunkt.</param>
/// <param name="Point">Die Mitte der Balkenoberkante.</param>
/// <param name="Bar">Das Rechteck des Balkens.</param>
/// <param name="ShowValue">Gibt an, ob der Wert am Datenpunkt gezeichnet wird.</param>
/// <param name="ShowLabel">Gibt an, ob die Beschriftung der Achse gezeichnet wird (höchstens 4 je Diagramm).</param>
/// <param name="ValueArea">Fläche und Ausrichtung des Wertes über dem Balken.</param>
/// <param name="LabelArea">Fläche und Ausrichtung der Achsenbeschriftung unter dem Balken.</param>
/// <param name="IsLast">Gibt an, ob es der letzte (neueste) Balken ist, der hervorgehoben wird.</param>
/// <returns>Der Wert.</returns>
public sealed record ChartItem(ChartPoint Source, PointF Point, RectF Bar, bool ShowValue, bool ShowLabel, ChartTextArea ValueArea, ChartTextArea LabelArea, bool IsLast);

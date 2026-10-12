namespace Tankradar.MAUI.Services.Charts;

/// <summary>
/// Die Fläche und Ausrichtung eines Textes im Diagramm (Wert oder Achsenbeschriftung). Die Fläche liegt immer innerhalb der Diagrammfläche,
/// damit der Text am Rand nicht abgeschnitten wird: am linken Rand linksbündig, am rechten Rand rechtsbündig, sonst mittig.
/// </summary>
/// <param name="Area">Die Fläche des Textes.</param>
/// <param name="Alignment">Die waagerechte Ausrichtung des Textes in der Fläche.</param>
/// <returns>Der Wert.</returns>
public sealed record ChartTextArea(RectF Area, HorizontalAlignment Alignment);

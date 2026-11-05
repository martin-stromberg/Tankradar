namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Ein Datenpunkt eines Diagramms.
/// </summary>
/// <param name="Label">Die Beschriftung der Achse, z. B. ein Datum oder ein Monat.</param>
/// <param name="Value">Der Wert.</param>
/// <param name="ValueText">Der am Datenpunkt angezeigte Wert, bereits formatiert.</param>
/// <returns>Der Wert.</returns>
public sealed record ChartPoint(string Label, decimal Value, string ValueText);

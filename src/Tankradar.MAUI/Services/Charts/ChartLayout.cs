namespace Tankradar.MAUI.Services.Charts;

/// <summary>
/// Das Ergebnis der Layoutberechnung eines Diagramms.
/// </summary>
/// <param name="Plot">Der Zeichenbereich der Werte.</param>
/// <param name="Minimum">Der Wert am unteren Rand des Zeichenbereichs.</param>
/// <param name="Maximum">Der Wert am oberen Rand des Zeichenbereichs.</param>
/// <param name="Items">Die Datenpunkte von links nach rechts (höchstens <see cref="Tankradar.MAUI.Models.Tankbook.TankbookStatisticsLimits.ChartMaxPoints"/>, die neuesten).</param>
/// <returns>Der Wert.</returns>
public sealed record ChartLayout(RectF Plot, decimal Minimum, decimal Maximum, IReadOnlyList<ChartItem> Items);

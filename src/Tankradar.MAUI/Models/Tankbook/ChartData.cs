namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Die Daten eines Balkendiagramms; der letzte Balken (neuester Wert) wird hervorgehoben.
/// </summary>
/// <param name="Points">Die Datenpunkte von links nach rechts.</param>
/// <param name="Unit">Die Einheit der Werte.</param>
/// <returns>Der Wert.</returns>
public sealed record ChartData(IReadOnlyList<ChartPoint> Points, string Unit)
{
    /// <summary>
    /// Gibt an, ob es Datenpunkte gibt.
    /// </summary>
    public bool HasData
    {
        get { return Points.Count > 0; }
    }
}

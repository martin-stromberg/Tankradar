namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Grenzen der Darstellung der Auswertung.
/// </summary>
public static class TankbookStatisticsLimits
{
    /// <summary>
    /// Höchstzahl der Datenpunkte eines Diagramms (die neuesten); die Kennzahlen und die Monatsliste enthalten alle Daten.
    /// </summary>
    public const int ChartMaxPoints = 12;

    /// <summary>
    /// Höchstzahl der angezeigten Zeilen der Monatsliste (die neuesten Monate); Kennzahlen und Gesamtkosten enthalten alle Daten.
    /// </summary>
    public const int MonthListMaxRows = 24;
}

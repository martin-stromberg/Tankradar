namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Kosten einer Art in einem Kalendermonat.
/// </summary>
/// <param name="Year">Das Jahr.</param>
/// <param name="Month">Der Monat (1 bis 12).</param>
/// <param name="TotalCost">Die Summe der Gesamtbeträge in Euro; 0 bei einem Monat ohne Vorgang.</param>
/// <param name="EntryCount">Die Zahl der Vorgänge im Monat.</param>
/// <returns>Der Wert.</returns>
public sealed record MonthlyCostSummary(int Year, int Month, decimal TotalCost, int EntryCount);

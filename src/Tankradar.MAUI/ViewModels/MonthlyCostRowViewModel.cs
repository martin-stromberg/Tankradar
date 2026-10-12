using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Eine Zeile der Monatsliste der Auswertung, z. B. „Oktober 2026: 74,36 €“.
/// </summary>
public sealed class MonthlyCostRowViewModel
{
    /// <summary>
    /// Erstellt die Zeile.
    /// </summary>
    /// <param name="month">Die Kosten des Monats.</param>
    /// <param name="automationKey">Der Schlüssel der Art für die AutomationId (<c>Fuel</c> bzw. <c>Electricity</c>).</param>
    public MonthlyCostRowViewModel(MonthlyCostSummary month, string automationKey)
    {
        ArgumentNullException.ThrowIfNull(month);
        ArgumentNullException.ThrowIfNull(automationKey);
        Text = TankbookStatisticsTexts.FormatMonthRow(month);
        AutomationId = $"TankbookEntries.Statistics.{automationKey}.Month";
    }

    /// <summary>
    /// Die AutomationId der Zeile; je Zeile einer Art identisch.
    /// </summary>
    public string AutomationId { get; }

    /// <summary>
    /// Der Text der Zeile.
    /// </summary>
    public string Text { get; }
}

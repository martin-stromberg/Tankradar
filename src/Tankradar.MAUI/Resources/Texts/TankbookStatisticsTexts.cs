using System.Globalization;
using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Resources.Texts;

/// <summary>
/// Zentrale deutsche UI-Texte und Formatierungen (de-DE) der Auswertung der Tank- und Ladevorgänge.
/// </summary>
public static class TankbookStatisticsTexts
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>
    /// Überschrift des Bereichs.
    /// </summary>
    public const string Heading = "Auswertung";

    /// <summary>
    /// Hinweis, solange es keine Vorgänge gibt.
    /// </summary>
    public const string NoEntriesNotice = "Noch keine Auswertung: Sie erscheint, sobald Tank- oder Ladevorgänge erfasst sind.";

    /// <summary>
    /// Fehlermeldung, wenn die Auswertung nicht geladen werden konnte.
    /// </summary>
    public const string LoadFailed = "Die Auswertung konnte nicht geladen werden.";

    /// <summary>
    /// Hinweis, wenn für den Verbrauch zu wenige volle Vorgänge vorliegen.
    /// </summary>
    public const string TooFewEntriesNotice = "Für den Verbrauch werden mindestens zwei volle Vorgänge benötigt. Alternativ kannst du am Fahrzeug einen Startverbrauch hinterlegen.";

    /// <summary>
    /// Beschriftung des Durchschnittsverbrauchs.
    /// </summary>
    public const string AverageLabel = "Ø Verbrauch";

    /// <summary>
    /// Beschriftung der Kosten pro 100 km.
    /// </summary>
    public const string CostPer100KmLabel = "Kosten pro 100 km";

    /// <summary>
    /// Beschriftung der Gesamtkosten.
    /// </summary>
    public const string TotalCostLabel = "Gesamtkosten";

    /// <summary>
    /// Kennzeichnung eines aus vollen Vorgängen berechneten Durchschnitts.
    /// </summary>
    public const string CalculatedSource = "Berechnet aus vollen Vorgängen";

    /// <summary>
    /// Kennzeichnung des Startverbrauchs als Ersatzwert.
    /// </summary>
    public const string StartConsumptionSource = "Startverbrauch – noch keine zwei vollen Vorgänge";

    /// <summary>
    /// Text, wenn die Kosten pro 100 km noch nicht berechnet werden können.
    /// </summary>
    public const string NotCalculable = "noch nicht berechenbar";

    /// <summary>
    /// Text, wenn kein Durchschnittsverbrauch vorliegt.
    /// </summary>
    public const string NoAverage = "–";

    /// <summary>
    /// Überschrift der Monatskosten.
    /// </summary>
    public const string MonthsHeading = "Monatskosten";

    /// <summary>
    /// Überschrift des Verbrauchsverlaufs.
    /// </summary>
    public const string ConsumptionChartHeading = "Verbrauchsverlauf";

    /// <summary>
    /// Überschrift des Kostenverlaufs.
    /// </summary>
    public const string CostChartHeading = "Kostenverlauf";

    /// <summary>
    /// Zusammenfassung eines Diagramms ohne Daten.
    /// </summary>
    public const string NoChartData = "Noch kein Verlauf: Es liegen keine berechneten Werte vor.";

    /// <summary>
    /// Formatiert den Durchschnittsverbrauch, z. B. „6,00 L/100 km“.
    /// </summary>
    /// <param name="value">Der Verbrauch; <see langword="null"/>, wenn es keinen gibt.</param>
    /// <param name="kind">Die Art.</param>
    /// <returns>Der Text.</returns>
    public static string FormatAverage(decimal? value, TankbookEntryKind kind)
    {
        return value is { } consumption ? TankbookEntryTexts.FormatConsumption(consumption, kind) : NoAverage;
    }

    /// <summary>
    /// Formatiert die Kosten pro 100 km, z. B. „12,34 € / 100 km“.
    /// </summary>
    /// <param name="value">Die Kosten; <see langword="null"/>, wenn sie noch nicht berechenbar sind.</param>
    /// <returns>Der Text.</returns>
    public static string FormatCostPer100Km(decimal? value)
    {
        return value is { } cost ? $"{TankbookEntryTexts.FormatAmount(cost)} / 100 km" : NotCalculable;
    }

    /// <summary>
    /// Liefert die Kennzeichnung der Herkunft des Durchschnittsverbrauchs.
    /// </summary>
    /// <param name="source">Die Herkunft.</param>
    /// <returns>Der Text; leer bei <see cref="TankbookAverageSource.None"/>.</returns>
    public static string GetSourceText(TankbookAverageSource source)
    {
        return source switch
        {
            TankbookAverageSource.Calculated => CalculatedSource,
            TankbookAverageSource.StartConsumption => StartConsumptionSource,
            _ => string.Empty,
        };
    }

    /// <summary>
    /// Formatiert eine Monatszeile, z. B. „Oktober 2026: 74,36 €“.
    /// </summary>
    /// <param name="month">Der Monat.</param>
    /// <returns>Der Text.</returns>
    public static string FormatMonthRow(MonthlyCostSummary month)
    {
        ArgumentNullException.ThrowIfNull(month);
        return $"{FormatMonthName(month.Year, month.Month)}: {TankbookEntryTexts.FormatAmount(month.TotalCost)}";
    }

    /// <summary>
    /// Formatiert Monat und Jahr ausgeschrieben, z. B. „Oktober 2026“.
    /// </summary>
    /// <param name="year">Das Jahr.</param>
    /// <param name="month">Der Monat (1 bis 12).</param>
    /// <returns>Der Text.</returns>
    public static string FormatMonthName(int year, int month)
    {
        return new DateTime(year, month, 1).ToString("MMMM yyyy", German);
    }

    /// <summary>
    /// Formatiert Monat und Jahr kurz für die Achse eines Diagramms, z. B. „10/26“.
    /// </summary>
    /// <param name="year">Das Jahr.</param>
    /// <param name="month">Der Monat (1 bis 12).</param>
    /// <returns>Der Text.</returns>
    public static string FormatMonthShort(int year, int month)
    {
        return new DateTime(year, month, 1).ToString("MM'/'yy", German);
    }

    /// <summary>
    /// Formatiert ein Datum kurz für die Achse eines Diagramms, z. B. „18.05.“.
    /// </summary>
    /// <param name="occurredAt">Der Zeitpunkt.</param>
    /// <returns>Der Text.</returns>
    public static string FormatDateShort(DateTime occurredAt)
    {
        return occurredAt.ToString("dd.MM.", German);
    }

    /// <summary>
    /// Formatiert einen Betrag ohne Nachkommastellen für den Wert am Balken, z. B. „74 €“.
    /// </summary>
    /// <param name="amount">Der Betrag.</param>
    /// <returns>Der Text.</returns>
    public static string FormatAmountShort(decimal amount)
    {
        return string.Create(German, $"{amount:N0} €");
    }

    /// <summary>
    /// Formatiert einen Verbrauchswert ohne Einheit für den Wert am Datenpunkt, z. B. „6,00“.
    /// </summary>
    /// <param name="consumption">Der Verbrauch.</param>
    /// <returns>Der Text.</returns>
    public static string FormatValueShort(decimal consumption)
    {
        return string.Create(German, $"{consumption:N2}");
    }

    /// <summary>
    /// Formatiert die Textzusammenfassung des Verbrauchsverlaufs, z. B. „Verlauf, 3 Werte, zuletzt 6,00 L/100 km, niedrigster 5,80, höchster 6,20“.
    /// </summary>
    /// <param name="points">Die dargestellten Werte, ältester zuerst.</param>
    /// <param name="kind">Die Art.</param>
    /// <returns>Der Text.</returns>
    public static string FormatConsumptionSummary(IReadOnlyList<decimal> points, TankbookEntryKind kind)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0)
        {
            return NoChartData;
        }

        return $"Verlauf, {FormatValueCount(points.Count)}, zuletzt {TankbookEntryTexts.FormatConsumption(points[^1], kind)}, niedrigster {FormatValueShort(points.Min())}, höchster {FormatValueShort(points.Max())}";
    }

    /// <summary>
    /// Formatiert die Textzusammenfassung des Kostenverlaufs, z. B. „Monatskosten, 3 Werte, zuletzt 74,36 €, niedrigster 0,00 €, höchster 120,00 €“.
    /// </summary>
    /// <param name="amounts">Die dargestellten Monatskosten, ältester Monat zuerst.</param>
    /// <returns>Der Text.</returns>
    public static string FormatCostSummary(IReadOnlyList<decimal> amounts)
    {
        ArgumentNullException.ThrowIfNull(amounts);
        if (amounts.Count == 0)
        {
            return NoChartData;
        }

        return $"Monatskosten, {FormatValueCount(amounts.Count)}, zuletzt {TankbookEntryTexts.FormatAmount(amounts[^1])}, niedrigster {TankbookEntryTexts.FormatAmount(amounts.Min())}, höchster {TankbookEntryTexts.FormatAmount(amounts.Max())}";
    }

    private static string FormatValueCount(int count)
    {
        return count == 1 ? "1 Wert" : $"{count} Werte";
    }
}

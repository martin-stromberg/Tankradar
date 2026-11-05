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
    public const string TooFewEntriesNotice = "Für den Verbrauch werden mindestens zwei volle Vorgänge benötigt.";

    /// <summary>
    /// Hinweis, wenn für die Startart noch kein voller Vorgang vorliegt (der Start-Kilometerstand liefert die Basis des ersten).
    /// </summary>
    public const string TooFewStartEntriesNotice = "Für den Verbrauch wird mindestens ein voller Vorgang benötigt.";

    /// <summary>
    /// Zusatz zum Hinweis, wenn die Art die Startart des Fahrzeugs ist und noch kein Startverbrauch hinterlegt ist.
    /// </summary>
    public const string StartConsumptionHint = "Alternativ kannst du am Fahrzeug einen Startverbrauch hinterlegen.";

    /// <summary>
    /// Beschriftung der Monatskarte ohne Monat; der Monatsname folgt, z. B. „Gesamtkosten Oktober“.
    /// </summary>
    public const string MonthCostLabelPrefix = "Gesamtkosten";

    /// <summary>
    /// Überschrift der Monatskarte ohne Monat; Monat und Jahr folgen, z. B. „Monatsanalyse Oktober 2026“.
    /// </summary>
    public const string MonthAnalysisHeadingPrefix = "Monatsanalyse";

    /// <summary>
    /// Sprachausgabe der Kachel des Durchschnittsverbrauchs (die sichtbare Beschriftung enthält das Zeichen „Ø“).
    /// </summary>
    public const string AverageLabelDescription = "Durchschnittsverbrauch";

    /// <summary>
    /// Sprachausgabe der Kachel der Kosten pro 100 km (die sichtbare Beschriftung ist „Kosten / 100 km“).
    /// </summary>
    public const string CostPer100KmLabelDescription = "Kosten pro 100 km";

    /// <summary>
    /// Beschriftung des Durchschnittsverbrauchs.
    /// </summary>
    public const string AverageLabel = "Ø Verbrauch";

    /// <summary>
    /// Beschriftung der Kosten pro 100 km.
    /// </summary>
    public const string CostPer100KmLabel = "Kosten / 100 km";

    /// <summary>
    /// Kennzeichnung eines aus vollen Vorgängen berechneten Durchschnitts.
    /// </summary>
    public const string CalculatedSource = "Berechnet aus vollen Vorgängen";

    /// <summary>
    /// Kennzeichnung eines Durchschnitts, der allein aus dem ersten Intervall ab dem Start-Kilometerstand stammt.
    /// </summary>
    public const string FromStartSource = "Ab Startstand – angenommen wird ein voller Tank beim Start";

    /// <summary>
    /// Kennzeichnung eines Durchschnitts, der allein aus dem ersten Intervall ab dem Start-Kilometerstand stammt, bei Strom.
    /// </summary>
    public const string ElectricFromStartSource = "Ab Startstand – angenommen wird ein voller Akku beim Start";

    /// <summary>
    /// Kennzeichnung des Startverbrauchs als Ersatzwert.
    /// </summary>
    public const string StartConsumptionSource = "Startverbrauch – noch keine Strecke aus vollen Vorgängen";

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
    public const string ConsumptionChartHeading = "Verbrauchsverlauf der letzten Vorgänge";

    /// <summary>
    /// Überschrift des Kostenverlaufs.
    /// </summary>
    public const string CostChartHeading = "Kostenverlauf";

    /// <summary>
    /// Kurze Beschreibung des Verbrauchsdiagramms für die Sprachausgabe (die Zusammenfassung steht im Text darunter).
    /// </summary>
    public const string ConsumptionChartDescription = "Balkendiagramm des Verbrauchsverlaufs";

    /// <summary>
    /// Kurze Beschreibung des Kostendiagramms für die Sprachausgabe (die Zusammenfassung steht im Text darunter).
    /// </summary>
    public const string CostChartDescription = "Balkendiagramm der Monatskosten";

    /// <summary>
    /// Hinweis, dass die Monatsliste gekürzt ist.
    /// </summary>
    public const string MonthsLimitedNotice = "Angezeigt werden die letzten 24 Monate; der Durchschnitt enthält alle Vorgänge.";

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
    /// Liefert den Hinweis bei zu wenigen vollen Vorgängen; der Verweis auf den Startverbrauch steht nur, wenn er für die Art etwas ändern würde.
    /// </summary>
    /// <param name="offersStartConsumption">Gibt an, ob die Art die Startart des Fahrzeugs ist und noch kein Startverbrauch hinterlegt ist.</param>
    /// <param name="isStartKind">Gibt an, ob die Art die Startart des Fahrzeugs ist (ein voller Vorgang genügt).</param>
    /// <returns>Der Text.</returns>
    public static string GetTooFewEntriesNotice(bool offersStartConsumption, bool isStartKind = false)
    {
        var notice = isStartKind ? TooFewStartEntriesNotice : TooFewEntriesNotice;
        return offersStartConsumption ? $"{notice} {StartConsumptionHint}" : notice;
    }

    /// <summary>
    /// Formatiert den Zahlenwert des Durchschnittsverbrauchs ohne Einheit, z. B. „6,00“.
    /// </summary>
    /// <param name="value">Der Verbrauch; <see langword="null"/>, wenn es keinen gibt.</param>
    /// <returns>Der Text.</returns>
    public static string FormatAverageValue(decimal? value)
    {
        return value is { } consumption ? string.Create(German, $"{consumption:N2}") : NoAverage;
    }

    /// <summary>
    /// Formatiert den Zahlenwert der Kosten pro 100 km ohne Einheit, z. B. „11,15“.
    /// </summary>
    /// <param name="value">Die Kosten; <see langword="null"/>, wenn sie noch nicht berechenbar sind.</param>
    /// <returns>Der Text.</returns>
    public static string FormatCostPer100KmValue(decimal? value)
    {
        return value is { } cost ? string.Create(German, $"{cost:N2}") : NoAverage;
    }

    /// <summary>
    /// Formatiert die Beschriftung der Monatskarte, z. B. „Gesamtkosten Oktober“.
    /// </summary>
    /// <param name="month">Der Monat (1 bis 12).</param>
    /// <returns>Der Text.</returns>
    public static string FormatMonthCostLabel(int month)
    {
        return $"{MonthCostLabelPrefix} {new DateTime(2000, month, 1).ToString("MMMM", German)}";
    }

    /// <summary>
    /// Formatiert die Überschrift der Monatskarte, z. B. „Monatsanalyse Oktober 2026“.
    /// </summary>
    /// <param name="year">Das Jahr.</param>
    /// <param name="month">Der Monat (1 bis 12).</param>
    /// <returns>Der Text.</returns>
    public static string FormatMonthAnalysisHeading(int year, int month)
    {
        return $"{MonthAnalysisHeadingPrefix} {FormatMonthName(year, month)}";
    }

    /// <summary>
    /// Formatiert die getankte bzw. geladene Menge des Monats, z. B. „98,50 L getankt“ oder „12,00 kWh geladen“.
    /// </summary>
    /// <param name="quantity">Die Menge.</param>
    /// <param name="kind">Die Art.</param>
    /// <returns>Der Text.</returns>
    public static string FormatMonthQuantity(decimal quantity, TankbookEntryKind kind)
    {
        var verb = kind == TankbookEntryKind.Electricity ? "geladen" : "getankt";
        return string.Create(German, $"{quantity:N2} {TankbookEntryTexts.GetQuantityUnit(kind)} {verb}");
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
    /// <param name="kind">Die Art; bei Strom ist von einem vollen Akku statt einem vollen Tank die Rede.</param>
    /// <returns>Der Text; leer bei <see cref="TankbookAverageSource.None"/>.</returns>
    public static string GetSourceText(TankbookAverageSource source, TankbookEntryKind kind = TankbookEntryKind.Fuel)
    {
        return source switch
        {
            TankbookAverageSource.Calculated => CalculatedSource,
            TankbookAverageSource.FromStart => kind == TankbookEntryKind.Electricity ? ElectricFromStartSource : FromStartSource,
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
    /// <param name="includesStartValue">Gibt an, ob der älteste Wert ab dem Start-Kilometerstand berechnet wurde (wird in der Zusammenfassung genannt).</param>
    /// <returns>Der Text.</returns>
    public static string FormatConsumptionSummary(IReadOnlyList<decimal> points, TankbookEntryKind kind, bool includesStartValue = false)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0)
        {
            return NoChartData;
        }

        return $"Verlauf, {FormatValueCount(points.Count)}, zuletzt {TankbookEntryTexts.FormatConsumption(points[^1], kind)}, niedrigster {FormatValueShort(points.Min())}, höchster {FormatValueShort(points.Max())}{(includesStartValue ? ", ältester Wert ab Startstand berechnet" : string.Empty)}";
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

using System.Globalization;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.MAUI.Services.Export;

/// <summary>
/// Formatiert Werte für den Export (de-DE): in der CSV-Datei ohne Tausendertrenner, damit Zahlen als Zahlen erkennbar bleiben.
/// </summary>
public static class TankbookExportFormatter
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>
    /// Formatiert ein Datum als <c>TT.MM.JJJJ</c>.
    /// </summary>
    /// <param name="value">Der Zeitpunkt.</param>
    /// <returns>Der Text.</returns>
    public static string FormatDate(DateTime value)
    {
        return value.ToString("dd.MM.yyyy", German);
    }

    /// <summary>
    /// Formatiert eine Uhrzeit als <c>HH:mm</c>.
    /// </summary>
    /// <param name="value">Der Zeitpunkt.</param>
    /// <returns>Der Text.</returns>
    public static string FormatTime(DateTime value)
    {
        return value.ToString("HH:mm", German);
    }

    /// <summary>
    /// Formatiert eine Menge oder einen Preis pro Einheit mit zwei bis drei Nachkommastellen und Dezimalkomma, ohne Tausendertrenner, z. B. <c>40,00</c> oder <c>1,859</c>.
    /// </summary>
    /// <param name="value">Der Wert.</param>
    /// <returns>Der Text.</returns>
    public static string FormatNumber(decimal value)
    {
        return value.ToString("0.00#", German);
    }

    /// <summary>
    /// Formatiert einen Betrag oder Verbrauch mit zwei Nachkommastellen und Dezimalkomma, ohne Tausendertrenner, z. B. <c>74,36</c>.
    /// </summary>
    /// <param name="value">Der Wert.</param>
    /// <returns>Der Text.</returns>
    public static string FormatDecimal2(decimal value)
    {
        return value.ToString("0.00", German);
    }

    /// <summary>
    /// Formatiert eine ganze Zahl ohne Tausendertrenner.
    /// </summary>
    /// <param name="value">Der Wert.</param>
    /// <returns>Der Text.</returns>
    public static string FormatInteger(int value)
    {
        return value.ToString("0", German);
    }

    /// <summary>
    /// Liefert den Namen der Mengeneinheit: <c>Liter</c> bei Kraftstoff, <c>kWh</c> bei Strom.
    /// </summary>
    /// <param name="kind">Die Art.</param>
    /// <returns>Die Einheit.</returns>
    public static string GetQuantityUnitName(TankbookEntryKind kind)
    {
        return kind == TankbookEntryKind.Electricity ? "kWh" : "Liter";
    }

    /// <summary>
    /// Liefert <c>Ja</c> oder <c>Nein</c>.
    /// </summary>
    /// <param name="value">Der Wert.</param>
    /// <returns>Der Text.</returns>
    public static string FormatYesNo(bool value)
    {
        return value ? "Ja" : "Nein";
    }

    /// <summary>
    /// Liefert den Hinweis zum Verbrauch eines Vorgangs: <c>ab Startstand</c>, wenn das Intervall am Start-Kilometerstand beginnt, sonst leer.
    /// </summary>
    /// <param name="overview">Der Vorgang.</param>
    /// <returns>Der Text.</returns>
    public static string GetConsumptionNote(TankbookEntryOverview overview)
    {
        ArgumentNullException.ThrowIfNull(overview);
        return overview.Consumption is not null && overview.IsFromStart ? TankbookEntryTexts.FromStartLabel : string.Empty;
    }

    /// <summary>
    /// Formatiert den Zeitraum für den Bericht, z. B. <c>01.01.2026 bis 31.12.2026</c>, <c>ab 01.01.2026</c>, <c>bis 31.12.2026</c> oder <c>Alle Vorgänge</c>.
    /// </summary>
    /// <param name="from">Der erste Tag; <see langword="null"/> ohne untere Grenze.</param>
    /// <param name="to">Der letzte Tag; <see langword="null"/> ohne obere Grenze.</param>
    /// <returns>Der Text.</returns>
    public static string FormatRange(DateTime? from, DateTime? to)
    {
        return (from, to) switch
        {
            (null, null) => TankbookExportTexts.RangeAllText,
            ({ } start, null) => $"ab {FormatDate(start)}",
            (null, { } end) => $"bis {FormatDate(end)}",
            ({ } start, { } end) => $"{FormatDate(start)} bis {FormatDate(end)}",
        };
    }
}

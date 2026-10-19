using System.Text;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.MAUI.Services.Export;

/// <summary>
/// Erzeugt die CSV-Datei eines Exports (ADR 0013): UTF-8 mit BOM, Semikolon als Trennzeichen, Zeilenende CRLF, Dezimalkomma ohne Tausendertrenner,
/// Vorgänge chronologisch aufsteigend. Textfelder, die mit einem Formelzeichen beginnen, erhalten ein vorangestelltes Hochkomma (Schutz vor Formelinjektion).
/// </summary>
public static class TankbookCsvWriter
{
    private const char Separator = ';';
    private const string LineEnd = "\r\n";

    /// <summary>
    /// Die Spaltenüberschriften in der Reihenfolge der Datei.
    /// </summary>
    public static IReadOnlyList<string> Columns { get; } =
    [
        "Datum",
        "Uhrzeit",
        "Tankstelle",
        "Art",
        "Menge",
        "Einheit",
        "Preis pro Einheit (EUR)",
        "Gesamtbetrag (EUR)",
        "Kilometerstand",
        "Teilbetankung",
        "Strecke (km)",
        "Verbrauch",
        "Verbrauchseinheit",
        "Hinweis zum Verbrauch",
        "Notiz",
    ];

    /// <summary>
    /// Erzeugt die Bytes der CSV-Datei.
    /// </summary>
    /// <param name="data">Die Exportdaten.</param>
    /// <returns>Die Datei mit BOM.</returns>
    public static byte[] Write(TankbookExportData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var builder = new StringBuilder();
        AppendRow(builder, Columns);
        foreach (var overview in data.Entries.OrderBy(entry => entry.Entry, TankbookEntryOrder.Ascending))
        {
            var entry = overview.Entry;
            AppendRow(
                builder,
                [
                    TankbookExportFormatter.FormatDate(entry.OccurredAt),
                    TankbookExportFormatter.FormatTime(entry.OccurredAt),
                    ProtectText(entry.StationName),
                    TankbookEntryTexts.GetKindLabel(entry.Kind),
                    TankbookExportFormatter.FormatNumber(entry.Quantity),
                    TankbookExportFormatter.GetQuantityUnitName(entry.Kind),
                    TankbookExportFormatter.FormatNumber(entry.UnitPrice),
                    TankbookExportFormatter.FormatDecimal2(entry.TotalAmount),
                    TankbookExportFormatter.FormatInteger(entry.OdometerKm),
                    TankbookExportFormatter.FormatYesNo(entry.IsPartial),
                    overview.DistanceKm is { } distance ? TankbookExportFormatter.FormatInteger(distance) : string.Empty,
                    overview.Consumption is { } consumption ? TankbookExportFormatter.FormatDecimal2(consumption) : string.Empty,
                    overview.Consumption is null ? string.Empty : TankbookEntryTexts.GetConsumptionUnit(entry.Kind),
                    TankbookExportFormatter.GetConsumptionNote(overview),
                    ProtectText(entry.Note),
                ]);
        }

        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return [.. encoding.GetPreamble(), .. encoding.GetBytes(builder.ToString())];
    }

    private static void AppendRow(StringBuilder builder, IReadOnlyList<string> fields)
    {
        for (var index = 0; index < fields.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(Separator);
            }

            builder.Append(Escape(fields[index]));
        }

        builder.Append(LineEnd);
    }

    private static string ProtectText(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value[0] is '=' or '+' or '-' or '@' or '\t' or '\r' ? "'" + value : value;
    }

    private static string Escape(string field)
    {
        if (field.IndexOfAny([Separator, '"', '\r', '\n']) < 0)
        {
            return field;
        }

        return "\"" + field.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}

using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.MAUI.Services.Export;

/// <summary>
/// Erzeugt den PDF-Bericht eines Exports auf A4 hoch: Kopf mit Fahrzeug und Zeitraum, Kennzahlen und Monatskosten je Art (aus den Rechnern der Auswertung),
/// danach die Vorgangstabelle mit Seitenumbruch und wiederholter Kopfzeile; in der Fußzeile steht „Seite x von y“. Es werden keine Diagramme gezeichnet.
/// </summary>
public sealed class TankbookPdfReportWriter
{
    private const double Left = 40;
    private const double Right = 555;
    private const double TopMargin = 56;
    private const double BottomLimit = 796;
    private const double TextSize = 10;
    private const double TableSize = 8;
    private const double LineHeight = 14;
    private const double RowHeight = 12;
    private const double NoteHeight = 10;
    private const double LabelWidth = 120;
    private const double Gray = 0.45;

    // Spaltengrenzen der Vorgangstabelle: Datum, Tankstelle, Menge, Preis, Betrag, Kilometerstand, Verbrauch.
    private static readonly double[] ColumnEdges = [40, 120, 235, 297, 349, 405, 461, 555];
    private const double CellPadding = 4;

    /// <summary>
    /// Erzeugt die Bytes des Berichts.
    /// </summary>
    /// <param name="data">Die Exportdaten.</param>
    /// <returns>Die PDF-Datei.</returns>
    public byte[] Write(TankbookExportData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var builder = new PdfDocumentBuilder($"{TankbookExportTexts.ReportTitle} {data.Vehicle.Name}", data.CreatedAt);
        builder.SetFooter(TankbookExportTexts.FormatPageFooter);
        var layout = new Layout(builder);
        WriteHeader(layout, data);
        WriteKeyFigures(layout, data);
        WriteMonthlyCosts(layout, data);
        WriteEntries(layout, data);
        return builder.Build();
    }

    private static void WriteHeader(Layout layout, TankbookExportData data)
    {
        layout.Builder.DrawText(Left, layout.Y + 16, TankbookExportTexts.ReportTitle, 20, bold: true);
        layout.Y += 30;
        WriteLabeled(layout, TankbookExportTexts.ReportVehicle, $"{data.Vehicle.Name} · {TankbookTexts.FormatVehicleDetails(data.Vehicle)}");
        WriteLabeled(layout, TankbookExportTexts.ReportRange, TankbookExportFormatter.FormatRange(data.From, data.To));
        WriteLabeled(layout, TankbookExportTexts.ReportCreated, TankbookExportFormatter.FormatDate(data.CreatedAt));
        WriteLabeled(layout, TankbookExportTexts.ReportEntryCount, TankbookExportFormatter.FormatInteger(data.Entries.Count));
        layout.Y += 4;
        layout.Builder.DrawLine(Left, layout.Y, Right, layout.Y, 0.8);
        layout.Y += 18;
    }

    private static void WriteKeyFigures(Layout layout, TankbookExportData data)
    {
        if (data.Statistics.Kinds.Count == 0)
        {
            return;
        }

        layout.Ensure(40);
        layout.Builder.DrawText(Left, layout.Y + 12, TankbookExportTexts.ReportKeyFigures, 14, bold: true);
        layout.Y += 22;
        foreach (var kind in data.Statistics.Kinds)
        {
            layout.Ensure(80);
            layout.Builder.DrawText(Left, layout.Y + 10, TankbookEntryTexts.GetKindLabel(kind.Kind), 11, bold: true);
            layout.Y += 18;
            WriteLabeled(layout, TankbookStatisticsTexts.AverageLabel, TankbookStatisticsTexts.FormatAverage(kind.AverageConsumption, kind.Kind));
            var source = TankbookStatisticsTexts.GetSourceText(kind.AverageSource, kind.Kind);
            if (source.Length > 0)
            {
                layout.Builder.DrawText(Left + LabelWidth, layout.Y + 6, PdfFontMetrics.Truncate(source, false, 8, Right - Left - LabelWidth), 8, gray: Gray);
                layout.Y += 11;
            }

            WriteLabeled(layout, TankbookStatisticsTexts.CostPer100KmLabel, TankbookStatisticsTexts.FormatCostPer100Km(kind.CostPer100Km));
            WriteLabeled(layout, TankbookExportTexts.ReportTotalCost, TankbookEntryTexts.FormatAmount(kind.TotalCost));
            if (kind.CalculatedIntervals > 0)
            {
                WriteLabeled(layout, TankbookExportTexts.ReportDistance, TankbookEntryTexts.FormatKilometers(kind.TotalDistanceKm));
            }

            layout.Y += 6;
        }

        if (data.From is not null || data.To is not null)
        {
            foreach (var line in PdfFontMetrics.Wrap(TankbookExportTexts.ReportRangeNote, false, 8, Right - Left))
            {
                layout.Ensure(12);
                layout.Builder.DrawText(Left, layout.Y + 6, line, 8, gray: Gray);
                layout.Y += 11;
            }
        }

        layout.Y += 10;
    }

    private static void WriteMonthlyCosts(Layout layout, TankbookExportData data)
    {
        foreach (var kind in data.Statistics.Kinds)
        {
            layout.Ensure(50);
            layout.Builder.DrawText(Left, layout.Y + 12, $"{TankbookExportTexts.ReportMonthlyCosts} {TankbookEntryTexts.GetKindLabel(kind.Kind)}", 12, bold: true);
            layout.Y += 20;
            foreach (var month in kind.Months)
            {
                layout.Ensure(RowHeight);
                layout.Builder.DrawText(Left, layout.Y + 9, TankbookStatisticsTexts.FormatMonthName(month.Year, month.Month), TextSize);
                layout.Builder.DrawTextRight(Left + 260, layout.Y + 9, TankbookEntryTexts.FormatAmount(month.TotalCost), TextSize);
                layout.Builder.DrawTextRight(Left + 400, layout.Y + 9, TankbookEntryTexts.FormatQuantity(month.TotalQuantity, kind.Kind), TextSize, gray: Gray);
                layout.Y += RowHeight + 1;
            }

            layout.Y += 12;
        }
    }

    private static void WriteEntries(Layout layout, TankbookExportData data)
    {
        layout.Ensure(60);
        layout.Builder.DrawText(Left, layout.Y + 12, TankbookExportTexts.ReportEntries, 14, bold: true);
        layout.Y += 24;
        layout.TableHeader = () => DrawTableHeader(layout);
        layout.TableHeader();
        var hasFromStart = false;
        foreach (var overview in data.Entries)
        {
            var entry = overview.Entry;
            var hasNote = !string.IsNullOrWhiteSpace(entry.Note);
            layout.Ensure(RowHeight + (hasNote ? NoteHeight : 0) + 2);
            var baseline = layout.Y + 9;
            var fromStart = overview.Consumption is not null && overview.IsFromStart;
            hasFromStart |= fromStart;
            layout.Builder.DrawText(ColumnEdges[0], baseline, $"{TankbookExportFormatter.FormatDate(entry.OccurredAt)} {TankbookExportFormatter.FormatTime(entry.OccurredAt)}", TableSize);
            layout.Builder.DrawText(ColumnEdges[1], baseline, PdfFontMetrics.Truncate(entry.StationName, false, TableSize, ColumnEdges[2] - ColumnEdges[1] - CellPadding), TableSize);
            DrawRight(layout, 2, baseline, TankbookEntryTexts.FormatQuantity(entry.Quantity, entry.Kind));
            DrawRight(layout, 3, baseline, TankbookEntryTexts.FormatUnitPrice(entry.UnitPrice, entry.Kind));
            DrawRight(layout, 4, baseline, TankbookEntryTexts.FormatAmount(entry.TotalAmount));
            DrawRight(layout, 5, baseline, TankbookEntryTexts.FormatKilometers(entry.OdometerKm));
            layout.Builder.DrawTextRight(ColumnEdges[7], baseline, FormatConsumptionCell(overview, fromStart), TableSize);
            layout.Y += RowHeight;
            if (hasNote)
            {
                var note = PdfFontMetrics.Truncate($"{TankbookEntryTexts.NoteLabel.Split(',')[0]}: {entry.Note!.ReplaceLineEndings(" ")}", false, 7, Right - ColumnEdges[1]);
                layout.Builder.DrawText(ColumnEdges[1], layout.Y + 6, note, 7, gray: Gray);
                layout.Y += NoteHeight;
            }

            layout.Builder.DrawLine(Left, layout.Y + 1, Right, layout.Y + 1, 0.3, 0.8);
            layout.Y += 2;
        }

        layout.TableHeader = null;
        if (hasFromStart)
        {
            layout.Y += 6;
            foreach (var line in PdfFontMetrics.Wrap(TankbookExportTexts.FromStartFootnote, false, 8, Right - Left))
            {
                layout.Ensure(12);
                layout.Builder.DrawText(Left, layout.Y + 6, line, 8, gray: Gray);
                layout.Y += 11;
            }
        }
    }

    private static void DrawTableHeader(Layout layout)
    {
        var baseline = layout.Y + 9;
        layout.Builder.DrawText(ColumnEdges[0], baseline, TankbookExportTexts.ColumnDate, TableSize, bold: true);
        layout.Builder.DrawText(ColumnEdges[1], baseline, TankbookExportTexts.ColumnStation, TableSize, bold: true);
        DrawRight(layout, 2, baseline, TankbookExportTexts.ColumnQuantity, bold: true);
        DrawRight(layout, 3, baseline, TankbookExportTexts.ColumnUnitPrice, bold: true);
        DrawRight(layout, 4, baseline, TankbookExportTexts.ColumnAmount, bold: true);
        DrawRight(layout, 5, baseline, TankbookExportTexts.ColumnOdometer, bold: true);
        layout.Builder.DrawTextRight(ColumnEdges[7], baseline, TankbookExportTexts.ColumnConsumption, TableSize, bold: true);
        layout.Builder.DrawLine(Left, layout.Y + 13, Right, layout.Y + 13, 0.8);
        layout.Y += 17;
    }

    private static void DrawRight(Layout layout, int column, double baseline, string text, bool bold = false)
    {
        var width = ColumnEdges[column + 1] - ColumnEdges[column] - CellPadding;
        layout.Builder.DrawTextRight(ColumnEdges[column + 1] - CellPadding, baseline, PdfFontMetrics.Truncate(text, bold, TableSize, width), TableSize, bold);
    }

    private static string FormatConsumptionCell(TankbookEntryOverview overview, bool fromStart)
    {
        if (overview.Consumption is { } consumption)
        {
            return TankbookEntryTexts.FormatConsumption(consumption, overview.Entry.Kind) + (fromStart ? "*" : string.Empty);
        }

        return overview.Entry.IsPartial ? TankbookEntryTexts.GetPartialBadge(overview.Entry.Kind) : TankbookExportTexts.NoValue;
    }

    private static void WriteLabeled(Layout layout, string label, string value)
    {
        layout.Ensure(LineHeight);
        layout.Builder.DrawText(Left, layout.Y + 10, label, TextSize, bold: true);
        layout.Builder.DrawText(Left + LabelWidth, layout.Y + 10, PdfFontMetrics.Truncate(value, false, TextSize, Right - Left - LabelWidth), TextSize);
        layout.Y += LineHeight;
    }

    private sealed class Layout
    {
        public Layout(PdfDocumentBuilder builder)
        {
            Builder = builder;
            builder.AddPage();
            Y = TopMargin;
        }

        public PdfDocumentBuilder Builder { get; }

        public double Y { get; set; }

        public Action? TableHeader { get; set; }

        public void Ensure(double height)
        {
            if (Y + height <= BottomLimit)
            {
                return;
            }

            Builder.AddPage();
            Y = TopMargin;
            TableHeader?.Invoke();
        }
    }
}

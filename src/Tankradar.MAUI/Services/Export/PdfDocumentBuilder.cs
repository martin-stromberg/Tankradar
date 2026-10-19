using System.Globalization;
using System.Text;

namespace Tankradar.MAUI.Services.Export;

/// <summary>
/// Minimaler PDF-1.4-Schreiber ohne Abhängigkeiten: A4-Hochformat, die Grundschriften Helvetica und Helvetica-Bold (nicht eingebettet,
/// <c>WinAnsiEncoding</c>), unkomprimierte Inhaltsströme, Textzeilen und Linien, Info-Dictionary, Querverweistabelle und Seitenzahlen in der Fußzeile.
/// Alle Koordinaten sind Punkte; senkrechte Angaben zählen von der Oberkante der Seite nach unten.
/// </summary>
public sealed class PdfDocumentBuilder
{
    /// <summary>
    /// Die Seitenbreite von A4 in Punkt.
    /// </summary>
    public const double PageWidth = 595.28;

    /// <summary>
    /// Die Seitenhöhe von A4 in Punkt.
    /// </summary>
    public const double PageHeight = 841.89;

    private const double FooterRight = 555;
    private const double FooterBaseline = 812;
    private const double FooterSize = 8;
    private const double FooterGray = 0.4;

    private readonly string _title;
    private readonly DateTime _createdAt;
    private readonly List<StringBuilder> _pages = [];
    private Func<int, int, string>? _footer;

    /// <summary>
    /// Erstellt den Schreiber.
    /// </summary>
    /// <param name="title">Der Titel des Dokuments (Info-Dictionary).</param>
    /// <param name="createdAt">Der Erstellungszeitpunkt (lokale Wanduhrzeit, Info-Dictionary).</param>
    public PdfDocumentBuilder(string title, DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(title);
        _title = title;
        _createdAt = createdAt;
    }

    /// <summary>
    /// Die Zahl der bisher angelegten Seiten.
    /// </summary>
    public int PageCount
    {
        get { return _pages.Count; }
    }

    /// <summary>
    /// Legt eine neue Seite an; alle folgenden Zeichenbefehle gehen auf diese Seite.
    /// </summary>
    public void AddPage()
    {
        _pages.Add(new StringBuilder());
    }

    /// <summary>
    /// Legt fest, wie die Fußzeile aus Seitennummer und Seitenzahl gebildet wird; sie steht rechts unten auf jeder Seite.
    /// </summary>
    /// <param name="format">Bildet den Text aus Seitennummer und Gesamtzahl.</param>
    public void SetFooter(Func<int, int, string> format)
    {
        ArgumentNullException.ThrowIfNull(format);
        _footer = format;
    }

    /// <summary>
    /// Schreibt Text linksbündig.
    /// </summary>
    /// <param name="x">Der linke Rand in Punkt.</param>
    /// <param name="baseline">Die Grundlinie in Punkt von der Oberkante der Seite.</param>
    /// <param name="text">Der Text.</param>
    /// <param name="size">Die Schriftgröße in Punkt.</param>
    /// <param name="bold">Gibt an, ob die fette Schrift verwendet wird.</param>
    /// <param name="gray">Der Grauwert von 0 (schwarz) bis 1 (weiß).</param>
    public void DrawText(double x, double baseline, string text, double size, bool bold = false, double gray = 0)
    {
        AppendText(CurrentPage(), x, baseline, text, size, bold, gray);
    }

    /// <summary>
    /// Schreibt Text rechtsbündig.
    /// </summary>
    /// <param name="right">Der rechte Rand in Punkt.</param>
    /// <param name="baseline">Die Grundlinie in Punkt von der Oberkante der Seite.</param>
    /// <param name="text">Der Text.</param>
    /// <param name="size">Die Schriftgröße in Punkt.</param>
    /// <param name="bold">Gibt an, ob die fette Schrift verwendet wird.</param>
    /// <param name="gray">Der Grauwert von 0 (schwarz) bis 1 (weiß).</param>
    public void DrawTextRight(double right, double baseline, string text, double size, bool bold = false, double gray = 0)
    {
        DrawText(right - PdfFontMetrics.Measure(text, bold, size), baseline, text, size, bold, gray);
    }

    /// <summary>
    /// Zeichnet eine Linie.
    /// </summary>
    /// <param name="x1">Der Anfang, waagerecht.</param>
    /// <param name="y1">Der Anfang, senkrecht von der Oberkante.</param>
    /// <param name="x2">Das Ende, waagerecht.</param>
    /// <param name="y2">Das Ende, senkrecht von der Oberkante.</param>
    /// <param name="width">Die Strichstärke in Punkt.</param>
    /// <param name="gray">Der Grauwert von 0 (schwarz) bis 1 (weiß).</param>
    public void DrawLine(double x1, double y1, double x2, double y2, double width = 0.5, double gray = 0)
    {
        CurrentPage().Append(Number(gray)).Append(" G ")
            .Append(Number(width)).Append(" w ")
            .Append(Number(x1)).Append(' ').Append(Number(PageHeight - y1)).Append(" m ")
            .Append(Number(x2)).Append(' ').Append(Number(PageHeight - y2)).Append(" l S\n");
    }

    /// <summary>
    /// Erzeugt die Bytes des Dokuments. Ohne Seite entsteht eine leere Seite.
    /// </summary>
    /// <returns>Die PDF-Datei.</returns>
    public byte[] Build()
    {
        if (_pages.Count == 0)
        {
            AddPage();
        }

        var contents = new List<string>();
        for (var index = 0; index < _pages.Count; index++)
        {
            var content = new StringBuilder(_pages[index].ToString());
            if (_footer is not null)
            {
                AppendText(content, FooterRight - PdfFontMetrics.Measure(_footer(index + 1, _pages.Count), false, FooterSize), FooterBaseline, _footer(index + 1, _pages.Count), FooterSize, false, FooterGray);
            }

            contents.Add(content.ToString());
        }

        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            $"<< /Type /Pages /Kids [{string.Join(' ', Enumerable.Range(0, contents.Count).Select(index => $"{PageObjectNumber(index)} 0 R"))}] /Count {contents.Count} >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>",
            $"<< /Title {PdfTextEncoder.ToLiteral(_title)} /Creator (Tankatlas) /Producer (Tankatlas) /CreationDate (D:{_createdAt:yyyyMMddHHmmss}) >>",
        };
        for (var index = 0; index < contents.Count; index++)
        {
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Number(PageWidth)} {Number(PageHeight)}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {ContentObjectNumber(index)} 0 R >>");
            objects.Add($"<< /Length {contents[index].Length} >>\nstream\n{contents[index]}endstream");
        }

        var output = new StringBuilder("%PDF-1.4\n%âãÏÓ\n");
        var offsets = new List<int>();
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(output.Length);
            output.Append(index + 1).Append(" 0 obj\n").Append(objects[index]).Append("\nendobj\n");
        }

        var xrefOffset = output.Length;
        output.Append("xref\n0 ").Append(objects.Count + 1).Append('\n');
        output.Append("0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            output.Append(offset.ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        }

        output.Append("trailer\n<< /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R /Info 5 0 R >>\n");
        output.Append("startxref\n").Append(xrefOffset).Append("\n%%EOF\n");
        return Encoding.Latin1.GetBytes(output.ToString());
    }

    private static int PageObjectNumber(int pageIndex)
    {
        return 6 + (pageIndex * 2);
    }

    private static int ContentObjectNumber(int pageIndex)
    {
        return 7 + (pageIndex * 2);
    }

    private static void AppendText(StringBuilder page, double x, double baseline, string text, double size, bool bold, double gray)
    {
        page.Append("BT /").Append(bold ? "F2 " : "F1 ").Append(Number(size)).Append(" Tf ")
            .Append(Number(gray)).Append(" g ")
            .Append(Number(x)).Append(' ').Append(Number(PageHeight - baseline)).Append(" Td ")
            .Append(PdfTextEncoder.ToLiteral(text)).Append(" Tj ET\n");
    }

    private static string Number(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private StringBuilder CurrentPage()
    {
        if (_pages.Count == 0)
        {
            AddPage();
        }

        return _pages[^1];
    }
}

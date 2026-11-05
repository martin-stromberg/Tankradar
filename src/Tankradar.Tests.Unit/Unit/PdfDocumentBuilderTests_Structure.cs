using System.Text.RegularExpressions;
using Tankradar.MAUI.Services.Export;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Gültigkeit der Struktur der vom <see cref="PdfDocumentBuilder"/> erzeugten Datei: Kopf, Seitenbaum, Querverweise, Abschluss.
/// </summary>
public class PdfDocumentBuilderTests_Structure : BaseTest
{
    private static byte[] CreateDocument(int pages, Func<int, int, string>? footer = null)
    {
        var builder = new PdfDocumentBuilder("Titel (Test)", new DateTime(2026, 10, 19, 9, 30, 15));
        if (footer is not null)
        {
            builder.SetFooter(footer);
        }

        for (var page = 1; page <= pages; page++)
        {
            builder.AddPage();
            builder.DrawText(40, 60, $"Seite {page} Käse (1)", 12, bold: page == 1);
            builder.DrawLine(40, 70, 500, 70);
        }

        return builder.Build();
    }

    /// <summary>
    /// Prüft Kopfzeile und Dateiende.
    /// </summary>
    [Fact]
    public void Build_HasHeaderAndEndMarker()
    {
        var reader = new PdfTestReader(CreateDocument(1));

        Assert.StartsWith("%PDF-1.4\n", reader.Text, StringComparison.Ordinal);
        Assert.EndsWith("%%EOF\n", reader.Text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass jeder Eintrag der Querverweistabelle auf den Beginn des zugehörigen Objekts zeigt.
    /// </summary>
    [Fact]
    public void Build_XrefOffsetsPointToObjectStarts()
    {
        var reader = new PdfTestReader(CreateDocument(3));

        var offsets = reader.ReadXrefOffsets();

        Assert.Equal(reader.Objects.Count, offsets.Count);
        for (var number = 1; number <= offsets.Count; number++)
        {
            Assert.StartsWith($"{number} 0 obj\n", reader.Text[offsets[number - 1]..], StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Prüft, dass <c>startxref</c> auf die Tabelle „xref“ zeigt und die Größe im Trailer stimmt.
    /// </summary>
    [Fact]
    public void Build_StartxrefPointsToXrefTable()
    {
        var reader = new PdfTestReader(CreateDocument(2));

        Assert.StartsWith("xref\n", reader.Text[reader.ReadStartXref()..], StringComparison.Ordinal);
        Assert.Contains($"/Size {reader.Objects.Count + 1}", reader.Text, StringComparison.Ordinal);
        Assert.Contains("/Root 1 0 R", reader.Text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft den Seitenbaum: Seitenzahl, Verweise auf die Seiten und den Elternknoten.
    /// </summary>
    [Fact]
    public void Build_PageTreeListsAllPages()
    {
        var reader = new PdfTestReader(CreateDocument(3));

        Assert.Equal(3, reader.PageObjectNumbers.Count);
        Assert.Contains("/Count 3", reader.Objects[2], StringComparison.Ordinal);
        Assert.All(reader.PageObjectNumbers, number => Assert.Contains("/Parent 2 0 R", reader.Objects[number], StringComparison.Ordinal));
        Assert.All(reader.PageObjectNumbers, number => Assert.Contains("/MediaBox [0 0 595.28 841.89]", reader.Objects[number], StringComparison.Ordinal));
    }

    /// <summary>
    /// Prüft die angegebene Länge der Inhaltsströme.
    /// </summary>
    [Fact]
    public void Build_StreamLengthsMatchContent()
    {
        var reader = new PdfTestReader(CreateDocument(2));

        foreach (var number in reader.PageObjectNumbers.Select(page => page + 1))
        {
            var body = reader.Objects[number];
            var length = int.Parse(Regex.Match(body, @"/Length (\d+)").Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            var start = body.IndexOf("stream\n", StringComparison.Ordinal) + 7;
            var end = body.LastIndexOf("endstream", StringComparison.Ordinal);
            Assert.Equal(length, end - start);
        }
    }

    /// <summary>
    /// Prüft das Info-Dictionary: Titel mit maskierten Klammern, Erzeuger und Erstellungsdatum.
    /// </summary>
    [Fact]
    public void Build_InfoDictionaryHasTitleAndCreationDate()
    {
        var reader = new PdfTestReader(CreateDocument(1));

        Assert.Contains("/Title <FEFF0054006900740065006C0020002800540065007300740029>", reader.Objects[5], StringComparison.Ordinal);
        Assert.Contains("/CreationDate (D:20261019093015)", reader.Objects[5], StringComparison.Ordinal);
        Assert.Contains("/Producer (Tankatlas)", reader.Objects[5], StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft die Grundschriften mit WinAnsi-Kodierung.
    /// </summary>
    [Fact]
    public void Build_UsesStandardFontsWithWinAnsiEncoding()
    {
        var reader = new PdfTestReader(CreateDocument(1));

        Assert.Contains("/BaseFont /Helvetica /Encoding /WinAnsiEncoding", reader.Objects[3], StringComparison.Ordinal);
        Assert.Contains("/BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding", reader.Objects[4], StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft die Textstellen je Seite einschließlich Umlauten und Klammern.
    /// </summary>
    [Fact]
    public void Build_TextCanBeReadBack()
    {
        var reader = new PdfTestReader(CreateDocument(2));

        Assert.Equal(["Seite 1 Käse (1)"], reader.Pages[0]);
        Assert.Equal(["Seite 2 Käse (1)"], reader.Pages[1]);
    }

    /// <summary>
    /// Prüft die Fußzeile „Seite x von y“ auf jeder Seite.
    /// </summary>
    [Fact]
    public void Build_FooterShowsPageNumberAndTotal()
    {
        var reader = new PdfTestReader(CreateDocument(3, (page, total) => $"Seite {page} von {total}"));

        Assert.Equal("Seite 1 von 3", reader.Pages[0][^1]);
        Assert.Equal("Seite 2 von 3", reader.Pages[1][^1]);
        Assert.Equal("Seite 3 von 3", reader.Pages[2][^1]);
    }

    /// <summary>
    /// Prüft, dass ein Dokument ohne Seite eine leere Seite erhält.
    /// </summary>
    [Fact]
    public void Build_WithoutPages_CreatesOneEmptyPage()
    {
        var reader = new PdfTestReader(new PdfDocumentBuilder("Leer", DateTime.Now).Build());

        Assert.Single(reader.PageObjectNumbers);
        Assert.Empty(reader.Pages[0]);
    }

    /// <summary>
    /// Prüft, dass die ganze Datei, abgesehen von der Binärmarkierung im Kopf, aus ASCII besteht.
    /// </summary>
    [Fact]
    public void Build_BodyIsAscii()
    {
        var bytes = CreateDocument(2);

        Assert.All(bytes.Skip(15), value => Assert.True(value < 128));
    }
}

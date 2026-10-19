using Tankradar.MAUI.Services.Export;
using Tankradar.Tests.Unit.Unit.Support;
using static Tankradar.Tests.Unit.Unit.Support.TankbookExportTestData;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Seitenumbruch des <see cref="TankbookPdfReportWriter"/>: Verteilung auf mehrere Seiten, wiederholte Tabellenkopfzeile und Seitenzahlen.
/// </summary>
public class TankbookPdfReportWriterTests_Pagination : BaseTest
{
    private static PdfTestReader WriteReport(int entryCount)
    {
        return new PdfTestReader(new TankbookPdfReportWriter().Write(Build(CreateSeries(entryCount))));
    }

    /// <summary>
    /// Prüft, dass wenige Vorgänge auf eine Seite passen und die Fußzeile „Seite 1 von 1“ trägt.
    /// </summary>
    [Fact]
    public void Write_FewEntries_FitOnOnePage()
    {
        var reader = WriteReport(5);

        Assert.Single(reader.Pages);
        Assert.Equal("Seite 1 von 1", reader.Pages[0][^1]);
    }

    /// <summary>
    /// Prüft, dass 200 Vorgänge auf mehrere Seiten verteilt werden und jede Seite ihre Nummer „x von y“ trägt.
    /// </summary>
    [Fact]
    public void Write_200Entries_SpanMultiplePagesWithPageNumbers()
    {
        var reader = WriteReport(200);

        Assert.True(reader.Pages.Count >= 4);
        for (var index = 0; index < reader.Pages.Count; index++)
        {
            Assert.Equal($"Seite {index + 1} von {reader.Pages.Count}", reader.Pages[index][^1]);
        }
    }

    /// <summary>
    /// Prüft, dass alle Vorgänge genau einmal im Bericht stehen.
    /// </summary>
    [Fact]
    public void Write_200Entries_EveryEntryAppearsOnce()
    {
        var reader = WriteReport(200);

        var stations = reader.AllText.Where(text => text.StartsWith("Station ", StringComparison.Ordinal)).ToList();
        Assert.Equal(200, stations.Count);
        Assert.Equal(200, stations.Distinct().Count());
    }

    /// <summary>
    /// Prüft, dass die Tabellenkopfzeile auf jeder Seite mit Vorgängen wiederholt wird.
    /// </summary>
    [Fact]
    public void Write_200Entries_RepeatsTableHeaderOnFollowingPages()
    {
        var reader = WriteReport(200);

        foreach (var page in reader.Pages.Skip(1))
        {
            Assert.Contains("Datum", page);
            Assert.Contains("Tankstelle", page);
            Assert.Contains("Verbrauch", page);
        }
    }

    /// <summary>
    /// Prüft, dass kein Text unterhalb des Fußbereichs oder außerhalb der Seite liegt.
    /// </summary>
    [Fact]
    public void Write_200Entries_KeepsTextInsidePage()
    {
        var reader = WriteReport(200);

        foreach (var stream in reader.PageStreams)
        {
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(stream, @" (?<x>[\d.]+) (?<y>[\d.]+) Td "))
            {
                var x = double.Parse(match.Groups["x"].Value, System.Globalization.CultureInfo.InvariantCulture);
                var y = double.Parse(match.Groups["y"].Value, System.Globalization.CultureInfo.InvariantCulture);
                Assert.InRange(x, 0, PdfDocumentBuilder.PageWidth);
                Assert.InRange(y, 20, PdfDocumentBuilder.PageHeight);
            }
        }
    }
}

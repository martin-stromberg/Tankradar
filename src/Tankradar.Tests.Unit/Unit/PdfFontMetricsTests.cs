using Tankradar.MAUI.Services.Export;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Zeichenbreiten, Kürzen und Umbruch der <see cref="PdfFontMetrics"/> an Stichproben der Helvetica-Metriken.
/// </summary>
public class PdfFontMetricsTests : BaseTest
{
    /// <summary>
    /// Prüft Stichproben der Breiten der normalen Schrift.
    /// </summary>
    [Fact]
    public void GetWidth_Regular_MatchesHelveticaSamples()
    {
        Assert.Equal(278, PdfFontMetrics.GetWidth((byte)' ', bold: false));
        Assert.Equal(556, PdfFontMetrics.GetWidth((byte)'0', bold: false));
        Assert.Equal(667, PdfFontMetrics.GetWidth((byte)'A', bold: false));
        Assert.Equal(833, PdfFontMetrics.GetWidth((byte)'M', bold: false));
        Assert.Equal(222, PdfFontMetrics.GetWidth((byte)'i', bold: false));
        Assert.Equal(1015, PdfFontMetrics.GetWidth((byte)'@', bold: false));
    }

    /// <summary>
    /// Prüft Stichproben der Breiten der fetten Schrift.
    /// </summary>
    [Fact]
    public void GetWidth_Bold_MatchesHelveticaBoldSamples()
    {
        Assert.Equal(722, PdfFontMetrics.GetWidth((byte)'A', bold: true));
        Assert.Equal(611, PdfFontMetrics.GetWidth((byte)'b', bold: true));
        Assert.Equal(278, PdfFontMetrics.GetWidth((byte)'l', bold: true));
    }

    /// <summary>
    /// Prüft, dass Akzentbuchstaben die Breite des Grundbuchstabens haben und Sonderzeichen eigene Werte.
    /// </summary>
    [Fact]
    public void GetWidth_ExtendedCharacters_UseBaseLetterOrOwnValue()
    {
        Assert.Equal(PdfFontMetrics.GetWidth((byte)'a', bold: false), PdfFontMetrics.GetWidth(0xE4, bold: false));
        Assert.Equal(PdfFontMetrics.GetWidth((byte)'U', bold: false), PdfFontMetrics.GetWidth(0xDC, bold: false));
        Assert.Equal(611, PdfFontMetrics.GetWidth(0xDF, bold: false));
        Assert.Equal(556, PdfFontMetrics.GetWidth(0x80, bold: false));
        Assert.Equal(1000, PdfFontMetrics.GetWidth(0x85, bold: false));
    }

    /// <summary>
    /// Prüft die Textbreite in Punkt: „Hello“ in Helvetica 10 Punkt ist 22,78 Punkt breit.
    /// </summary>
    [Fact]
    public void Measure_Text_SumsWidthsScaledBySize()
    {
        Assert.Equal(22.78, PdfFontMetrics.Measure("Hello", bold: false, 10), 2);
    }

    /// <summary>
    /// Prüft, dass ein passender Text unverändert bleibt und ein zu langer mit Auslassungszeichen in die Breite passt.
    /// </summary>
    [Fact]
    public void Truncate_LongText_FitsWidthWithEllipsis()
    {
        Assert.Equal("Hello", PdfFontMetrics.Truncate("Hello", bold: false, 10, 100));

        var shortened = PdfFontMetrics.Truncate("Alpha Tankstelle Nord-Ost", bold: false, 10, 60);

        Assert.EndsWith("…", shortened, StringComparison.Ordinal);
        Assert.True(PdfFontMetrics.Measure(shortened, bold: false, 10) <= 60);
        Assert.StartsWith("Alpha", shortened, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass ein Text, bei dem nicht einmal das Auslassungszeichen passt, leer wird.
    /// </summary>
    [Fact]
    public void Truncate_NothingFits_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, PdfFontMetrics.Truncate("Hello", bold: false, 10, 1));
    }

    /// <summary>
    /// Prüft das Kürzen eines Textes aus Emoji (Surrogatpaare): Es bleibt kein halbes Paar, das Ergebnis passt in die Breite.
    /// </summary>
    [Fact]
    public void Truncate_SurrogatePairs_NeverSplitsPair()
    {
        var text = string.Concat(Enumerable.Repeat("\U0001F600", 10));

        var shortened = PdfFontMetrics.Truncate(text, bold: false, 10, 30);

        Assert.EndsWith("…", shortened, StringComparison.Ordinal);
        Assert.True(PdfFontMetrics.Measure(shortened, bold: false, 10) <= 30);
        Assert.DoesNotContain(true, shortened.Where((c, i) => char.IsHighSurrogate(c) && (i + 1 >= shortened.Length || !char.IsLowSurrogate(shortened[i + 1]))).Select(_ => true));
        Assert.True(shortened.Length > 1);
    }

    /// <summary>
    /// Prüft den Umbruch an Leerzeichen innerhalb der Breite.
    /// </summary>
    [Fact]
    public void Wrap_LongText_BreaksAtSpacesWithinWidth()
    {
        var lines = PdfFontMetrics.Wrap("Dies ist ein langer Satz der umbrochen werden muss", bold: false, 10, 100);

        Assert.True(lines.Count > 1);
        Assert.All(lines, line => Assert.True(PdfFontMetrics.Measure(line, bold: false, 10) <= 100));
        Assert.Equal("Dies ist ein langer Satz der umbrochen werden muss", string.Join(' ', lines));
    }

    /// <summary>
    /// Prüft, dass ein einzelnes zu langes Wort gekürzt wird und ein leerer Text eine leere Zeile ergibt.
    /// </summary>
    [Fact]
    public void Wrap_VeryLongWordAndEmptyText_AreHandled()
    {
        var lines = PdfFontMetrics.Wrap(new string('W', 100), bold: false, 10, 50);

        Assert.Single(lines);
        Assert.True(PdfFontMetrics.Measure(lines[0], bold: false, 10) <= 50);
        Assert.Equal([string.Empty], PdfFontMetrics.Wrap(string.Empty, bold: false, 10, 50));
    }
}

using Tankradar.MAUI.Services.Export;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Kodierung des <see cref="PdfTextEncoder"/> für die PDF-Grundschriften (WinAnsi).
/// </summary>
public class PdfTextEncoderTests : BaseTest
{
    /// <summary>
    /// Prüft, dass Umlaute, ß und das Eurozeichen auf die WinAnsi-Codes abgebildet werden.
    /// </summary>
    [Fact]
    public void ToWinAnsi_GermanCharacters_MapToWinAnsiCodes()
    {
        var bytes = PdfTextEncoder.ToWinAnsi("äöüÄÖÜß€");

        Assert.Equal([0xE4, 0xF6, 0xFC, 0xC4, 0xD6, 0xDC, 0xDF, 0x80], bytes);
    }

    /// <summary>
    /// Prüft die Ersetzung nicht abbildbarer Zeichen durch ein Fragezeichen, je Zeichen genau eines, auch bei Zeichenpaaren.
    /// </summary>
    [Fact]
    public void ToWinAnsi_UnmappableCharacters_BecomeQuestionMark()
    {
        Assert.Equal("a?b?c"u8.ToArray(), PdfTextEncoder.ToWinAnsi("aЖb😀c"));
    }

    /// <summary>
    /// Prüft, dass Steuerzeichen zu Leerzeichen werden.
    /// </summary>
    [Fact]
    public void ToWinAnsi_ControlCharacters_BecomeSpaces()
    {
        Assert.Equal("a b c"u8.ToArray(), PdfTextEncoder.ToWinAnsi("a\tb\nc"));
    }

    /// <summary>
    /// Prüft die Maskierung von Klammern und Rückstrich und die Escape-Folge für Zeichen ab 128.
    /// </summary>
    [Fact]
    public void ToLiteral_EscapesParenthesesBackslashAndHighCharacters()
    {
        Assert.Equal("(a\\(b\\)c\\\\d)", PdfTextEncoder.ToLiteral("a(b)c\\d"));
        Assert.Equal("(K\\344se \\200)", PdfTextEncoder.ToLiteral("Käse €"));
    }

    /// <summary>
    /// Prüft, dass das Ergebnis nur aus ASCII-Zeichen besteht.
    /// </summary>
    [Fact]
    public void ToLiteral_ContainsOnlyAscii()
    {
        Assert.All(PdfTextEncoder.ToLiteral("Größe – 5 € • … ž"), character => Assert.True(character < 128));
    }
}

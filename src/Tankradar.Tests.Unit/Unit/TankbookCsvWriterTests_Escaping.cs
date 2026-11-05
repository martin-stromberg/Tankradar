using System.Text;
using Tankradar.MAUI.Services.Export;
using static Tankradar.Tests.Unit.Unit.Support.TankbookExportTestData;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Maskierung und Formelschutz des <see cref="TankbookCsvWriter"/> (ADR 0013).
/// </summary>
public class TankbookCsvWriterTests_Escaping : BaseTest
{
    private static string WriteSingle(string station, string? note = null)
    {
        var data = Build([CreateEntry(1, new DateTime(2026, 3, 5, 7, 0, 0), 10_500, station: station, note: note)]);
        return Encoding.UTF8.GetString(TankbookCsvWriter.Write(data));
    }

    /// <summary>
    /// Prüft, dass ein Feld mit Semikolon in Anführungszeichen steht.
    /// </summary>
    [Fact]
    public void Write_SemicolonInField_IsQuoted()
    {
        Assert.Contains(";\"Alpha; Nord\";", WriteSingle("Alpha; Nord"), StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass Anführungszeichen verdoppelt werden.
    /// </summary>
    [Fact]
    public void Write_QuoteInField_IsDoubled()
    {
        Assert.Contains(";\"Die \"\"Beste\"\"\";", WriteSingle("Die \"Beste\""), StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass ein Zeilenumbruch in der Notiz das Feld in Anführungszeichen setzt und die Zeile nicht zerbricht.
    /// </summary>
    [Fact]
    public void Write_LineBreakInNote_IsQuoted()
    {
        var text = WriteSingle("Alpha", "Zeile 1\nZeile 2");

        Assert.EndsWith(";\"Zeile 1\nZeile 2\"\r\n", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft den Formelschutz: Textfelder mit führendem Formelzeichen erhalten ein Hochkomma.
    /// </summary>
    /// <param name="value">Der Text.</param>
    /// <param name="expected">Der erwartete Feldinhalt.</param>
    [Theory]
    [InlineData("=SUMME(A1)", "'=SUMME(A1)")]
    [InlineData("+49 123", "'+49 123")]
    [InlineData("-Tankstelle", "'-Tankstelle")]
    [InlineData("@Befehl", "'@Befehl")]
    [InlineData("\tTab", "'\tTab")]
    public void Write_FormulaCharacters_ArePrefixed(string value, string expected)
    {
        var text = WriteSingle(value, value);

        Assert.Contains(";" + expected + ";", text, StringComparison.Ordinal);
        Assert.EndsWith(";" + expected + "\r\n", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass auch Formelzeichen nach führendem Leerraum geschützt werden, normaler Text mit Leerraum aber unverändert bleibt.
    /// </summary>
    [Fact]
    public void Write_FormulaAfterLeadingWhitespace_IsPrefixed()
    {
        Assert.Contains(";' =1+1;", WriteSingle(" =1+1"), StringComparison.Ordinal);
        Assert.Contains(";'  @Befehl;", WriteSingle("  @Befehl"), StringComparison.Ordinal);
        Assert.Contains(";  Alpha;", WriteSingle("  Alpha"), StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass Zahlen, auch negative Werte, nicht mit einem Hochkomma versehen werden.
    /// </summary>
    [Fact]
    public void Write_Numbers_AreNotPrefixed()
    {
        var data = Build([CreateEntry(1, new DateTime(2026, 3, 5, 7, 0, 0), 10_500, 40m, -5m)]);

        var text = Encoding.UTF8.GetString(TankbookCsvWriter.Write(data));

        Assert.DoesNotContain("'", text, StringComparison.Ordinal);
        Assert.Contains(";-5,00;", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass ein Text mit Formelzeichen nicht in der Mitte verändert wird.
    /// </summary>
    [Fact]
    public void Write_FormulaCharacterInMiddle_IsUnchanged()
    {
        Assert.Contains(";Alpha=Beta;", WriteSingle("Alpha=Beta"), StringComparison.Ordinal);
    }
}

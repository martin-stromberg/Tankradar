using Tankradar.MAUI.Services.Export;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Bildung des Dateinamens durch den <see cref="TankbookExportFileNamer"/>.
/// </summary>
public class TankbookExportFileNamerTests : BaseTest
{
    private static readonly DateTime Date = new(2026, 10, 19);

    /// <summary>
    /// Prüft das Schema <c>Tankbuch_Fahrzeug_Datum.Endung</c>.
    /// </summary>
    [Fact]
    public void Create_SimpleName_UsesSchema()
    {
        Assert.Equal("Tankbuch_Golf_2026-10-19.csv", TankbookExportFileNamer.Create("Golf", Date, "csv"));
    }

    /// <summary>
    /// Prüft, dass Leerzeichen und ungültige Zeichen durch einen Unterstrich ersetzt werden.
    /// </summary>
    [Fact]
    public void Create_SpecialCharacters_AreReplaced()
    {
        Assert.Equal("Tankbuch_Mein_Auto_A_B_2026-10-19.pdf", TankbookExportFileNamer.Create("Mein Auto: A/B", Date, "pdf"));
    }

    /// <summary>
    /// Prüft, dass alle unter Windows und iOS-Freigaben ungültigen Zeichen ersetzt werden, unabhängig von der Plattform.
    /// </summary>
    [Fact]
    public void Create_AllReservedCharacters_AreReplaced()
    {
        Assert.Equal("Tankbuch_a_b_c_d_e_f_g_h_i_2026-10-19.csv", TankbookExportFileNamer.Create("a:b\"c/d\\e*f?g<h>i|", Date, "csv"));
    }

    /// <summary>
    /// Prüft, dass Umlaute erhalten bleiben.
    /// </summary>
    [Fact]
    public void Create_Umlauts_AreKept()
    {
        Assert.Equal("Tankbuch_Käfer_2026-10-19.csv", TankbookExportFileNamer.Create("Käfer", Date, "csv"));
    }

    /// <summary>
    /// Prüft, dass lange Namen auf 40 Zeichen gekürzt werden.
    /// </summary>
    [Fact]
    public void Create_LongName_IsLimitedTo40Characters()
    {
        var name = TankbookExportFileNamer.Create(new string('a', 100), Date, "csv");

        Assert.Equal("Tankbuch_" + new string('a', 40) + "_2026-10-19.csv", name);
    }

    /// <summary>
    /// Prüft, dass ein leerer oder nur aus Sonderzeichen bestehender Name durch „Fahrzeug“ ersetzt wird.
    /// </summary>
    /// <param name="vehicleName">Der Name des Fahrzeugs.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("///")]
    public void Create_EmptyName_UsesFallback(string vehicleName)
    {
        Assert.Equal("Tankbuch_Fahrzeug_2026-10-19.csv", TankbookExportFileNamer.Create(vehicleName, Date, "csv"));
    }
}

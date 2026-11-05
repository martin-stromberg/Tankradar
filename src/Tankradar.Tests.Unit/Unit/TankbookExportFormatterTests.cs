using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Export;
using static Tankradar.Tests.Unit.Unit.Support.TankbookExportTestData;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Formate des <see cref="TankbookExportFormatter"/>.
/// </summary>
public class TankbookExportFormatterTests : BaseTest
{
    /// <summary>
    /// Prüft Zahlen mit Dezimalkomma und ohne Tausendertrenner.
    /// </summary>
    [Fact]
    public void Numbers_UseDecimalCommaWithoutThousandsSeparator()
    {
        Assert.Equal("1234,50", TankbookExportFormatter.FormatDecimal2(1234.5m));
        Assert.Equal("40,00", TankbookExportFormatter.FormatNumber(40m));
        Assert.Equal("1,859", TankbookExportFormatter.FormatNumber(1.859m));
        Assert.Equal("123456", TankbookExportFormatter.FormatInteger(123_456));
    }

    /// <summary>
    /// Prüft Datum und Uhrzeit.
    /// </summary>
    [Fact]
    public void DateAndTime_UseGermanFormat()
    {
        var value = new DateTime(2026, 3, 5, 7, 9, 0);

        Assert.Equal("05.03.2026", TankbookExportFormatter.FormatDate(value));
        Assert.Equal("07:09", TankbookExportFormatter.FormatTime(value));
    }

    /// <summary>
    /// Prüft die Einheitennamen und Ja/Nein.
    /// </summary>
    [Fact]
    public void QuantityUnitName_IsLiterOrKwh()
    {
        Assert.Equal("Liter", TankbookExportFormatter.GetQuantityUnitName(TankbookEntryKind.Fuel));
        Assert.Equal("kWh", TankbookExportFormatter.GetQuantityUnitName(TankbookEntryKind.Electricity));
        Assert.Equal("Ja", TankbookExportFormatter.FormatYesNo(true));
        Assert.Equal("Nein", TankbookExportFormatter.FormatYesNo(false));
    }

    /// <summary>
    /// Prüft die Textform des Zeitraums.
    /// </summary>
    [Fact]
    public void FormatRange_CoversAllCombinations()
    {
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 12, 31);

        Assert.Equal("Alle Vorgänge", TankbookExportFormatter.FormatRange(null, null));
        Assert.Equal("ab 01.01.2026", TankbookExportFormatter.FormatRange(from, null));
        Assert.Equal("bis 31.12.2026", TankbookExportFormatter.FormatRange(null, to));
        Assert.Equal("01.01.2026 bis 31.12.2026", TankbookExportFormatter.FormatRange(from, to));
    }

    /// <summary>
    /// Prüft den Hinweis „ab Startstand“ nur bei berechnetem Verbrauch aus dem Startintervall.
    /// </summary>
    [Fact]
    public void GetConsumptionNote_OnlyForFromStartIntervals()
    {
        var data = Build([CreateEntry(1, new DateTime(2026, 1, 5, 8, 0, 0), 10_500), CreateEntry(2, new DateTime(2026, 1, 20, 8, 0, 0), 11_000)]);

        Assert.Equal("ab Startstand", TankbookExportFormatter.GetConsumptionNote(data.Entries[0]));
        Assert.Equal(string.Empty, TankbookExportFormatter.GetConsumptionNote(data.Entries[1]));
    }
}

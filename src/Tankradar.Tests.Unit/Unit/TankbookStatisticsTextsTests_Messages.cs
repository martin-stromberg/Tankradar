using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Hinweise und Kennzeichnungen von <see cref="TankbookStatisticsTexts"/>.
/// </summary>
public class TankbookStatisticsTextsTests_Messages : BaseTest
{
    /// <summary>
    /// Prüft die Kennzeichnung der Herkunft des Durchschnitts für alle Werte.
    /// </summary>
    [Fact]
    public void SourceText_CoversAllValues()
    {
        Assert.Equal(string.Empty, TankbookStatisticsTexts.GetSourceText(TankbookAverageSource.None));
        Assert.Equal("Berechnet aus vollen Vorgängen", TankbookStatisticsTexts.GetSourceText(TankbookAverageSource.Calculated));
        Assert.Equal("Startverbrauch – noch keine zwei vollen Vorgänge", TankbookStatisticsTexts.GetSourceText(TankbookAverageSource.StartConsumption));
    }

    /// <summary>
    /// Prüft die festen Texte des Bereichs.
    /// </summary>
    [Fact]
    public void Notices_Wording()
    {
        Assert.Equal("Auswertung", TankbookStatisticsTexts.Heading);
        Assert.Contains("mindestens zwei volle Vorgänge", TankbookStatisticsTexts.TooFewEntriesNotice, StringComparison.Ordinal);
        Assert.Contains("Startverbrauch", TankbookStatisticsTexts.TooFewEntriesNotice, StringComparison.Ordinal);
        Assert.Equal("Die Auswertung konnte nicht geladen werden.", TankbookStatisticsTexts.LoadFailed);
        Assert.NotEmpty(TankbookStatisticsTexts.NoEntriesNotice);
    }
}

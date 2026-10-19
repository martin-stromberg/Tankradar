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
        Assert.Equal("Ab Startstand – angenommen wird ein voller Tank beim Start", TankbookStatisticsTexts.GetSourceText(TankbookAverageSource.FromStart));
        Assert.Equal("Startverbrauch – noch keine Strecke aus vollen Vorgängen", TankbookStatisticsTexts.GetSourceText(TankbookAverageSource.StartConsumption));
    }

    /// <summary>
    /// Prüft die festen Texte des Bereichs.
    /// </summary>
    [Fact]
    public void Notices_Wording()
    {
        Assert.Equal("Auswertung", TankbookStatisticsTexts.Heading);
        Assert.Contains("mindestens zwei volle Vorgänge", TankbookStatisticsTexts.TooFewEntriesNotice, StringComparison.Ordinal);
        Assert.DoesNotContain("Startverbrauch", TankbookStatisticsTexts.TooFewEntriesNotice, StringComparison.Ordinal);
        Assert.Contains("Startverbrauch", TankbookStatisticsTexts.GetTooFewEntriesNotice(true), StringComparison.Ordinal);
        Assert.StartsWith(TankbookStatisticsTexts.TooFewEntriesNotice, TankbookStatisticsTexts.GetTooFewEntriesNotice(true), StringComparison.Ordinal);
        Assert.Equal(TankbookStatisticsTexts.TooFewEntriesNotice, TankbookStatisticsTexts.GetTooFewEntriesNotice(false));
        Assert.Equal("Die Auswertung konnte nicht geladen werden.", TankbookStatisticsTexts.LoadFailed);
        Assert.NotEmpty(TankbookStatisticsTexts.NoEntriesNotice);
    }
}

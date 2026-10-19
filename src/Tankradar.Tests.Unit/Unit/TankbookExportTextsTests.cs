using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Texte aus <see cref="TankbookExportTexts"/> für alle Werte von <see cref="TankbookExportFormat"/>, <see cref="TankbookExportRangeKind"/> und <see cref="TankbookExportStatus"/>.
/// </summary>
public class TankbookExportTextsTests : BaseTest
{
    /// <summary>
    /// Prüft Beschriftung und Endung der Formate.
    /// </summary>
    [Fact]
    public void Formats_HaveLabelAndExtension()
    {
        Assert.Equal("CSV", TankbookExportTexts.GetFormatLabel(TankbookExportFormat.Csv));
        Assert.Equal("PDF", TankbookExportTexts.GetFormatLabel(TankbookExportFormat.Pdf));
        Assert.Equal("csv", TankbookExportTexts.GetFileExtension(TankbookExportFormat.Csv));
        Assert.Equal("pdf", TankbookExportTexts.GetFileExtension(TankbookExportFormat.Pdf));
        Assert.Throws<ArgumentOutOfRangeException>(() => TankbookExportTexts.GetFormatLabel((TankbookExportFormat)9));
        Assert.Throws<ArgumentOutOfRangeException>(() => TankbookExportTexts.GetFileExtension((TankbookExportFormat)9));
    }

    /// <summary>
    /// Prüft die Beschriftung aller Zeitraumarten.
    /// </summary>
    [Fact]
    public void RangeKinds_HaveLabels()
    {
        Assert.Equal("Alle Vorgänge", TankbookExportTexts.GetRangeLabel(TankbookExportRangeKind.All));
        Assert.Equal("Dieses Jahr", TankbookExportTexts.GetRangeLabel(TankbookExportRangeKind.ThisYear));
        Assert.Equal("Letztes Jahr", TankbookExportTexts.GetRangeLabel(TankbookExportRangeKind.LastYear));
        Assert.Equal("Letzte 12 Monate", TankbookExportTexts.GetRangeLabel(TankbookExportRangeKind.Last12Months));
        Assert.Equal("Eigener Zeitraum", TankbookExportTexts.GetRangeLabel(TankbookExportRangeKind.Custom));
        Assert.Throws<ArgumentOutOfRangeException>(() => TankbookExportTexts.GetRangeLabel((TankbookExportRangeKind)9));
    }

    /// <summary>
    /// Prüft die Meldung zu jedem Ergebnis.
    /// </summary>
    [Fact]
    public void Statuses_HaveMessages()
    {
        Assert.Equal(string.Empty, TankbookExportTexts.GetStatusMessage(TankbookExportStatus.Exported, true));
        Assert.Equal("Für dieses Fahrzeug gibt es noch keine Vorgänge zum Exportieren.", TankbookExportTexts.GetStatusMessage(TankbookExportStatus.NoEntries, true));
        Assert.Equal("Im gewählten Zeitraum gibt es keine Vorgänge.", TankbookExportTexts.GetStatusMessage(TankbookExportStatus.NoEntries, false));
        Assert.Equal("Das Fahrzeug wurde nicht gefunden.", TankbookExportTexts.GetStatusMessage(TankbookExportStatus.VehicleNotFound, false));
        Assert.Equal("Der Zeitraum ist ungültig.", TankbookExportTexts.GetStatusMessage(TankbookExportStatus.InvalidRange, false));
        Assert.Equal("Der Export konnte nicht erstellt werden.", TankbookExportTexts.GetStatusMessage(TankbookExportStatus.Failed, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => TankbookExportTexts.GetStatusMessage((TankbookExportStatus)9, false));
    }

    /// <summary>
    /// Prüft die Meldung zu jedem Fehler des Zeitraums.
    /// </summary>
    [Fact]
    public void RangeErrors_HaveMessages()
    {
        Assert.Equal(string.Empty, TankbookExportTexts.GetRangeErrorMessage(TankbookExportRangeError.None));
        Assert.Equal("Das Startdatum liegt nach dem Enddatum.", TankbookExportTexts.GetRangeErrorMessage(TankbookExportRangeError.StartAfterEnd));
        Assert.Equal("Das Enddatum liegt in der Zukunft.", TankbookExportTexts.GetRangeErrorMessage(TankbookExportRangeError.EndInFuture));
        Assert.Throws<ArgumentOutOfRangeException>(() => TankbookExportTexts.GetRangeErrorMessage((TankbookExportRangeError)9));
    }

    /// <summary>
    /// Prüft Bereitmeldung, Fußzeile und Hinweis zur Weitergabe.
    /// </summary>
    [Fact]
    public void Messages_AreFormatted()
    {
        Assert.Equal("Export bereit: Tankbuch_Golf_2026-10-19.csv.", TankbookExportTexts.FormatReady("Tankbuch_Golf_2026-10-19.csv"));
        Assert.Equal("Seite 2 von 5", TankbookExportTexts.FormatPageFooter(2, 5));
        Assert.Contains("unverschlüsselt", TankbookExportTexts.PrivacyNotice, StringComparison.Ordinal);
        Assert.Contains("Belegfotos", TankbookExportTexts.PrivacyNotice, StringComparison.Ordinal);
        Assert.Equal("Exportieren", TankbookExportTexts.StartButton);
    }
}

using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Formatierung von Datum, Größe, Zählwerten und die Rückfragetexte der Sicherung.
/// </summary>
public class BackupTextsTests_Format : BaseTest
{
    private static readonly DateTime Created = new(2026, 10, 12, 11, 15, 0, DateTimeKind.Utc);

    private static string Local()
    {
        return Created.ToLocalTime().ToString("dd.MM.yyyy HH:mm", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
    }

    /// <summary>
    /// Prüft das Datum in Ortszeit.
    /// </summary>
    [Fact]
    public void FormatDate_UsesLocalTime()
    {
        Assert.Equal(Local(), BackupTexts.FormatDate(Created));
        Assert.Matches(@"^\d{2}\.\d{2}\.\d{4} \d{2}:\d{2}$", BackupTexts.FormatDate(Created));
    }

    /// <summary>
    /// Prüft die Größenangaben in Byte, KB und MB mit deutschem Dezimalzeichen.
    /// </summary>
    /// <param name="bytes">Die Größe in Bytes.</param>
    /// <param name="expected">Der erwartete Text.</param>
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1,5 KB")]
    [InlineData(1_258_291, "1,2 MB")]
    [InlineData(5_242_880, "5 MB")]
    [InlineData(3_221_225_472, "3 GB")]
    public void FormatSize_GermanFormat(long bytes, string expected)
    {
        Assert.Equal(expected, BackupTexts.FormatSize(bytes));
    }

    /// <summary>
    /// Prüft die Zählwerte mit Ein- und Mehrzahl.
    /// </summary>
    [Fact]
    public void FormatCounts_SingularAndPlural()
    {
        var counts = new BackupManifestCounts { Vehicles = 1, Entries = 14, Groups = 1, Favorites = 0, Stations = 40, Prices = 1, Photos = 2 };

        Assert.Equal("1 Fahrzeug, 14 Tank- und Ladevorgänge, 1 Favoritengruppe, 0 Favoriten, 40 Tankstellen, 1 Preisstand und 2 Belegfotos", BackupTexts.FormatCounts(counts));
        Assert.Equal("1 Fahrzeug, 1 Tank- oder Ladevorgang, 0 Favoritengruppen, 0 Favoriten, 0 Tankstellen, 0 Preisstände und 1 Belegfoto",
            BackupTexts.FormatCounts(new BackupManifestCounts { Vehicles = 1, Entries = 1, Photos = 1 }));
    }

    /// <summary>
    /// Prüft die Rückfrage zur Wiederherstellung: Datum, Version, Zählwerte, Warnung und Hinweis auf eine vorherige Sicherung.
    /// </summary>
    [Fact]
    public void FormatRestoreQuestion_ContainsAllParts()
    {
        var text = BackupTexts.FormatRestoreQuestion(Created, "0.1.19", new BackupManifestCounts { Vehicles = 2 });

        Assert.StartsWith($"Sicherung vom {Local()}, Version 0.1.19 wiederherstellen?", text, StringComparison.Ordinal);
        Assert.Contains("Sie enthält 2 Fahrzeuge,", text, StringComparison.Ordinal);
        Assert.Contains("Die aktuellen Daten werden vollständig ersetzt. Das lässt sich nicht rückgängig machen.", text, StringComparison.Ordinal);
        Assert.EndsWith("Erstelle vorher eine Sicherung, wenn du den aktuellen Stand behalten möchtest.", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft die Rückfrage ohne Version und ohne Zählwerte.
    /// </summary>
    [Fact]
    public void FormatRestoreQuestion_WithoutVersionAndCounts()
    {
        var text = BackupTexts.FormatRestoreQuestion(Created, null, null);

        Assert.StartsWith($"Sicherung vom {Local()} wiederherstellen? Die aktuellen Daten", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Sie enthält", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft Rückfrage zum Löschen, Titel, Kennzeichnung nicht lesbarer Dateien und die Hinweise für die Sprachausgabe.
    /// </summary>
    [Fact]
    public void DeleteQuestionTitleAndHints()
    {
        Assert.Equal($"Sicherung vom {Local()} löschen? Die Sicherungsdatei wird endgültig gelöscht. Deine aktuellen Daten bleiben unverändert.", BackupTexts.FormatDeleteQuestion(Created));
        Assert.Equal(Local(), BackupTexts.FormatItemTitle(new BackupFileInfo("a.zip", Created, 1, "1", true)));
        Assert.Equal($"{Local()} (nicht lesbar)", BackupTexts.FormatItemTitle(new BackupFileInfo("a.zip", Created, 1, string.Empty, false)));
        Assert.Equal($"Sicherung vom {Local()} wiederherstellen", BackupTexts.FormatRestoreHint(Created));
        Assert.Equal($"Sicherung vom {Local()} teilen oder speichern", BackupTexts.FormatShareHint(Created));
        Assert.Equal($"Sicherung vom {Local()} löschen", BackupTexts.FormatDeleteHint(Created));
    }
}

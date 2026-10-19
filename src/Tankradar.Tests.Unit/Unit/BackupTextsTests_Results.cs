using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass jeder Wert von <see cref="BackupResult"/> eine zentrale, verständliche Meldung hat.
/// </summary>
public class BackupTextsTests_Results : BaseTest
{
    /// <summary>
    /// Prüft für jeden Wert von <see cref="BackupResult"/>, dass die Meldung nicht leer ist und ohne Fachbegriffe auskommt.
    /// </summary>
    [Fact]
    public void GetMessage_AllResults_HaveText()
    {
        foreach (var result in Enum.GetValues<BackupResult>())
        {
            var message = BackupTexts.GetMessage(result, "0.2.0");
            Assert.False(string.IsNullOrWhiteSpace(message), result.ToString());
            Assert.DoesNotContain("Exception", message, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Prüft die Wortlaute der Fehlerfälle: ungültig, zu neu, nicht gefunden, ausgelastet, fehlgeschlagen; Daten bleiben unverändert.
    /// </summary>
    [Fact]
    public void GetMessage_ErrorCases_AreExact()
    {
        Assert.Equal("Die Datei ist keine gültige Tankatlas-Sicherung oder beschädigt. Deine Daten wurden nicht verändert.", BackupTexts.GetMessage(BackupResult.InvalidFile));
        Assert.Equal("Diese Sicherung stammt aus einer neueren Version von Tankatlas (0.2.0). Bitte aktualisiere die App. Deine Daten wurden nicht verändert.", BackupTexts.GetMessage(BackupResult.TooNew, "0.2.0"));
        Assert.Equal("Diese Sicherung stammt aus einer neueren Version von Tankatlas. Bitte aktualisiere die App. Deine Daten wurden nicht verändert.", BackupTexts.GetMessage(BackupResult.TooNew));
        Assert.Equal("Die Sicherung wurde nicht gefunden.", BackupTexts.GetMessage(BackupResult.NotFound));
        Assert.Contains("Deine Daten wurden nicht verändert.", BackupTexts.GetMessage(BackupResult.Failed), StringComparison.Ordinal);
        Assert.Contains("bereits ein anderer", BackupTexts.GetMessage(BackupResult.Busy), StringComparison.Ordinal);
        Assert.Equal("Der Vorgang wurde abgeschlossen.", BackupTexts.GetMessage(BackupResult.Ok));
    }

    /// <summary>
    /// Prüft, dass der Hinweis der Karte den Umfang, die lokale Ablage und die fehlende Verschlüsselung nennt.
    /// </summary>
    [Fact]
    public void Hint_NamesScopeAndEncryption()
    {
        Assert.Contains("Zugangsschlüssel zum Preisdienst", BackupTexts.Hint, StringComparison.Ordinal);
        Assert.Contains("nur auf diesem Gerät", BackupTexts.Hint, StringComparison.Ordinal);
        Assert.Contains("nicht verschlüsselt", BackupTexts.Hint, StringComparison.Ordinal);
        Assert.Contains("nicht automatisch", BackupTexts.Hint, StringComparison.Ordinal);
    }
}

using FlaUI.Core.Definitions;
using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// Basis der E2E-Tests der Sicherung: Bedienung der Karte „Sicherung“ in den Optionen per UI-Automation und Zugriff auf die Dateien im Testdatenverzeichnis.
/// </summary>
public abstract class BackupE2ETestBase : TankbookE2ETestBase
{
    /// <summary>
    /// Die Meldung nach einer erfolgreichen Sicherung.
    /// </summary>
    protected const string CreatedMessage = "Sicherung erstellt.";

    /// <summary>
    /// Die Meldung nach einer erfolgreichen Wiederherstellung.
    /// </summary>
    protected const string RestoredMessage = "Sicherung wiederhergestellt. Die Daten sind auf den Stand der Sicherung zurückgesetzt.";

    /// <summary>
    /// Startet die App ohne zusätzliche Umgebung.
    /// </summary>
    protected BackupE2ETestBase()
    {
    }

    /// <summary>
    /// Startet die App mit zusätzlicher Umgebung (z. B. Test-Fotoauswahl).
    /// </summary>
    /// <param name="additionalEnvironment">Die zusätzlichen Umgebungsvariablen.</param>
    protected BackupE2ETestBase(IReadOnlyDictionary<string, string> additionalEnvironment)
        : base(additionalEnvironment)
    {
    }

    /// <summary>
    /// Das Verzeichnis der Sicherungen im Testdatenverzeichnis.
    /// </summary>
    protected string BackupDirectory
    {
        get { return Path.Combine(TestDataDirectory, "backups"); }
    }

    /// <summary>
    /// Das Verzeichnis, in das der Testmodus „Teilen“ kopiert.
    /// </summary>
    protected string SharedDirectory
    {
        get { return Path.Combine(TestDataDirectory, "test-shared"); }
    }

    /// <summary>
    /// Öffnet die Optionen und wartet auf die Karte „Sicherung“.
    /// </summary>
    protected void OpenBackup()
    {
        OpenSettings();
        WaitForAutomationId("Settings.Backup.Create");
    }

    /// <summary>
    /// Liefert die AutomationId eines Elements eines Listeneintrags (1-basiert, neueste Sicherung zuerst).
    /// </summary>
    /// <param name="item">Der Listenplatz ab 1.</param>
    /// <param name="element">Der Elementname, z. B. <c>Restore</c> oder <c>RestoreQuestion</c>.</param>
    /// <returns>Die AutomationId.</returns>
    protected static string ItemId(int item, string element)
    {
        return $"Settings.Backup.Item.{item}.{element}";
    }

    /// <summary>
    /// Wartet, bis die Statusmeldung der Karte genau den Text zeigt.
    /// </summary>
    /// <param name="expected">Der erwartete Text.</param>
    protected void WaitForStatus(string expected)
    {
        WaitUntil(() => FindByAutomationId("Settings.Backup.Status")?.Name == expected, $"Die Statusmeldung der Sicherung ist nicht „{expected}“, zuletzt: „{FindByAutomationId("Settings.Backup.Status")?.Name}“.");
    }

    /// <summary>
    /// Wartet, bis die Statusmeldung der Karte den Text enthält.
    /// </summary>
    /// <param name="fragment">Der erwartete Textteil.</param>
    protected void WaitForStatusContaining(string fragment)
    {
        WaitUntil(() => FindByAutomationId("Settings.Backup.Status")?.Name.Contains(fragment, StringComparison.Ordinal) == true, $"Die Statusmeldung der Sicherung enthält nicht „{fragment}“, zuletzt: „{FindByAutomationId("Settings.Backup.Status")?.Name}“.");
    }

    /// <summary>
    /// Wartet, bis ein Element nicht mehr vorhanden ist.
    /// </summary>
    /// <param name="automationId">Die AutomationId.</param>
    protected void WaitUntilGone(string automationId)
    {
        WaitUntil(() => FindByAutomationId(automationId) is null, $"'{automationId}' ist weiterhin sichtbar.");
    }

    /// <summary>
    /// Die Sicherungsdateien im Testdatenverzeichnis.
    /// </summary>
    /// <returns>Die Dateinamen.</returns>
    protected string[] BackupFiles()
    {
        return Directory.Exists(BackupDirectory) ? Directory.GetFiles(BackupDirectory, "*.zip").Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal).ToArray() : [];
    }

    /// <summary>
    /// Wartet, bis die Anzahl der Sicherungsdateien dem Erwartungswert entspricht.
    /// </summary>
    /// <param name="count">Die erwartete Anzahl.</param>
    protected void WaitForBackupFileCount(int count)
    {
        WaitUntil(() => BackupFiles().Length == count, $"Es gibt nicht {count} Sicherungsdateien, sondern {BackupFiles().Length}.");
    }

    /// <summary>
    /// Erstellt eine Sicherung über die Oberfläche und wartet auf die Bestätigung.
    /// </summary>
    protected void CreateBackup()
    {
        Press("Settings.Backup.Create");
        WaitForStatus(CreatedMessage);
    }

    /// <summary>
    /// Stellt die Sicherung am Listenplatz über die Rückfrage wieder her und wartet auf die Meldung.
    /// </summary>
    /// <param name="item">Der Listenplatz ab 1.</param>
    protected void RestoreBackup(int item)
    {
        Press(ItemId(item, "Restore"));
        WaitForAutomationId(ItemId(item, "RestoreQuestion"));
        Press(ItemId(item, "RestoreConfirm"));
        WaitForStatus(RestoredMessage);
    }

    /// <summary>
    /// Liest, ob der Schalter „In der Nähe“ eingeschaltet ist.
    /// </summary>
    /// <returns><see langword="true"/>, wenn eingeschaltet.</returns>
    protected bool IsNearbyOn()
    {
        return WaitForAutomationId("Settings.Nearby.Switch").Patterns.Toggle.Pattern.ToggleState.Value == ToggleState.On;
    }

    /// <summary>
    /// Pfad einer eindeutigen Datei im gemeinsamen Temp-Verzeichnis der E2E-Tests (außerhalb des Testdatenverzeichnisses).
    /// </summary>
    /// <param name="extension">Die Endung mit Punkt.</param>
    /// <returns>Der Pfad.</returns>
    protected static string NewExternalPath(string extension)
    {
        var directory = Path.Combine(Path.GetTempPath(), "Tankradar.Tests.E2E");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "extern-" + Guid.NewGuid().ToString("N") + extension);
    }

    /// <summary>
    /// Umgebung für den Start mit Test-Dateiauswahl.
    /// </summary>
    /// <param name="path">Der Pfad der Datei, die die Auswahl liefert.</param>
    /// <returns>Die Umgebungsvariablen.</returns>
    protected static Dictionary<string, string> ImportEnvironment(string path)
    {
        return new Dictionary<string, string> { [TestDataPaths.TestBackupImportEnvironmentVariable] = path };
    }
}

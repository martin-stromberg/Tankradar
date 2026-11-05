namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: „Teilen“ im Testmodus legt eine Kopie ab, ohne dass ein blockierender Systemdialog erscheint.
/// </summary>
public class BackupE2ETests_Share : BackupE2ETestBase
{
    /// <summary>
    /// Prüft, dass die Datei im Testverzeichnis für geteilte Dateien landet und die Oberfläche bedienbar bleibt.
    /// </summary>
    [Fact]
    public void Share_CopiesFileInTestMode_WithoutDialog()
    {
        RunWithDiagnostics(() =>
        {
            OpenBackup();
            CreateBackup();
            var name = BackupFiles().Single();

            Press(ItemId(1, "Share"));

            WaitUntil(() => File.Exists(Path.Combine(SharedDirectory, name)), "Die geteilte Sicherung liegt nicht im Testverzeichnis.");
            Assert.Equal(new FileInfo(Path.Combine(BackupDirectory, name)).Length, new FileInfo(Path.Combine(SharedDirectory, name)).Length);
            WaitForAutomationId("Settings.Backup.Create");
            Assert.NotEqual("Die Sicherung konnte nicht weitergegeben werden.", FindByAutomationId("Settings.Backup.Status")?.Name);
        });
    }
}

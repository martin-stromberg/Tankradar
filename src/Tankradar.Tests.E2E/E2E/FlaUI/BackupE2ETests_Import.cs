namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Eine externe Sicherungsdatei wird über die Test-Dateiauswahl (ohne Systemdialog) eingespielt, geprüft und wiederhergestellt.
/// </summary>
public class BackupE2ETests_Import : BackupE2ETestBase
{
    private string? _externalPath;

    /// <summary>
    /// Prüft die Kette Auswahl, Prüfung, Aufnahme in die Liste, Rückfrage und Wiederherstellung mit einer zuvor kopierten Sicherung.
    /// </summary>
    [Fact]
    public void ImportExternalFile_AddsToListAndRestores()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "84350", "5,4");
            OpenBackup();
            CreateBackup();
            _externalPath = NewExternalPath(".zip");
            File.Copy(Path.Combine(BackupDirectory, BackupFiles().Single()), _externalPath);
            Press(ItemId(1, "Delete"));
            Press(ItemId(1, "DeleteConfirm"));
            WaitForAutomationId("Settings.Backup.Empty");
            WaitForBackupFileCount(0);
            OpenTankbook();
            CreateVehicle("Opel", "Petrol", "1000", string.Empty);

            RestartApplication(ImportEnvironment(_externalPath));
            OpenBackup();
            WaitForAutomationId("Settings.Backup.Empty");
            Press("Settings.Backup.Import");

            WaitForStatus("Die Datei ist eine gültige Sicherung und wurde in die Liste übernommen.");
            WaitForAutomationId(ItemId(1, "Title"));
            Assert.Contains("Sicherung vom", WaitForAutomationId(ItemId(1, "RestoreQuestion")).Name, StringComparison.Ordinal);
            WaitForBackupFileCount(1);
            Press(ItemId(1, "RestoreConfirm"));
            WaitForStatus(RestoredMessage);
            OpenTankbook();
            Tankbook.WaitForTexts("Tankbook.Vehicle.Name", "Golf");
        });
    }

    /// <summary>
    /// Prüft, dass ein Abbruch der Dateiauswahl (keine Datei konfiguriert) nichts verändert und keine Meldung zeigt.
    /// </summary>
    [Fact]
    public void ImportWithoutFile_IsCancelledSilently()
    {
        RunWithDiagnostics(() =>
        {
            OpenBackup();
            Press("Settings.Backup.Import");
            Thread.Sleep(500);

            WaitForAutomationId("Settings.Backup.Empty");
            Assert.False(Exists("Settings.Backup.Status"));
            Assert.Empty(BackupFiles());
        });
    }

    /// <inheritdoc />
    protected override void Cleanup()
    {
        if (_externalPath is not null)
        {
            File.Delete(_externalPath);
        }

        base.Cleanup();
    }
}

using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Beschädigte, manipulierte und zu neue Sicherungsdateien werden beim Einspielen mit verständlicher Meldung abgelehnt, ohne Daten oder Liste zu verändern.
/// </summary>
public class BackupE2ETests_InvalidFiles : BackupE2ETestBase
{
    private const string InvalidMessage = "Die Datei ist keine gültige Tankatlas-Sicherung oder beschädigt. Deine Daten wurden nicht verändert.";

    private readonly List<string> _externalPaths = [];

    /// <summary>
    /// Prüft eine Textdatei mit der Endung .zip, ein Archiv mit Zip-Slip-Eintrag und ein Archiv mit falscher Prüfsumme nacheinander.
    /// </summary>
    [Fact]
    public void BrokenAndManipulatedFiles_AreRejected_WithoutChangingData()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "84350", "5,4");
            var text = NewExternalFile(path => File.WriteAllText(path, "Das ist keine Sicherung."));
            var slip = NewExternalFile(path => BackupTestArchive.Create(path, new byte[64], extraEntries: [("../evil.txt", [1, 2, 3])], listExtraEntriesInManifest: true));
            var hash = NewExternalFile(path => BackupTestArchive.Create(path, new byte[64], wrongDatabaseHash: true));

            foreach (var file in new[] { text, slip, hash })
            {
                RestartApplication(ImportEnvironment(file));
                OpenBackup();
                Press("Settings.Backup.Import");
                WaitForStatus(InvalidMessage);
                WaitForAutomationId("Settings.Backup.Empty");
                Assert.Empty(BackupFiles());
            }

            OpenTankbook();
            Tankbook.WaitForTexts("Tankbook.Vehicle.Name", "Golf");
        });
    }

    /// <summary>
    /// Prüft, dass eine Sicherung einer neueren App-Version mit dem Hinweis zur Aktualisierung abgelehnt wird.
    /// </summary>
    [Fact]
    public void TooNewBackup_IsRejected_WithUpdateHint()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "84350", "5,4");
            var tooNew = NewExternalFile(path => BackupTestArchive.Create(path, new byte[64], schemaVersion: 99, appVersion: "9.9.9"));

            RestartApplication(ImportEnvironment(tooNew));
            OpenBackup();
            Press("Settings.Backup.Import");

            WaitForStatusContaining("neueren Version von Tankatlas");
            WaitForStatusContaining("Deine Daten wurden nicht verändert.");
            WaitForAutomationId("Settings.Backup.Empty");
            Assert.Empty(BackupFiles());
            OpenTankbook();
            Tankbook.WaitForTexts("Tankbook.Vehicle.Name", "Golf");
        });
    }

    /// <inheritdoc />
    protected override void Cleanup()
    {
        foreach (var path in _externalPaths)
        {
            File.Delete(path);
        }

        base.Cleanup();
    }

    private string NewExternalFile(Action<string> create)
    {
        var path = NewExternalPath(".zip");
        create(path);
        _externalPaths.Add(path);
        return path;
    }
}

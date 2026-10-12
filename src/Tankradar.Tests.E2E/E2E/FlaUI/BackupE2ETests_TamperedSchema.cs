using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Eine Sicherung mit gültigem Manifest und gültigen Prüfsummen, deren Datenbank manipuliert ist (Trigger, View, fehlende oder zusätzliche Tabelle oder Spalte, zusätzlicher Index),
/// wird beim Einspielen abgelehnt; Daten und Liste bleiben unverändert.
/// </summary>
public class BackupE2ETests_TamperedSchema : BackupE2ETestBase
{
    private const string InvalidMessage = "Die Datei ist keine gültige Tankatlas-Sicherung oder beschädigt. Deine Daten wurden nicht verändert.";

    private readonly List<string> _externalPaths = [];

    /// <summary>
    /// Prüft nacheinander alle Arten der Schemamanipulation: Meldung, unveränderte Liste und weiterhin vorhandenes Fahrzeug samt Möglichkeit, ein weiteres anzulegen (Trigger wirkungslos).
    /// </summary>
    [Fact]
    public void TamperedDatabases_AreRejected_WithoutChangingData()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "84350", "5,4");
            OpenBackup();
            CreateBackup();
            var source = Path.Combine(BackupDirectory, BackupFiles().Single());
            var files = Enum.GetValues<BackupTamper>().Select(tamper =>
            {
                var path = NewExternalPath(".zip");
                BackupTestTampering.CreateTamperedCopy(source, path, tamper);
                _externalPaths.Add(path);
                return path;
            }).ToList();

            foreach (var file in files)
            {
                RestartApplication(ImportEnvironment(file));
                OpenBackup();
                WaitForAutomationId(ItemId(1, "Title"));
                Press("Settings.Backup.Import");
                WaitForStatus(InvalidMessage);
                Assert.Single(BackupFiles());
            }

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
}

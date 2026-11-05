using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Eine Sicherung mit gültigem Manifest und gültigen Prüfsummen, deren Datenbank präpariert ist, wird nie ungeprüft übernommen:
/// Trigger (auch mit reserviertem Namenspräfix) und Zeile in der Migrationssperre bleiben ohne Wirkung (die App bleibt benutzbar, der Import hängt nicht), ein ungültiges Datum wird abgelehnt.
/// </summary>
public class BackupE2ETests_TamperedSchema : BackupE2ETestBase
{
    private const string InvalidMessage = "Die Datei ist keine gültige Tankatlas-Sicherung oder beschädigt. Deine Daten wurden nicht verändert.";

    private readonly List<string> _externalPaths = [];

    /// <summary>
    /// Prüft, dass ein Trigger mit dem Präfix <c>sqlite_</c> nicht in die Livedatenbank gelangt: Nach dem Wiederherstellen bleiben die Vorgänge beim Anlegen eines weiteren Fahrzeugs erhalten.
    /// </summary>
    [Fact]
    public void PrefixedTrigger_HasNoEffectAfterRestore()
    {
        RunWithDiagnostics(() =>
        {
            ImportAndRestoreTampered(BackupTamper.PrefixedTrigger);

            OpenTankbook();
            CreateVehicle("Neu", "Petrol", "100", string.Empty);
            Tankbook.OpenEntries("Golf");
            Tankbook.WaitUntil(() => Tankbook.ReadEntryTexts("TankbookEntry.Item.Station").Count == 1, "Der Vorgang von Golf ist nach dem Anlegen eines Fahrzeugs verschwunden.");
            Tankbook.CreateEntry("Beta Tankstelle", "30", "1,859", string.Empty, "85500");
            Assert.Equal(2, TankbookTestDatabase.CountEntries(DatabasePath));
        });
    }

    /// <summary>
    /// Prüft, dass eine Zeile in der Migrationssperre das Einspielen nicht blockiert: Der Import endet mit der Rückfrage, die Wiederherstellung gelingt, und alle Bedienelemente bleiben benutzbar.
    /// </summary>
    [Fact]
    public void MigrationLockRow_DoesNotBlockImportAndRestore()
    {
        RunWithDiagnostics(() =>
        {
            ImportAndRestoreTampered(BackupTamper.MigrationLockRow);

            OpenTankbook();
            CreateVehicle("Neu", "Petrol", "100", string.Empty);
            Tankbook.WaitForTexts("Tankbook.Vehicle.Name", "Golf", "Neu");
            OpenBackup();
            CreateBackup();
        });
    }

    /// <summary>
    /// Prüft, dass ein ungültiger Datumswert in der Datenbank schon beim Einspielen abgelehnt wird; Liste und Daten bleiben unverändert.
    /// </summary>
    [Fact]
    public void InvalidDate_IsRejected_WithoutChangingData()
    {
        RunWithDiagnostics(() =>
        {
            var file = PrepareTamperedFile(BackupTamper.InvalidDate);

            RestartApplication(ImportEnvironment(file));
            OpenBackup();
            WaitForAutomationId("Settings.Backup.Empty");
            Press("Settings.Backup.Import");
            WaitForStatus(InvalidMessage);
            WaitForBackupFileCount(0);
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

    private string PrepareTamperedFile(BackupTamper tamper)
    {
        OpenTankbook();
        CreateVehicle("Golf", "Diesel", "84350", "5,4");
        Tankbook.OpenEntries("Golf");
        Tankbook.CreateEntry("Alpha Tankstelle", "40", "1,859", string.Empty, "84900");
        OpenBackup();
        CreateBackup();
        var path = NewExternalPath(".zip");
        BackupTestTampering.CreateTamperedCopy(Path.Combine(BackupDirectory, BackupFiles().Single()), path, tamper);
        _externalPaths.Add(path);
        Press(ItemId(1, "Delete"));
        Press(ItemId(1, "DeleteConfirm"));
        WaitForAutomationId("Settings.Backup.Empty");
        WaitForBackupFileCount(0);
        return path;
    }

    private void ImportAndRestoreTampered(BackupTamper tamper)
    {
        var file = PrepareTamperedFile(tamper);
        OpenTankbook();
        CreateVehicle("Opel", "Petrol", "1000", string.Empty);

        RestartApplication(ImportEnvironment(file));
        OpenBackup();
        WaitForAutomationId("Settings.Backup.Empty");
        Press("Settings.Backup.Import");

        WaitForStatus("Die Datei ist eine gültige Sicherung und wurde in die Liste übernommen.");
        var question = WaitForAutomationId(ItemId(1, "RestoreQuestion")).Name;
        Assert.Contains("1 Fahrzeug,", question, StringComparison.Ordinal);
        Assert.Contains("1 Tank- oder Ladevorgang,", question, StringComparison.Ordinal);
        Press(ItemId(1, "RestoreConfirm"));
        WaitForStatus(RestoredMessage);
    }
}

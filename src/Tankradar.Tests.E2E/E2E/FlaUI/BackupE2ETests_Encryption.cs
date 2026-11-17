namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Eine mit Passwort verschlüsselte Sicherung wird erstellt, in der Liste gekennzeichnet und über das Passwort-Stadium wiederhergestellt (Issue #67, ADR 0019).
/// </summary>
public class BackupE2ETests_Encryption : BackupE2ETestBase
{
    private const string Password = "geheim-123";
    private string? _externalPath;

    /// <summary>
    /// Prüft den vollen Weg: mit Passwort erstellen, Kennzeichnung in der Liste, Passwort-Stadium, Rückfrage, Wiederherstellung der Daten.
    /// </summary>
    [Fact]
    public void CreateEncrypted_MarkedAndRestoresThroughPasswordStage()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "84350", "5,4");
            OpenBackup();
            EnterPassword(Password);
            CreateBackup();

            WaitUntil(() => FindByAutomationId(ItemId(1, "Title"))?.Name.Contains("(verschlüsselt)", StringComparison.Ordinal) == true, "Der Eintrag trägt keine Verschlüsselungskennzeichnung.");

            OpenTankbook();
            CreateVehicle("Opel", "Petrol", "1000", string.Empty);
            OpenBackup();

            RestoreEncryptedBackup(1, Password);

            OpenTankbook();
            Tankbook.WaitForTexts("Tankbook.Vehicle.Name", "Golf");
        });
    }

    /// <summary>
    /// Prüft, dass unterschiedliche Passwörter gemeldet werden und keine Datei entsteht.
    /// </summary>
    [Fact]
    public void Create_PasswordMismatch_ShowsMessageWithoutFile()
    {
        RunWithDiagnostics(() =>
        {
            OpenBackup();
            EnterPassword("eins", "zwei");

            Press("Settings.Backup.Create");

            WaitForStatusContaining("nicht überein");
            WaitForAutomationId("Settings.Backup.Empty");
            Assert.Empty(BackupFiles());
        });
    }

    /// <summary>
    /// Prüft, dass ein falsches Passwort gemeldet wird, die Daten unverändert bleiben und die Eingabe direkt erneut versucht werden kann.
    /// </summary>
    [Fact]
    public void Restore_WrongPassword_DataUnchangedThenRetrySucceeds()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "84350", "5,4");
            OpenBackup();
            EnterPassword(Password);
            CreateBackup();
            OpenTankbook();
            CreateVehicle("Opel", "Petrol", "1000", string.Empty);
            OpenBackup();

            Press(ItemId(1, "Restore"));
            EnterRestorePassword(1, "falsch");

            WaitForStatusContaining("falsch");
            WaitForPasswordStage(1);

            EnterRestorePassword(1, Password);
            WaitForAutomationId(ItemId(1, "RestoreConfirm"));
            Press(ItemId(1, "RestoreConfirm"));
            WaitForStatus(RestoredMessage);

            OpenTankbook();
            Tankbook.WaitForTexts("Tankbook.Vehicle.Name", "Golf");
        });
    }

    /// <summary>
    /// Prüft, dass „Abbrechen“ im Passwort-Stadium die Rückfrage schließt und nichts wiederherstellt.
    /// </summary>
    [Fact]
    public void Restore_CancelAtPasswordStage_ClosesQuestion()
    {
        RunWithDiagnostics(() =>
        {
            OpenBackup();
            EnterPassword(Password);
            CreateBackup();

            Press(ItemId(1, "Restore"));
            WaitForPasswordStage(1);
            Press(ItemId(1, "RestoreCancel"));

            WaitUntilGone(ItemId(1, "RestorePassword"));
            Assert.Null(FindByAutomationId(ItemId(1, "RestoreConfirm")));
        });
    }

    /// <summary>
    /// Prüft das Einspielen einer verschlüsselten Datei: Passwortbereich, Aufnahme in die Liste und Wiederherstellung über die Zählwert-Rückfrage ohne erneute Passworteingabe.
    /// </summary>
    [Fact]
    public void ImportEncrypted_AsksPasswordAndRestores()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "84350", "5,4");
            OpenBackup();
            EnterPassword(Password);
            CreateBackup();
            _externalPath = NewExternalPath(".zip");
            File.Copy(Path.Combine(BackupDirectory, BackupFiles().Single()), _externalPath);
            Press(ItemId(1, "Delete"));
            Press(ItemId(1, "DeleteConfirm"));
            WaitForAutomationId("Settings.Backup.Empty");

            RestartApplication(ImportEnvironment(_externalPath));
            OpenBackup();
            Press("Settings.Backup.Import");

            WaitForAutomationId("Settings.Backup.ImportPassword");
            TypePassword("Settings.Backup.ImportPassword", Password);
            Press("Settings.Backup.ImportPasswordConfirm");

            WaitForStatus("Die Datei ist eine gültige Sicherung und wurde in die Liste übernommen.");
            WaitUntil(() => FindByAutomationId(ItemId(1, "Title"))?.Name.Contains("(verschlüsselt)", StringComparison.Ordinal) == true, "Der eingespielte Eintrag trägt keine Verschlüsselungskennzeichnung.");
            WaitForBackupFileCount(1);

            // Das beim Einspielen eingegebene Passwort gilt für die Rückfrage mit; ein zweites Mal wird es nicht abgefragt.
            WaitForAutomationId(ItemId(1, "RestoreConfirm"));
            Press(ItemId(1, "RestoreConfirm"));
            WaitForStatus(RestoredMessage);

            OpenTankbook();
            Tankbook.WaitForTexts("Tankbook.Vehicle.Name", "Golf");
        });
    }

    /// <summary>
    /// Prüft, dass „Abbrechen“ im Passwortbereich des Einspielens die Datei verwirft und die Liste leer bleibt.
    /// </summary>
    [Fact]
    public void ImportEncrypted_Cancel_LeavesListEmpty()
    {
        RunWithDiagnostics(() =>
        {
            OpenBackup();
            EnterPassword(Password);
            CreateBackup();
            _externalPath = NewExternalPath(".zip");
            File.Copy(Path.Combine(BackupDirectory, BackupFiles().Single()), _externalPath);
            Press(ItemId(1, "Delete"));
            Press(ItemId(1, "DeleteConfirm"));
            WaitForAutomationId("Settings.Backup.Empty");

            RestartApplication(ImportEnvironment(_externalPath));
            OpenBackup();
            Press("Settings.Backup.Import");

            WaitForAutomationId("Settings.Backup.ImportPassword");
            Press("Settings.Backup.ImportPasswordCancel");

            WaitUntilGone("Settings.Backup.ImportPassword");
            WaitForAutomationId("Settings.Backup.Empty");
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

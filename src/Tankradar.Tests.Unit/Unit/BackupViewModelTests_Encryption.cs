using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Passwortflüsse des <see cref="Tankradar.MAUI.ViewModels.BackupViewModel"/>: verschlüsselt erstellen, entsperren, wiederherstellen und einspielen.
/// </summary>
public class BackupViewModelTests_Encryption : BackupViewModelTestBase
{
    private static PickedBackupFile Picked()
    {
        return new PickedBackupFile("extern.zip", () => Task.FromResult<Stream>(new MemoryStream([1])));
    }

    /// <summary>
    /// Prüft, dass ein gesetztes Passwort zum Dienst geht und beide Felder danach leer sind.
    /// </summary>
    [Fact]
    public async Task Create_WithPassword_PassesPasswordAndClearsFields()
    {
        await AppearAsync();
        ViewModel.CreatePassword = "geheim";
        ViewModel.CreatePasswordRepeat = "geheim";

        await PressAsync(ViewModel.CreateCommand);

        Assert.Equal(["geheim"], Service.CreatedPasswords);
        Assert.Equal(string.Empty, ViewModel.CreatePassword);
        Assert.Equal(string.Empty, ViewModel.CreatePasswordRepeat);
        Assert.Equal(BackupTexts.Created, ViewModel.StatusMessage);
        Assert.True(ViewModel.Items[0].Info.IsEncrypted);
        Assert.EndsWith("(verschlüsselt)", ViewModel.Items[0].Title, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass leere Passwortfelder eine unverschlüsselte Sicherung erzeugen.
    /// </summary>
    [Fact]
    public async Task Create_WithoutPassword_PassesNull()
    {
        await AppearAsync();

        await PressAsync(ViewModel.CreateCommand);

        Assert.Equal<string?>([null], Service.CreatedPasswords);
        Assert.False(ViewModel.Items[0].Info.IsEncrypted);
    }

    /// <summary>
    /// Prüft, dass unterschiedliche Passwörter gemeldet werden und der Dienst nicht aufgerufen wird.
    /// </summary>
    [Fact]
    public async Task Create_PasswordMismatch_ShowsMessageAndSkipsService()
    {
        await AppearAsync();
        ViewModel.CreatePassword = "eins";
        ViewModel.CreatePasswordRepeat = "zwei";

        await PressAsync(ViewModel.CreateCommand);

        Assert.Equal(0, Service.CreateCount);
        Assert.Equal(BackupTexts.PasswordsDoNotMatch, ViewModel.StatusMessage);
        Assert.Equal(string.Empty, ViewModel.CreatePassword);
    }

    /// <summary>
    /// Prüft, dass eine verschlüsselte Sicherung zuerst das Passwort-Stadium zeigt, ohne den Dienst zu befragen.
    /// </summary>
    [Fact]
    public async Task Restore_Encrypted_ShowsPasswordStageFirst()
    {
        Service.Files = [new BackupFileInfo("tankatlas-backup-enc.zip", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 5, string.Empty, true, true)];
        await AppearAsync();
        var item = ViewModel.Items[0];

        await PressAsync(item.RestoreCommand);

        Assert.True(item.IsConfirmingRestore);
        Assert.True(item.IsPasswordStage);
        Assert.False(item.IsCountStage);
        Assert.Equal(BackupTexts.PasswordQuestion, item.RestoreQuestion);
        Assert.Empty(Service.Inspected);
    }

    /// <summary>
    /// Prüft, dass die Passwortprüfung das Passwort weitergibt und bei Erfolg die Zählwerte zeigt.
    /// </summary>
    [Fact]
    public async Task Unlock_CorrectPassword_ShowsCounts()
    {
        Service.Files = [new BackupFileInfo("tankatlas-backup-enc.zip", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 5, string.Empty, true, true)];
        await AppearAsync();
        var item = ViewModel.Items[0];
        await PressAsync(item.RestoreCommand);
        item.RestorePassword = "geheim";

        await PressAsync(item.UnlockRestoreCommand);

        Assert.Equal(["geheim"], Service.InspectedPasswords);
        Assert.True(item.IsConfirmingRestore);
        Assert.False(item.IsPasswordStage);
        Assert.True(item.IsCountStage);
        Assert.Contains("vollständig ersetzt", item.RestoreQuestion, StringComparison.Ordinal);
        Assert.Equal("geheim", item.RestorePassword);
    }

    /// <summary>
    /// Prüft, dass „Prüfen“ mit leerem Passwortfeld die fehlende Eingabe meldet und die Abfrage offen lässt.
    /// </summary>
    [Fact]
    public async Task Unlock_EmptyPassword_ShowsMessageAndKeepsStage()
    {
        Service.Files = [new BackupFileInfo("tankatlas-backup-enc.zip", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 5, string.Empty, true, true)];
        await AppearAsync();
        var item = ViewModel.Items[0];
        await PressAsync(item.RestoreCommand);
        Service.InspectResult = BackupResult.PasswordRequired;

        await PressAsync(item.UnlockRestoreCommand);

        Assert.Equal(BackupTexts.PasswordRequired, ViewModel.StatusMessage);
        Assert.True(item.IsConfirmingRestore);
        Assert.True(item.IsPasswordStage);
    }

    /// <summary>
    /// Prüft, dass ein falsches Passwort gemeldet wird und die Abfrage zum erneuten Versuch offen bleibt.
    /// </summary>
    [Fact]
    public async Task Unlock_WrongPassword_ShowsMessageAndKeepsStage()
    {
        Service.Files = [new BackupFileInfo("tankatlas-backup-enc.zip", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 5, string.Empty, true, true)];
        await AppearAsync();
        var item = ViewModel.Items[0];
        await PressAsync(item.RestoreCommand);
        item.RestorePassword = "falsch";
        Service.InspectResult = BackupResult.WrongPassword;

        await PressAsync(item.UnlockRestoreCommand);

        Assert.Equal(BackupTexts.WrongPassword, ViewModel.StatusMessage);
        Assert.True(item.IsPasswordStage);
        Assert.Equal(string.Empty, item.RestorePassword);
    }

    /// <summary>
    /// Prüft, dass die Bestätigung das geprüfte Passwort an die Wiederherstellung weitergibt.
    /// </summary>
    [Fact]
    public async Task ConfirmRestore_Encrypted_PassesPassword()
    {
        Service.Files = [new BackupFileInfo("tankatlas-backup-enc.zip", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 5, string.Empty, true, true)];
        await AppearAsync();
        var item = ViewModel.Items[0];
        await PressAsync(item.RestoreCommand);
        item.RestorePassword = "geheim";
        await PressAsync(item.UnlockRestoreCommand);

        await PressAsync(item.ConfirmRestoreCommand);

        Assert.Equal(["geheim"], Service.RestoredPasswords);
        Assert.Equal(BackupTexts.Restored, ViewModel.StatusMessage);
    }

    /// <summary>
    /// Prüft, dass eine unverschlüsselte Sicherung ohne Passwort wiederhergestellt wird.
    /// </summary>
    [Fact]
    public async Task ConfirmRestore_Unencrypted_PassesNull()
    {
        await AppearAsync();
        var item = ViewModel.Items[0];
        await PressAsync(item.RestoreCommand);

        await PressAsync(item.ConfirmRestoreCommand);

        Assert.Equal<string?>([null], Service.RestoredPasswords);
    }

    /// <summary>
    /// Prüft, dass eine verschlüsselte Datei beim Einspielen die Passwortabfrage öffnet und das eingegebene Passwort direkt zur Zählwert-Rückfrage führt.
    /// </summary>
    [Fact]
    public async Task Import_Encrypted_AsksPasswordAndShowsCounts()
    {
        await AppearAsync();
        Picker.File = Picked();
        Service.ImportResults.Enqueue(BackupResult.PasswordRequired);
        Service.ImportedFile = new BackupFileInfo("tankatlas-backup-enc.zip", new DateTime(2026, 10, 12, 13, 0, 0, DateTimeKind.Utc), 2048, string.Empty, true, true);

        await PressAsync(ViewModel.ImportCommand);

        Assert.True(ViewModel.IsAwaitingImportPassword);
        Assert.Equal(BackupTexts.ImportPasswordQuestion, ViewModel.ImportPasswordText);

        ViewModel.ImportPassword = "geheim";
        await PressAsync(ViewModel.ConfirmImportPasswordCommand);

        Assert.False(ViewModel.IsAwaitingImportPassword);
        Assert.Equal<string?>([null, "geheim"], Service.ImportedPasswords);
        var item = ViewModel.Items.Single(candidate => candidate.FileName == Service.ImportedFile.FileName);
        // Das beim Einspielen geprüfte Passwort wird für die Rückfrage weiterverwendet: keine zweite Abfrage, direkt die Zählwerte.
        Assert.Equal<string?>(["geheim"], Service.InspectedPasswords);
        Assert.True(item.IsConfirmingRestore);
        Assert.True(item.IsCountStage);
        Assert.Equal("geheim", item.RestorePassword);
        Assert.Equal(BackupTexts.Imported, ViewModel.StatusMessage);
        Assert.Equal(string.Empty, ViewModel.ImportPassword);
    }

    /// <summary>
    /// Prüft, dass „Prüfen“ der Einspiel-Abfrage mit leerem Feld die fehlende Eingabe meldet, ohne den Dienst erneut zu befragen.
    /// </summary>
    [Fact]
    public async Task Import_ConfirmEmptyPassword_ShowsMessageAndStays()
    {
        await AppearAsync();
        Picker.File = Picked();
        Service.ImportResults.Enqueue(BackupResult.PasswordRequired);
        await PressAsync(ViewModel.ImportCommand);
        Assert.True(ViewModel.IsAwaitingImportPassword);

        await PressAsync(ViewModel.ConfirmImportPasswordCommand);

        Assert.Equal(BackupTexts.PasswordRequired, ViewModel.StatusMessage);
        Assert.True(ViewModel.IsAwaitingImportPassword);
        Assert.Single(Service.Imported);
    }

    /// <summary>
    /// Prüft, dass „Abbrechen“ der Passwortabfrage die gewählte Datei verwirft.
    /// </summary>
    [Fact]
    public async Task Import_Encrypted_Cancel_DiscardsFile()
    {
        await AppearAsync();
        Picker.File = Picked();
        Service.ImportResults.Enqueue(BackupResult.PasswordRequired);
        await PressAsync(ViewModel.ImportCommand);

        ViewModel.CancelImportPasswordCommand.Execute(null);

        Assert.False(ViewModel.IsAwaitingImportPassword);
        Assert.Single(Service.Imported);
        Assert.Equal(2, ViewModel.Items.Count);
    }
}

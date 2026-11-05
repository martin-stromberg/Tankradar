using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Teilen und Einspielen einer externen Datei im <see cref="Tankradar.MAUI.ViewModels.BackupViewModel"/>.
/// </summary>
public class BackupViewModelTests_ShareImport : BackupViewModelTestBase
{
    private static PickedBackupFile Picked()
    {
        return new PickedBackupFile("extern.zip", () => Task.FromResult<Stream>(new MemoryStream([1])));
    }

    /// <summary>
    /// Prüft, dass „Teilen“ den Pfad der gewählten Sicherung an die Systemfunktion übergibt.
    /// </summary>
    [Fact]
    public async Task Share_PassesFullPathOfSelectedFile()
    {
        await AppearAsync();

        await PressAsync(ViewModel.Items[1].ShareCommand);

        Assert.Equal([Path.Combine("C:\\daten\\backups", "tankatlas-backup-20261011-080000.zip")], Sharer.Shared);
        Assert.False(ViewModel.HasStatusMessage);
    }

    /// <summary>
    /// Prüft die Fehlermeldung, wenn das Teilen fehlschlägt, und die Meldung, wenn die Datei nicht mehr vorhanden ist.
    /// </summary>
    [Fact]
    public async Task Share_Failures_ShowMessages()
    {
        await AppearAsync();
        Sharer.Exception = new InvalidOperationException();
        await PressAsync(ViewModel.Items[0].ShareCommand);
        Assert.Equal(BackupTexts.ShareFailed, ViewModel.StatusMessage);

        Sharer.Exception = null;
        Service.PathAvailable = false;
        await PressAsync(ViewModel.Items[0].ShareCommand);
        Assert.Equal(BackupTexts.NotFound, ViewModel.StatusMessage);
    }

    /// <summary>
    /// Prüft, dass ein Abbruch der Dateiauswahl nichts meldet und den Dienst nicht aufruft.
    /// </summary>
    [Fact]
    public async Task Import_Cancelled_DoesNothing()
    {
        await AppearAsync();
        Picker.File = null;

        await PressAsync(ViewModel.ImportCommand);

        Assert.Equal(1, Picker.PickCount);
        Assert.Empty(Service.Imported);
        Assert.False(ViewModel.HasStatusMessage);
        Assert.False(ViewModel.IsBusy);
    }

    /// <summary>
    /// Prüft, dass eine ungültige oder zu neue Datei mit der zentralen Meldung abgelehnt wird und nicht in die Liste kommt.
    /// </summary>
    [Fact]
    public async Task Import_InvalidAndTooNew_ShowMessages()
    {
        await AppearAsync();
        Picker.File = Picked();
        Service.ImportResult = BackupResult.InvalidFile;
        await PressAsync(ViewModel.ImportCommand);
        Assert.Equal(BackupTexts.InvalidFile, ViewModel.StatusMessage);

        Service.ImportResult = BackupResult.TooNew;
        Service.Manifest.AppVersion = "0.9.0";
        await PressAsync(ViewModel.ImportCommand);
        Assert.Equal(BackupTexts.FormatTooNew("0.9.0"), ViewModel.StatusMessage);
        Assert.Equal(2, ViewModel.Items.Count);
    }

    /// <summary>
    /// Prüft, dass eine gültige Datei in die Liste aufgenommen wird und direkt die Rückfrage zur Wiederherstellung dieses Eintrags erscheint.
    /// </summary>
    [Fact]
    public async Task Import_Success_OpensRestoreQuestionForNewItem()
    {
        await AppearAsync();
        Picker.File = Picked();

        await PressAsync(ViewModel.ImportCommand);

        Assert.Equal(["extern.zip"], Service.Imported);
        Assert.Equal(3, ViewModel.Items.Count);
        var item = ViewModel.Items.Single(candidate => candidate.FileName == Service.ImportedFile.FileName);
        Assert.True(item.IsConfirmingRestore);
        Assert.Empty(Service.Restored);
        Assert.Equal(BackupTexts.Imported, ViewModel.StatusMessage);
    }

    /// <summary>
    /// Prüft, dass ein Fehler der Dateiauswahl gemeldet wird.
    /// </summary>
    [Fact]
    public async Task Import_PickerFails_ShowsMessage()
    {
        await AppearAsync();
        Picker.Exception = new InvalidOperationException();

        await PressAsync(ViewModel.ImportCommand);

        Assert.Equal(BackupTexts.PickFailed, ViewModel.StatusMessage);
        Assert.Empty(Service.Imported);
    }
}

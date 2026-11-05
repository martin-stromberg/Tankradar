using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Löschen einer Sicherung im <see cref="Tankradar.MAUI.ViewModels.BackupViewModel"/> nach Rückfrage.
/// </summary>
public class BackupViewModelTests_Delete : BackupViewModelTestBase
{
    /// <summary>
    /// Prüft, dass „Löschen“ zuerst die Rückfrage zeigt und noch nichts löscht, und dass „Abbrechen“ sie ohne Aufruf schließt.
    /// </summary>
    [Fact]
    public async Task Delete_AsksFirstAndCancelKeepsFile()
    {
        await AppearAsync();
        var item = ViewModel.Items[0];

        item.DeleteCommand.Execute(null);

        Assert.True(item.IsConfirmingDelete);
        Assert.Equal(BackupTexts.FormatDeleteQuestion(item.Info.CreatedUtc), item.DeleteQuestion);
        Assert.Equal(item.DeleteQuestion, Announcer.Announcements[^1]);
        Assert.Empty(Service.Deleted);

        item.CancelDeleteCommand.Execute(null);

        Assert.False(item.IsConfirmingDelete);
        Assert.Empty(Service.Deleted);
        Assert.Equal(2, ViewModel.Items.Count);
    }

    /// <summary>
    /// Prüft, dass die Bestätigung die gewählte Datei löscht, die Liste aktualisiert und die Meldung zeigt.
    /// </summary>
    [Fact]
    public async Task Delete_Confirm_DeletesAndUpdatesList()
    {
        await AppearAsync();
        var item = ViewModel.Items[1];
        item.DeleteCommand.Execute(null);

        await PressAsync(item.ConfirmDeleteCommand);

        Assert.Equal(["tankatlas-backup-20261011-080000.zip"], Service.Deleted);
        Assert.Single(ViewModel.Items);
        Assert.Equal(BackupTexts.Deleted, ViewModel.StatusMessage);
    }

    /// <summary>
    /// Prüft, dass die letzte Sicherung zur leeren Liste führt.
    /// </summary>
    [Fact]
    public async Task Delete_LastItem_ShowsEmptyList()
    {
        Service.Files = [Service.Files[0]];
        await AppearAsync();
        var item = ViewModel.Items[0];
        item.DeleteCommand.Execute(null);

        await PressAsync(item.ConfirmDeleteCommand);

        Assert.True(ViewModel.IsEmpty);
    }

    /// <summary>
    /// Prüft, dass eine bereits fehlende Datei gemeldet wird und die Liste trotzdem aktualisiert wird.
    /// </summary>
    [Fact]
    public async Task Delete_NotFound_ReloadsList()
    {
        await AppearAsync();
        Service.DeleteResult = BackupResult.NotFound;
        var item = ViewModel.Items[0];
        item.DeleteCommand.Execute(null);

        await PressAsync(item.ConfirmDeleteCommand);

        Assert.Equal(BackupTexts.NotFound, ViewModel.StatusMessage);
        Assert.Single(ViewModel.Items);
    }

    /// <summary>
    /// Prüft, dass ein Fehler beim Löschen gemeldet wird und die Datei in der Liste bleibt.
    /// </summary>
    [Fact]
    public async Task Delete_Failed_KeepsListEntry()
    {
        await AppearAsync();
        Service.DeleteResult = BackupResult.Failed;
        var item = ViewModel.Items[0];
        item.DeleteCommand.Execute(null);

        await PressAsync(item.ConfirmDeleteCommand);

        Assert.Equal(BackupTexts.Failed, ViewModel.StatusMessage);
        Assert.Equal(2, ViewModel.Items.Count);
    }

    /// <summary>
    /// Prüft, dass eine Ausnahme beim Löschen als allgemeiner Fehler gemeldet wird.
    /// </summary>
    [Fact]
    public async Task Delete_Exception_ShowsFailure()
    {
        await AppearAsync();
        var item = ViewModel.Items[0];
        item.DeleteCommand.Execute(null);
        Service.Exception = new UnauthorizedAccessException();

        await PressAsync(item.ConfirmDeleteCommand);

        Assert.Equal(BackupTexts.Failed, ViewModel.StatusMessage);
        Assert.False(ViewModel.IsBusy);
    }
}

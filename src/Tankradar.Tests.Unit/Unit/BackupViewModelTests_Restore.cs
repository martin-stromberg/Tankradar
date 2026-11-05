using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Wiederherstellung im <see cref="Tankradar.MAUI.ViewModels.BackupViewModel"/>: Rückfrage vor dem Überschreiben, Abbruch, Bestätigung und Meldungen je Ergebnis.
/// </summary>
public class BackupViewModelTests_Restore : BackupViewModelTestBase
{
    /// <summary>
    /// Prüft, dass „Wiederherstellen“ zuerst die Rückfrage mit Datum, Version und Zählwerten zeigt und noch nichts wiederherstellt.
    /// </summary>
    [Fact]
    public async Task Restore_ShowsQuestionWithCountsBeforeAnyChange()
    {
        await AppearAsync();
        var item = ViewModel.Items[0];

        await PressAsync(item.RestoreCommand);

        Assert.True(item.IsConfirmingRestore);
        Assert.Equal(["tankatlas-backup-20261012-131500.zip"], Service.Inspected);
        Assert.Empty(Service.Restored);
        Assert.Contains(BackupTexts.FormatDate(item.Info.CreatedUtc), item.RestoreQuestion, StringComparison.Ordinal);
        Assert.Contains("Version 0.1.19", item.RestoreQuestion, StringComparison.Ordinal);
        Assert.Contains("1 Fahrzeug, 2 Tank- und Ladevorgänge, 3 Favoritengruppen", item.RestoreQuestion, StringComparison.Ordinal);
        Assert.Contains("vollständig ersetzt", item.RestoreQuestion, StringComparison.Ordinal);
        Assert.Equal(item.RestoreQuestion, Announcer.Announcements[^1]);
    }

    /// <summary>
    /// Prüft, dass „Abbrechen“ die Rückfrage schließt und den Dienst nicht zum Wiederherstellen aufruft.
    /// </summary>
    [Fact]
    public async Task Restore_Cancel_DoesNotCallService()
    {
        await AppearAsync();
        var item = ViewModel.Items[0];
        await PressAsync(item.RestoreCommand);

        item.CancelRestoreCommand.Execute(null);

        Assert.False(item.IsConfirmingRestore);
        Assert.Empty(Service.Restored);
    }

    /// <summary>
    /// Prüft, dass die Bestätigung die gewählte Datei wiederherstellt, das Ereignis auslöst und die Meldung zeigt.
    /// </summary>
    [Fact]
    public async Task Restore_Confirm_RestoresSelectedFileAndRaisesRestored()
    {
        await AppearAsync();
        var item = ViewModel.Items[1];
        var raised = 0;
        ViewModel.Restored += (_, _) => raised++;
        await PressAsync(item.RestoreCommand);

        await PressAsync(item.ConfirmRestoreCommand);

        Assert.Equal(["tankatlas-backup-20261011-080000.zip"], Service.Restored);
        Assert.Equal(1, raised);
        Assert.Equal(BackupTexts.Restored, ViewModel.StatusMessage);
        Assert.Contains(BackupTexts.Restored, Announcer.Announcements);
        Assert.False(ViewModel.IsBusy);
    }

    /// <summary>
    /// Prüft, dass bei jedem Ergebnis außer <see cref="BackupResult.Ok"/> das Ereignis nicht ausgelöst wird und die zentrale Meldung erscheint.
    /// </summary>
    /// <param name="result">Das Ergebnis des Dienstes.</param>
    [Theory]
    [InlineData(BackupResult.NotFound)]
    [InlineData(BackupResult.InvalidFile)]
    [InlineData(BackupResult.TooNew)]
    [InlineData(BackupResult.Busy)]
    [InlineData(BackupResult.Failed)]
    public async Task Restore_NotOk_ShowsMessageAndDoesNotRaiseRestored(BackupResult result)
    {
        await AppearAsync();
        var item = ViewModel.Items[0];
        var raised = false;
        ViewModel.Restored += (_, _) => raised = true;
        await PressAsync(item.RestoreCommand);
        Service.RestoreResult = result;

        await PressAsync(item.ConfirmRestoreCommand);

        Assert.False(raised);
        Assert.Equal(BackupTexts.GetMessage(result), ViewModel.StatusMessage);
    }

    /// <summary>
    /// Prüft, dass eine Ausnahme beim Wiederherstellen als allgemeiner Fehler gemeldet wird.
    /// </summary>
    [Fact]
    public async Task Restore_Exception_ShowsFailure()
    {
        await AppearAsync();
        var item = ViewModel.Items[0];
        await PressAsync(item.RestoreCommand);
        Service.Exception = new IOException("gesperrt");

        await PressAsync(item.ConfirmRestoreCommand);

        Assert.Equal(BackupTexts.Failed, ViewModel.StatusMessage);
        Assert.False(ViewModel.IsBusy);
    }

    /// <summary>
    /// Prüft, dass eine zu neue Sicherung schon bei der Prüfung vor der Rückfrage mit der Version gemeldet wird, ohne Rückfrage.
    /// </summary>
    [Fact]
    public async Task Restore_TooNew_ShowsVersionAndNoQuestion()
    {
        await AppearAsync();
        Service.InspectResult = BackupResult.TooNew;
        Service.Manifest.AppVersion = "0.2.0";
        var item = ViewModel.Items[0];

        await PressAsync(item.RestoreCommand);

        Assert.False(item.IsConfirmingRestore);
        Assert.Equal(BackupTexts.FormatTooNew("0.2.0"), ViewModel.StatusMessage);
    }

    /// <summary>
    /// Prüft, dass eine nicht lesbare Datei sofort die Fehlermeldung zeigt, ohne den Dienst zu befragen.
    /// </summary>
    [Fact]
    public async Task Restore_UnreadableFile_ShowsInvalidWithoutService()
    {
        Service.Files = [new BackupFileInfo("tankatlas-backup-x.zip", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 5, string.Empty, false)];
        await AppearAsync();

        await PressAsync(ViewModel.Items[0].RestoreCommand);

        Assert.Empty(Service.Inspected);
        Assert.Equal(BackupTexts.InvalidFile, ViewModel.StatusMessage);
    }

    /// <summary>
    /// Prüft, dass eine nicht gefundene Datei bei der Prüfung gemeldet und die Liste neu geladen wird.
    /// </summary>
    [Fact]
    public async Task Restore_InspectNotFound_ReloadsList()
    {
        await AppearAsync();
        Service.InspectResult = BackupResult.NotFound;
        var loads = Service.ListCount;

        await PressAsync(ViewModel.Items[0].RestoreCommand);

        Assert.Equal(BackupTexts.NotFound, ViewModel.StatusMessage);
        Assert.Equal(loads + 1, Service.ListCount);
    }

    /// <summary>
    /// Prüft, dass nur eine Rückfrage gleichzeitig offen ist (auch zwischen Wiederherstellen und Löschen).
    /// </summary>
    [Fact]
    public async Task Questions_OnlyOneOpenAtATime()
    {
        await AppearAsync();
        var first = ViewModel.Items[0];
        var second = ViewModel.Items[1];

        await PressAsync(first.RestoreCommand);
        await PressAsync(second.RestoreCommand);
        Assert.False(first.IsConfirmingRestore);
        Assert.True(second.IsConfirmingRestore);

        await PressAsync(first.DeleteCommand);
        Assert.True(first.IsConfirmingDelete);
        Assert.False(second.IsConfirmingRestore);
    }
}

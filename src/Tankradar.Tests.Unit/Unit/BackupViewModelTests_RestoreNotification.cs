using Tankradar.MAUI.Models.Backup;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass das <see cref="Tankradar.MAUI.ViewModels.BackupViewModel"/> eine erfolgreiche Wiederherstellung dem <see cref="MAUI.Services.Backup.IRestoreNotifier"/> meldet (H5).
/// </summary>
public class BackupViewModelTests_RestoreNotification : BackupViewModelTestBase
{
    /// <summary>
    /// Prüft, dass eine erfolgreiche Wiederherstellung genau eine Benachrichtigung auslöst.
    /// </summary>
    [Fact]
    public async Task Restore_Success_NotifiesOnce()
    {
        await AppearAsync();
        var item = ViewModel.Items[0];
        await PressAsync(item.RestoreCommand);

        await PressAsync(item.ConfirmRestoreCommand);

        Assert.Equal(1, Restore.Generation);
    }

    /// <summary>
    /// Prüft, dass ein fehlgeschlagener Versuch keine Benachrichtigung auslöst.
    /// </summary>
    [Fact]
    public async Task Restore_Failure_DoesNotNotify()
    {
        await AppearAsync();
        var item = ViewModel.Items[0];
        await PressAsync(item.RestoreCommand);
        Service.RestoreResult = BackupResult.NotFound;

        await PressAsync(item.ConfirmRestoreCommand);

        Assert.Equal(0, Restore.Generation);
    }

    /// <summary>
    /// Prüft, dass das Ereignis <c>Restored</c> des ViewModels weiterhin ausgelöst wird.
    /// </summary>
    [Fact]
    public async Task Restore_Success_StillRaisesViewModelEvent()
    {
        await AppearAsync();
        var item = ViewModel.Items[0];
        var raised = 0;
        ViewModel.Restored += (_, _) => raised++;
        await PressAsync(item.RestoreCommand);

        await PressAsync(item.ConfirmRestoreCommand);

        Assert.Equal(1, raised);
    }
}

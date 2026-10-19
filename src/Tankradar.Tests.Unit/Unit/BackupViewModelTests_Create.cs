using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Erstellen einer Sicherung im <see cref="Tankradar.MAUI.ViewModels.BackupViewModel"/> samt Sperre während des Vorgangs.
/// </summary>
public class BackupViewModelTests_Create : BackupViewModelTestBase
{
    /// <summary>
    /// Prüft, dass das Erstellen die Liste aktualisiert, die Meldung zeigt und ansagt.
    /// </summary>
    [Fact]
    public async Task Create_Success_ReloadsListAndAnnounces()
    {
        await AppearAsync();

        await PressAsync(ViewModel.CreateCommand);

        Assert.Equal(3, ViewModel.Items.Count);
        Assert.Equal(BackupTexts.Created, ViewModel.StatusMessage);
        Assert.Equal(BackupTexts.Created, Announcer.Announcements[^1]);
        Assert.False(ViewModel.IsBusy);
        Assert.Equal(1, Service.CreateCount);
    }

    /// <summary>
    /// Prüft, dass während des Erstellens alle Schaltflächen gesperrt sind und danach wieder frei werden.
    /// </summary>
    [Fact]
    public async Task Create_WhileRunning_LocksAllCommands()
    {
        await AppearAsync();
        Service.CreateGate = new TaskCompletionSource();

        ViewModel.CreateCommand.Execute(null);

        Assert.True(ViewModel.IsBusy);
        Assert.False(ViewModel.CreateCommand.CanExecute(null));
        Assert.False(ViewModel.ImportCommand.CanExecute(null));
        Assert.All(ViewModel.Items, item =>
        {
            Assert.False(item.RestoreCommand.CanExecute(null));
            Assert.False(item.ShareCommand.CanExecute(null));
            Assert.False(item.DeleteCommand.CanExecute(null));
        });

        ViewModel.CreateCommand.Execute(null);
        Assert.Equal(1, Service.CreateCount);

        Service.CreateGate.SetResult();
        await ViewModel.LastTask;

        Assert.False(ViewModel.IsBusy);
        Assert.True(ViewModel.CreateCommand.CanExecute(null));
        Assert.All(ViewModel.Items, item => Assert.True(item.RestoreCommand.CanExecute(null)));
    }

    /// <summary>
    /// Prüft die Meldungen bei <see cref="BackupResult.Busy"/> und <see cref="BackupResult.Failed"/>.
    /// </summary>
    /// <param name="result">Das Ergebnis des Dienstes.</param>
    /// <param name="expected">Die erwartete Meldung.</param>
    [Theory]
    [InlineData(BackupResult.Busy, BackupTexts.Busy)]
    [InlineData(BackupResult.Failed, BackupTexts.CreateFailed)]
    [InlineData(BackupResult.InvalidFile, BackupTexts.CreateFailed)]
    public async Task Create_NotOk_ShowsMessage(BackupResult result, string expected)
    {
        await AppearAsync();
        Service.CreateResult = result;

        await PressAsync(ViewModel.CreateCommand);

        Assert.Equal(expected, ViewModel.StatusMessage);
        Assert.Equal(2, ViewModel.Items.Count);
        Assert.False(ViewModel.IsBusy);
    }

    /// <summary>
    /// Prüft, dass eine Ausnahme des Dienstes gemeldet wird und die Sperre aufgehoben wird.
    /// </summary>
    [Fact]
    public async Task Create_Exception_ShowsFailureAndUnlocks()
    {
        await AppearAsync();
        Service.Exception = new IOException("Platte voll");

        await PressAsync(ViewModel.CreateCommand);

        Assert.Equal(BackupTexts.CreateFailed, ViewModel.StatusMessage);
        Assert.False(ViewModel.IsBusy);
    }
}

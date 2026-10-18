using Tankradar.MAUI.Models;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Einbettung der Karte „Sicherung“ in das <see cref="SettingsViewModel"/>.
/// </summary>
public class SettingsViewModelTests_Backup : BaseTest
{
    private readonly FakeSettingsService _settings = new();
    private readonly FakeBackupService _service = new();

    private BackupViewModel CreateBackup()
    {
        return new BackupViewModel(_service, new FakeBackupFilePicker(), new FakeBackupSharer(), new FakeScreenReaderAnnouncer(), new ListLogger<BackupViewModel>());
    }

    /// <summary>
    /// Prüft, dass ohne Sicherungsdienst die Karte nicht angezeigt wird.
    /// </summary>
    [Fact]
    public void WithoutBackup_HasBackupIsFalse()
    {
        var viewModel = new SettingsViewModel(_settings, new ListLogger<SettingsViewModel>());

        Assert.Null(viewModel.Backup);
        Assert.False(viewModel.HasBackup);
    }

    /// <summary>
    /// Prüft, dass das Erscheinen der Seite die Liste der Sicherungen lädt.
    /// </summary>
    [Fact]
    public async Task OnAppearing_LoadsBackupList()
    {
        var backup = CreateBackup();
        var viewModel = new SettingsViewModel(_settings, new ListLogger<SettingsViewModel>(), null, backup);

        viewModel.OnAppearing();
        await backup.LastTask;

        Assert.True(viewModel.HasBackup);
        Assert.Same(backup, viewModel.Backup);
        Assert.Equal(1, _service.ListCount);
    }

    /// <summary>
    /// Prüft, dass nach einer Wiederherstellung die Einstellungen neu geladen und angezeigt werden.
    /// </summary>
    [Fact]
    public async Task Restored_ReloadsSettings()
    {
        _service.Files = [new MAUI.Models.Backup.BackupFileInfo("tankatlas-backup-1.zip", DateTime.UtcNow, 10, "0.1.19", true)];
        var backup = CreateBackup();
        var viewModel = new SettingsViewModel(_settings, new ListLogger<SettingsViewModel>(), null, backup);
        await viewModel.LoadAsync();
        var loads = _settings.LoadCount;
        backup.OnAppearing();
        await backup.LastTask;
        _settings.Stored = _settings.Stored with { ShowNearby = true };
        var item = backup.Items[0];
        item.RestoreCommand.Execute(null);
        await backup.LastTask;

        item.ConfirmRestoreCommand.Execute(null);
        await backup.LastTask;
        await WaitForAsync(() => _settings.LoadCount > loads && viewModel.ShowNearby);

        Assert.True(viewModel.ShowNearby);
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 200 && !condition(); attempt++)
        {
            await Task.Delay(10);
        }
    }
}

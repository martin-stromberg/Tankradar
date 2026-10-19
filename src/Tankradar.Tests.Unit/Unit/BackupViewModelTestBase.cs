using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Services.Backup;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Gemeinsamer Aufbau der Tests des <see cref="BackupViewModel"/> mit ersetztem Sicherungsdienst, ersetzter Dateiauswahl und Weitergabe.
/// </summary>
public abstract class BackupViewModelTestBase : BaseTest
{
    /// <summary>
    /// Erstellt den Aufbau mit zwei Sicherungen (neueste zuerst).
    /// </summary>
    protected BackupViewModelTestBase()
    {
        Service.Files =
        [
            new BackupFileInfo("tankatlas-backup-20261012-131500.zip", new DateTime(2026, 10, 12, 13, 15, 0, DateTimeKind.Utc), 1_258_291, "0.1.19", true),
            new BackupFileInfo("tankatlas-backup-20261011-080000.zip", new DateTime(2026, 10, 11, 8, 0, 0, DateTimeKind.Utc), 900, "0.1.18", true),
        ];
        ViewModel = new BackupViewModel(Service, Picker, Sharer, Restore, Announcer, new ListLogger<BackupViewModel>());
    }

    /// <summary>
    /// Der ersetzte Sicherungsdienst.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeBackupService Service { get; } = new();

    /// <summary>
    /// Die ersetzte Dateiauswahl.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeBackupFilePicker Picker { get; } = new();

    /// <summary>
    /// Die ersetzte Weitergabe.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeBackupSharer Sharer { get; } = new();

    /// <summary>
    /// Die Benachrichtigung nach einer Wiederherstellung.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected RestoreNotifier Restore { get; } = new();

    /// <summary>
    /// Die Sprachausgabe.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeScreenReaderAnnouncer Announcer { get; } = new();

    /// <summary>
    /// Das zu testende ViewModel.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected BackupViewModel ViewModel { get; }

    /// <summary>
    /// Lädt die Liste, wie es beim Erscheinen der Seite geschieht.
    /// </summary>
    /// <returns>Ein Task, der nach dem Laden abgeschlossen ist.</returns>
    protected async Task AppearAsync()
    {
        ViewModel.OnAppearing();
        await ViewModel.LastTask;
    }

    /// <summary>
    /// Führt einen Befehl aus und wartet auf den dadurch gestarteten Vorgang.
    /// </summary>
    /// <param name="command">Der Befehl.</param>
    /// <returns>Ein Task, der nach dem Vorgang abgeschlossen ist.</returns>
    protected async Task PressAsync(System.Windows.Input.ICommand command)
    {
        Assert.True(command.CanExecute(null));
        command.Execute(null);
        await ViewModel.LastTask;
    }
}

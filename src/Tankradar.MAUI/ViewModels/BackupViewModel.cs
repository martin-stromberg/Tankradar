using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services;
using Tankradar.MAUI.Services.Backup;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// ViewModel der Karte „Sicherung“ in den Optionen: Sicherung erstellen, Liste, Wiederherstellen und Löschen nach Rückfrage im Eintrag, Teilen und Einspielen einer Datei.
/// Während eines Vorgangs sind alle Schaltflächen gesperrt.
/// </summary>
public class BackupViewModel : BaseViewModel
{
    private readonly IBackupService _service;
    private readonly IBackupFilePicker _picker;
    private readonly IBackupSharer _sharer;
    private readonly IRestoreNotifier _restoreNotifier;
    private readonly IScreenReaderAnnouncer _announcer;
    private readonly ILogger<BackupViewModel> _logger;
    private string? _statusMessage;
    private string _createPassword = string.Empty;
    private string _createPasswordRepeat = string.Empty;
    private bool _isAwaitingImportPassword;
    private string _importPassword = string.Empty;
    private string _importPasswordText = string.Empty;
    private PickedBackupFile? _pendingImport;

    /// <summary>
    /// Erstellt das ViewModel.
    /// </summary>
    /// <param name="service">Der Sicherungsdienst.</param>
    /// <param name="picker">Die Dateiauswahl für das Einspielen.</param>
    /// <param name="sharer">Die Weitergabe über die Systemfunktion.</param>
    /// <param name="restoreNotifier">Meldet geöffneten Seiten, dass die Daten wiederhergestellt wurden.</param>
    /// <param name="announcer">Die Sprachausgabe für Meldungen.</param>
    /// <param name="logger">Logger.</param>
    public BackupViewModel(
        IBackupService service,
        IBackupFilePicker picker,
        IBackupSharer sharer,
        IRestoreNotifier restoreNotifier,
        IScreenReaderAnnouncer announcer,
        ILogger<BackupViewModel> logger)
    {
        _service = service;
        _picker = picker;
        _sharer = sharer;
        _restoreNotifier = restoreNotifier;
        _announcer = announcer;
        _logger = logger;
        CreateCommand = new Command(() => LastTask = CreateAsync(), () => IsNotBusy);
        ImportCommand = new Command(() => LastTask = ImportAsync(), () => IsNotBusy);
        ConfirmImportPasswordCommand = new Command(() => LastTask = ConfirmImportPasswordAsync(), () => IsNotBusy);
        CancelImportPasswordCommand = new Command(CancelImportPassword, () => IsNotBusy);
        LastTask = Task.CompletedTask;
    }

    /// <summary>
    /// Die Sicherungen, neueste zuerst.
    /// </summary>
    public ObservableCollection<BackupItemViewModel> Items { get; } = [];

    /// <summary>
    /// Gibt an, ob es Sicherungen gibt.
    /// </summary>
    public bool HasItems
    {
        get { return Items.Count > 0; }
    }

    /// <summary>
    /// Gibt an, ob die Liste leer ist (Hinweis „Es gibt noch keine Sicherung“).
    /// </summary>
    public bool IsEmpty
    {
        get { return Items.Count == 0; }
    }

    /// <summary>
    /// Die zuletzt gemeldete Mitteilung; <see langword="null"/>, wenn keine anliegt.
    /// </summary>
    public string? StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob eine <see cref="StatusMessage"/> anliegt.
    /// </summary>
    public bool HasStatusMessage
    {
        get { return StatusMessage is { Length: > 0 }; }
    }

    /// <summary>
    /// Überschrift der Karte.
    /// </summary>
    public string Heading
    {
        get { return BackupTexts.Heading; }
    }

    /// <summary>
    /// Hinweis zu Umfang und Ablage.
    /// </summary>
    public string Hint
    {
        get { return BackupTexts.Hint; }
    }

    /// <summary>
    /// Beschriftung der Schaltfläche „Sicherung erstellen“.
    /// </summary>
    public string CreateText
    {
        get { return BackupTexts.CreateButton; }
    }

    /// <summary>
    /// Beschriftung der Schaltfläche zum Einspielen einer Datei.
    /// </summary>
    public string ImportText
    {
        get { return BackupTexts.ImportButton; }
    }

    /// <summary>
    /// Hinweis bei leerer Liste.
    /// </summary>
    public string EmptyText
    {
        get { return BackupTexts.Empty; }
    }

    /// <summary>
    /// Platzhalter des Passwortfelds.
    /// </summary>
    public string PasswordPlaceholderText
    {
        get { return BackupTexts.PasswordPlaceholder; }
    }

    /// <summary>
    /// Platzhalter des Wiederholungsfelds.
    /// </summary>
    public string PasswordRepeatPlaceholderText
    {
        get { return BackupTexts.PasswordRepeatPlaceholder; }
    }

    /// <summary>
    /// Hinweis zu den Passwortfeldern beim Erstellen.
    /// </summary>
    public string PasswordHintText
    {
        get { return BackupTexts.PasswordHint; }
    }

    /// <summary>
    /// Beschriftung der Bestätigung der Passwortabfrage beim Einspielen.
    /// </summary>
    public string ImportUnlockText
    {
        get { return BackupTexts.UnlockButton; }
    }

    /// <summary>
    /// Beschriftung von „Abbrechen“ der Passwortabfrage beim Einspielen.
    /// </summary>
    public string ImportCancelText
    {
        get { return BackupTexts.Cancel; }
    }

    /// <summary>
    /// Das Passwort der neuen Sicherung; leer erzeugt eine unverschlüsselte Datei (nie gespeichert, wird nach dem Vorgang geleert).
    /// </summary>
    public string CreatePassword
    {
        get => _createPassword;
        set => SetProperty(ref _createPassword, value ?? string.Empty);
    }

    /// <summary>
    /// Die Wiederholung des Passworts der neuen Sicherung (Abgleich vor dem Erstellen).
    /// </summary>
    public string CreatePasswordRepeat
    {
        get => _createPasswordRepeat;
        set => SetProperty(ref _createPasswordRepeat, value ?? string.Empty);
    }

    /// <summary>
    /// Gibt an, ob die Passwortabfrage für eine verschlüsselte Datei beim Einspielen angezeigt wird.
    /// </summary>
    public bool IsAwaitingImportPassword
    {
        get => _isAwaitingImportPassword;
        private set => SetProperty(ref _isAwaitingImportPassword, value);
    }

    /// <summary>
    /// Das Passwort der einzuspielenden Datei (nie gespeichert; wird nach dem Vorgang geleert).
    /// </summary>
    public string ImportPassword
    {
        get => _importPassword;
        set => SetProperty(ref _importPassword, value ?? string.Empty);
    }

    /// <summary>
    /// Der Text der Passwortabfrage beim Einspielen.
    /// </summary>
    public string ImportPasswordText
    {
        get => _importPasswordText;
        private set => SetProperty(ref _importPasswordText, value);
    }

    /// <summary>
    /// Befehl „Sicherung erstellen“.
    /// </summary>
    public ICommand CreateCommand { get; }

    /// <summary>
    /// Befehl „Sicherung aus Datei einspielen“.
    /// </summary>
    public ICommand ImportCommand { get; }

    /// <summary>
    /// Befehl „Prüfen“ der Passwortabfrage beim Einspielen.
    /// </summary>
    public ICommand ConfirmImportPasswordCommand { get; }

    /// <summary>
    /// Befehl „Abbrechen“ der Passwortabfrage beim Einspielen.
    /// </summary>
    public ICommand CancelImportPasswordCommand { get; }

    /// <summary>
    /// Der zuletzt gestartete Vorgang (für Tests und Abwarten).
    /// </summary>
    public Task LastTask { get; private set; }

    /// <inheritdoc />
    public override void OnAppearing()
    {
        LastTask = LoadAsync();
    }

    /// <summary>
    /// Lädt die Liste der Sicherungen.
    /// </summary>
    /// <returns>Ein Task, der nach dem Laden abgeschlossen ist.</returns>
    public async Task LoadAsync()
    {
        try
        {
            var list = await _service.ListAsync().ConfigureAwait(true);
            ApplyList(list);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Die Sicherungen konnten nicht gelesen werden ({ExceptionType}).", ex.GetType().Name);
            SetStatus(BackupTexts.ListFailed);
        }
    }

    /// <summary>
    /// Erstellt eine Sicherung und aktualisiert die Liste; bei gefüllten Passwortfeldern mit Verschlüsselung.
    /// </summary>
    /// <returns>Ein Task, der nach dem Vorgang abgeschlossen ist.</returns>
    public async Task CreateAsync()
    {
        var password = CreatePassword;
        var repeat = CreatePasswordRepeat;
        if (!TryBeginOperation())
        {
            return;
        }

        try
        {
            if (password != repeat)
            {
                SetStatus(BackupTexts.PasswordsDoNotMatch);
                return;
            }

            var outcome = await _service.CreateAsync(string.IsNullOrEmpty(password) ? null : password).ConfigureAwait(true);
            if (outcome.Result == BackupResult.Ok)
            {
                await LoadAsync().ConfigureAwait(true);
                SetStatus(BackupTexts.Created);
            }
            else
            {
                SetStatus(outcome.Result == BackupResult.Busy ? BackupTexts.Busy : BackupTexts.CreateFailed);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Die Sicherung konnte nicht erstellt werden ({ExceptionType}).", ex.GetType().Name);
            SetStatus(BackupTexts.CreateFailed);
        }
        finally
        {
            // Die Passwörter bleiben nicht in der Oberfläche liegen, gleich wie der Vorgang ausgegangen ist.
            CreatePassword = string.Empty;
            CreatePasswordRepeat = string.Empty;
            EndOperation();
        }
    }

    /// <summary>
    /// Zeigt für einen Eintrag die Rückfrage zur Wiederherstellung mit den Zählwerten der Sicherung; bei einer nicht nutzbaren Datei stattdessen die Fehlermeldung.
    /// </summary>
    /// <param name="item">Der Eintrag.</param>
    /// <returns>Ein Task, der nach dem Lesen der Sicherung abgeschlossen ist.</returns>
    public async Task BeginRestoreAsync(BackupItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!TryBeginOperation())
        {
            return;
        }

        try
        {
            await ShowRestoreQuestionAsync(item).ConfigureAwait(true);
        }
        finally
        {
            EndOperation();
        }
    }

    /// <summary>
    /// Prüft das Passwort einer verschlüsselten Sicherung und zeigt bei Erfolg die Rückfrage mit den Zählwerten.
    /// </summary>
    /// <param name="item">Der Eintrag.</param>
    /// <returns>Ein Task, der nach der Prüfung abgeschlossen ist.</returns>
    public async Task UnlockRestoreAsync(BackupItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);

        // Das Passwort wird vor Beginn des Vorgangs gesichert: TryBeginOperation schließt die Rückfrage und leert das Feld.
        var password = item.RestorePassword;
        if (!TryBeginOperation())
        {
            return;
        }

        try
        {
            var outcome = await _service.InspectAsync(item.FileName, password).ConfigureAwait(true);
            if (outcome is { Result: BackupResult.Ok, Manifest: { } manifest })
            {
                item.ShowRestoreQuestion(BackupTexts.FormatRestoreQuestion(item.Info.CreatedUtc, manifest.AppVersion, manifest.Counts));
                item.RestorePassword = password;
                _announcer.Announce(item.RestoreQuestion);
                return;
            }

            SetStatus(BackupTexts.GetMessage(outcome.Result, outcome.Manifest?.AppVersion));
            if (outcome.Result is BackupResult.WrongPassword or BackupResult.PasswordRequired)
            {
                // Die Passwortabfrage bleibt geöffnet, damit die Eingabe direkt erneut versucht werden kann.
                item.ShowPasswordQuestion(BackupTexts.PasswordQuestion);
                _announcer.Announce(item.RestoreQuestion);
            }
            else if (outcome.Result == BackupResult.NotFound)
            {
                await LoadAsync().ConfigureAwait(true);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Die Sicherung konnte nicht geprüft werden ({ExceptionType}).", ex.GetType().Name);
            SetStatus(BackupTexts.Failed);
        }
        finally
        {
            EndOperation();
        }
    }

    /// <summary>
    /// Stellt die Daten nach der Bestätigung aus einem Eintrag wieder her.
    /// </summary>
    /// <param name="item">Der Eintrag.</param>
    /// <returns>Ein Task, der nach dem Vorgang abgeschlossen ist.</returns>
    public async Task ConfirmRestoreAsync(BackupItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);

        // Das Passwort wird vor Beginn des Vorgangs gesichert: TryBeginOperation schließt die Rückfrage und leert das Feld.
        var password = item.RestorePassword;
        if (!TryBeginOperation())
        {
            return;
        }

        try
        {
            var result = await _service.RestoreAsync(item.FileName, string.IsNullOrEmpty(password) ? null : password).ConfigureAwait(true);
            if (result == BackupResult.Ok)
            {
                SetStatus(BackupTexts.Restored);
                await LoadAsync().ConfigureAwait(true);
                _restoreNotifier.NotifyRestored();
                return;
            }

            SetStatus(BackupTexts.GetMessage(result));
            if (result == BackupResult.NotFound)
            {
                await LoadAsync().ConfigureAwait(true);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Die Wiederherstellung ist fehlgeschlagen ({ExceptionType}).", ex.GetType().Name);
            SetStatus(BackupTexts.Failed);
        }
        finally
        {
            EndOperation();
        }
    }

    /// <summary>
    /// Zeigt für einen Eintrag die Rückfrage zum Löschen.
    /// </summary>
    /// <param name="item">Der Eintrag.</param>
    public void BeginDelete(BackupItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (IsBusy)
        {
            return;
        }

        CloseQuestions();
        StatusMessage = null;
        item.ShowDeleteQuestion();
        _announcer.Announce(item.DeleteQuestion);
    }

    /// <summary>
    /// Löscht eine Sicherung nach der Bestätigung und aktualisiert die Liste.
    /// </summary>
    /// <param name="item">Der Eintrag.</param>
    /// <returns>Ein Task, der nach dem Vorgang abgeschlossen ist.</returns>
    public async Task ConfirmDeleteAsync(BackupItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!TryBeginOperation())
        {
            return;
        }

        try
        {
            var result = await _service.DeleteAsync(item.FileName).ConfigureAwait(true);
            SetStatus(result == BackupResult.Ok ? BackupTexts.Deleted : BackupTexts.GetMessage(result));
            if (result is BackupResult.Ok or BackupResult.NotFound)
            {
                await LoadAsync().ConfigureAwait(true);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Die Sicherung konnte nicht gelöscht werden ({ExceptionType}).", ex.GetType().Name);
            SetStatus(BackupTexts.Failed);
        }
        finally
        {
            EndOperation();
        }
    }

    /// <summary>
    /// Gibt eine Sicherung über die Systemfunktion „Teilen/Speichern“ weiter.
    /// </summary>
    /// <param name="item">Der Eintrag.</param>
    /// <returns>Ein Task, der nach der Weitergabe abgeschlossen ist.</returns>
    public async Task ShareAsync(BackupItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!TryBeginOperation())
        {
            return;
        }

        try
        {
            var path = _service.GetFullPath(item.FileName);
            if (path is null)
            {
                SetStatus(BackupTexts.NotFound);
                await LoadAsync().ConfigureAwait(true);
                return;
            }

            await _sharer.ShareAsync(path, BackupTexts.Heading).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Die Sicherung konnte nicht weitergegeben werden ({ExceptionType}).", ex.GetType().Name);
            SetStatus(BackupTexts.ShareFailed);
        }
        finally
        {
            EndOperation();
        }
    }

    /// <summary>
    /// Lässt eine externe Datei auswählen, prüft sie, nimmt sie in die Liste auf und zeigt die Rückfrage zur Wiederherstellung.
    /// </summary>
    /// <returns>Ein Task, der nach dem Vorgang abgeschlossen ist.</returns>
    public async Task ImportAsync()
    {
        if (!TryBeginOperation())
        {
            return;
        }

        try
        {
            PickedBackupFile? picked;
            try
            {
                picked = await _picker.PickAsync().ConfigureAwait(true);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning("Die Dateiauswahl ist fehlgeschlagen ({ExceptionType}).", ex.GetType().Name);
                SetStatus(BackupTexts.PickFailed);
                return;
            }

            if (picked is null)
            {
                return;
            }

            await ImportFileAsync(picked, null).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Die Sicherung konnte nicht eingespielt werden ({ExceptionType}).", ex.GetType().Name);
            SetStatus(BackupTexts.Failed);
        }
        finally
        {
            EndOperation();
        }
    }

    private async Task ConfirmImportPasswordAsync()
    {
        var file = _pendingImport;
        var password = ImportPassword;
        if (file is null || !TryBeginOperation())
        {
            return;
        }

        try
        {
            if (string.IsNullOrEmpty(password))
            {
                // Die vorgemerkte Datei ist verschlüsselt; ohne Eingabe gibt es nichts zu prüfen, die Abfrage bleibt und die Meldung fordert das Passwort an.
                SetStatus(BackupTexts.PasswordRequired);
                return;
            }

            await ImportFileAsync(file, password).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Die Sicherung konnte nicht eingespielt werden ({ExceptionType}).", ex.GetType().Name);
            SetStatus(BackupTexts.Failed);
        }
        finally
        {
            EndOperation();
        }
    }

    private void CancelImportPassword()
    {
        // Die gewählte Datei und das Passwort werden verworfen; in der Liste ändert sich nichts.
        _pendingImport = null;
        ImportPassword = string.Empty;
        IsAwaitingImportPassword = false;
    }

    private async Task ImportFileAsync(PickedBackupFile file, string? password)
    {
        var outcome = await _service.ImportAsync(file, password).ConfigureAwait(true);
        if (outcome.Result is BackupResult.PasswordRequired or BackupResult.WrongPassword)
        {
            // Die Datei bleibt vorgemerkt: die Passwortabfrage fordert die Eingabe an, bei falschem Passwort kommt zusätzlich die Meldung dazu.
            _pendingImport = file;
            ImportPassword = string.Empty;
            ImportPasswordText = BackupTexts.ImportPasswordQuestion;
            IsAwaitingImportPassword = true;
            _announcer.Announce(BackupTexts.ImportPasswordQuestion);
            if (outcome.Result == BackupResult.WrongPassword)
            {
                SetStatus(BackupTexts.WrongPassword);
            }

            return;
        }

        _pendingImport = null;
        ImportPassword = string.Empty;
        IsAwaitingImportPassword = false;
        if (outcome.Result != BackupResult.Ok || outcome.File is null)
        {
            SetStatus(BackupTexts.GetMessage(outcome.Result, outcome.Manifest?.AppVersion));
            return;
        }

        await LoadAsync().ConfigureAwait(true);
        var item = Items.FirstOrDefault(candidate => candidate.FileName == outcome.File.FileName);
        if (item is not null)
        {
            // Das beim Einspielen eingegebene Passwort gilt für die Rückfrage mit; es wird nicht ein zweites Mal abgefragt.
            await ShowRestoreQuestionAsync(item, password).ConfigureAwait(true);
            if (item.IsConfirmingRestore)
            {
                StatusMessage = BackupTexts.Imported;
            }
        }
    }

    private async Task ShowRestoreQuestionAsync(BackupItemViewModel item, string? password = null)
    {
        CloseQuestions();
        StatusMessage = null;
        if (!item.Info.IsReadable)
        {
            SetStatus(BackupTexts.InvalidFile);
            return;
        }

        if (item.Info.IsEncrypted && string.IsNullOrEmpty(password))
        {
            // Verschlüsselte Dateien fragen zuerst das Passwort ab; die Zählwerte folgen erst nach dem Entschlüsseln.
            item.ShowPasswordQuestion(BackupTexts.PasswordQuestion);
            _announcer.Announce(item.RestoreQuestion);
            return;
        }

        try
        {
            var outcome = await _service.InspectAsync(item.FileName, password).ConfigureAwait(true);
            if (outcome is { Result: BackupResult.Ok, Manifest: { } manifest })
            {
                item.ShowRestoreQuestion(BackupTexts.FormatRestoreQuestion(item.Info.CreatedUtc, manifest.AppVersion, manifest.Counts));
                if (!string.IsNullOrEmpty(password))
                {
                    item.RestorePassword = password;
                }

                _announcer.Announce(item.RestoreQuestion);
                return;
            }

            SetStatus(BackupTexts.GetMessage(outcome.Result, outcome.Manifest?.AppVersion));
            if (outcome.Result == BackupResult.NotFound)
            {
                await LoadAsync().ConfigureAwait(true);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Die Sicherung konnte nicht geprüft werden ({ExceptionType}).", ex.GetType().Name);
            SetStatus(BackupTexts.Failed);
        }
    }

    private bool TryBeginOperation()
    {
        if (IsBusy)
        {
            return false;
        }

        IsBusy = true;
        CloseQuestions();
        StatusMessage = null;
        RefreshCommandStates();
        return true;
    }

    private void EndOperation()
    {
        IsBusy = false;
        RefreshCommandStates();
    }

    private void CloseQuestions()
    {
        foreach (var item in Items)
        {
            item.HideRestoreQuestion();
            item.HideDeleteQuestion();
        }
    }

    private void RefreshCommandStates()
    {
        ((Command)CreateCommand).ChangeCanExecute();
        ((Command)ImportCommand).ChangeCanExecute();
        ((Command)ConfirmImportPasswordCommand).ChangeCanExecute();
        ((Command)CancelImportPasswordCommand).ChangeCanExecute();
        foreach (var item in Items)
        {
            item.RefreshCommandStates();
        }
    }

    private void ApplyList(IReadOnlyList<BackupFileInfo> list)
    {
        Items.Clear();
        for (var index = 0; index < list.Count; index++)
        {
            Items.Add(new BackupItemViewModel(
                list[index],
                index + 1,
                item => LastTask = BeginRestoreAsync(item),
                item => LastTask = UnlockRestoreAsync(item),
                item => LastTask = ConfirmRestoreAsync(item),
                item => LastTask = ShareAsync(item),
                BeginDelete,
                item => LastTask = ConfirmDeleteAsync(item),
                () => IsNotBusy));
        }

        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private void SetStatus(string message)
    {
        StatusMessage = message;
        _announcer.Announce(message);
    }
}

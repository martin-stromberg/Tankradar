using System.Globalization;
using System.Windows.Input;
using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Eine Sicherung in der Liste der Optionen mit Datum, Größe, den Schaltflächen „Wiederherstellen“, „Teilen“ und „Löschen“ und den Rückfragen im Eintrag.
/// </summary>
public sealed class BackupItemViewModel : BaseViewModel
{
    private bool _isConfirmingRestore;
    private string _restoreQuestion = string.Empty;
    private bool _isPasswordStage;
    private string _restorePassword = string.Empty;
    private bool _isConfirmingDelete;
    private string _deleteQuestion = string.Empty;

    /// <summary>
    /// Erstellt den Eintrag.
    /// </summary>
    /// <param name="info">Die Sicherung.</param>
    /// <param name="index">Die 1-basierte Position in der Liste (neueste zuerst); Grundlage der AutomationIds.</param>
    /// <param name="restore">Startet die Wiederherstellung (zeigt die Rückfrage).</param>
    /// <param name="unlockRestore">Prüft das eingegebene Passwort einer verschlüsselten Sicherung (zeigt danach die Zählwerte).</param>
    /// <param name="confirmRestore">Stellt nach der Bestätigung wieder her.</param>
    /// <param name="share">Gibt die Sicherung weiter.</param>
    /// <param name="delete">Startet das Löschen (zeigt die Rückfrage).</param>
    /// <param name="confirmDelete">Löscht nach der Bestätigung.</param>
    /// <param name="canExecute">Gibt an, ob die Befehle gerade ausführbar sind (gesperrt, solange ein Vorgang läuft).</param>
    public BackupItemViewModel(
        BackupFileInfo info,
        int index,
        Action<BackupItemViewModel> restore,
        Action<BackupItemViewModel> unlockRestore,
        Action<BackupItemViewModel> confirmRestore,
        Action<BackupItemViewModel> share,
        Action<BackupItemViewModel> delete,
        Action<BackupItemViewModel> confirmDelete,
        Func<bool> canExecute)
    {
        ArgumentNullException.ThrowIfNull(info);
        Info = info;
        Index = index;
        AutomationKey = index.ToString(CultureInfo.InvariantCulture);
        Title = BackupTexts.FormatItemTitle(info);
        SizeText = BackupTexts.FormatSize(info.SizeBytes);
        RestoreHint = BackupTexts.FormatRestoreHint(info.CreatedUtc);
        ShareHint = BackupTexts.FormatShareHint(info.CreatedUtc);
        DeleteHint = BackupTexts.FormatDeleteHint(info.CreatedUtc);
        RestoreCommand = new Command(() => restore(this), canExecute);
        UnlockRestoreCommand = new Command(() => unlockRestore(this), canExecute);
        ConfirmRestoreCommand = new Command(() => confirmRestore(this), canExecute);
        ShareCommand = new Command(() => share(this), canExecute);
        DeleteCommand = new Command(() => delete(this), canExecute);
        ConfirmDeleteCommand = new Command(() => confirmDelete(this), canExecute);
        CancelRestoreCommand = new Command(HideRestoreQuestion);
        CancelDeleteCommand = new Command(HideDeleteQuestion);
    }

    /// <summary>
    /// Die Sicherung.
    /// </summary>
    public BackupFileInfo Info { get; }

    /// <summary>
    /// Der Dateiname der Sicherung (nur intern, wird nie angezeigt).
    /// </summary>
    public string FileName
    {
        get { return Info.FileName; }
    }

    /// <summary>
    /// Die 1-basierte Position in der Liste (neueste zuerst).
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Schlüssel für die AutomationIds (die 1-basierte Position als Text).
    /// </summary>
    public string AutomationKey { get; }

    /// <summary>
    /// Die Dateigröße, z. B. „1,2 MB“.
    /// </summary>
    public string SizeText { get; }

    /// <summary>
    /// Hinweis der Schaltfläche „Wiederherstellen“ für die Sprachausgabe; nennt das Datum.
    /// </summary>
    public string RestoreHint { get; }

    /// <summary>
    /// Hinweis der Schaltfläche „Teilen“ für die Sprachausgabe; nennt das Datum.
    /// </summary>
    public string ShareHint { get; }

    /// <summary>
    /// Hinweis der Schaltfläche „Löschen“ für die Sprachausgabe; nennt das Datum.
    /// </summary>
    public string DeleteHint { get; }

    /// <summary>
    /// Gibt an, ob die Rückfrage zur Wiederherstellung angezeigt wird.
    /// </summary>
    public bool IsConfirmingRestore
    {
        get => _isConfirmingRestore;
        private set
        {
            if (SetProperty(ref _isConfirmingRestore, value))
            {
                OnPropertyChanged(nameof(IsCountStage));
            }
        }
    }

    /// <summary>
    /// Die Rückfrage zur Wiederherstellung mit Datum, Version und Zählwerten; im Passwort-Stadium die Passwortabfrage.
    /// </summary>
    public string RestoreQuestion
    {
        get => _restoreQuestion;
        private set => SetProperty(ref _restoreQuestion, value);
    }

    /// <summary>
    /// Gibt an, ob die Rückfrage zur Wiederherstellung gerade das Passwort einer verschlüsselten Sicherung abfragt.
    /// </summary>
    public bool IsPasswordStage
    {
        get => _isPasswordStage;
        private set
        {
            if (SetProperty(ref _isPasswordStage, value))
            {
                OnPropertyChanged(nameof(IsCountStage));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob die Rückfrage zur Wiederherstellung gerade die Zählwerte und die Bestätigungsschaltfläche zeigt.
    /// </summary>
    public bool IsCountStage
    {
        get { return IsConfirmingRestore && !IsPasswordStage; }
    }

    /// <summary>
    /// Das Passwort zur verschlüsselten Sicherung (nie gespeichert; nach der Passwortprüfung für die Bestätigung vorgemerkt).
    /// </summary>
    public string RestorePassword
    {
        get => _restorePassword;
        set => SetProperty(ref _restorePassword, value ?? string.Empty);
    }

    /// <summary>
    /// Gibt an, ob die Rückfrage zum Löschen angezeigt wird.
    /// </summary>
    public bool IsConfirmingDelete
    {
        get => _isConfirmingDelete;
        private set => SetProperty(ref _isConfirmingDelete, value);
    }

    /// <summary>
    /// Die Rückfrage zum Löschen.
    /// </summary>
    public string DeleteQuestion
    {
        get => _deleteQuestion;
        private set => SetProperty(ref _deleteQuestion, value);
    }

    /// <summary>
    /// Beschriftung der Schaltfläche „Wiederherstellen“.
    /// </summary>
    public string RestoreText
    {
        get { return BackupTexts.RestoreButton; }
    }

    /// <summary>
    /// Beschriftung der Schaltfläche „Teilen“.
    /// </summary>
    public string ShareText
    {
        get { return BackupTexts.ShareButton; }
    }

    /// <summary>
    /// Beschriftung der Schaltfläche „Löschen“.
    /// </summary>
    public string DeleteText
    {
        get { return BackupTexts.DeleteButton; }
    }

    /// <summary>
    /// Beschriftung der Bestätigung der Wiederherstellung.
    /// </summary>
    public string ConfirmRestoreText
    {
        get { return BackupTexts.ConfirmRestore; }
    }

    /// <summary>
    /// Beschriftung der Bestätigung des Löschens.
    /// </summary>
    public string ConfirmDeleteText
    {
        get { return BackupTexts.ConfirmDelete; }
    }

    /// <summary>
    /// Platzhalter des Passwortfelds im Passwort-Stadium.
    /// </summary>
    public string PasswordPlaceholderText
    {
        get { return BackupTexts.PasswordPlaceholder; }
    }

    /// <summary>
    /// Beschriftung der Schaltfläche, die das Passwort prüft.
    /// </summary>
    public string UnlockText
    {
        get { return BackupTexts.UnlockButton; }
    }

    /// <summary>
    /// Beschriftung von „Abbrechen“ in den Rückfragen.
    /// </summary>
    public string CancelText
    {
        get { return BackupTexts.Cancel; }
    }

    /// <summary>
    /// Befehl „Wiederherstellen“: zeigt die Rückfrage.
    /// </summary>
    public ICommand RestoreCommand { get; }

    /// <summary>
    /// Befehl „Prüfen“ im Passwort-Stadium: prüft das eingegebene Passwort und zeigt danach die Zählwerte.
    /// </summary>
    public ICommand UnlockRestoreCommand { get; }

    /// <summary>
    /// Befehl der Bestätigung der Wiederherstellung.
    /// </summary>
    public ICommand ConfirmRestoreCommand { get; }

    /// <summary>
    /// Befehl „Abbrechen“ der Rückfrage zur Wiederherstellung.
    /// </summary>
    public ICommand CancelRestoreCommand { get; }

    /// <summary>
    /// Befehl „Teilen“.
    /// </summary>
    public ICommand ShareCommand { get; }

    /// <summary>
    /// Befehl „Löschen“: zeigt die Rückfrage.
    /// </summary>
    public ICommand DeleteCommand { get; }

    /// <summary>
    /// Befehl der Bestätigung des Löschens.
    /// </summary>
    public ICommand ConfirmDeleteCommand { get; }

    /// <summary>
    /// Befehl „Abbrechen“ der Rückfrage zum Löschen.
    /// </summary>
    public ICommand CancelDeleteCommand { get; }

    /// <summary>
    /// Zeigt die Rückfrage zur Wiederherstellung mit den Zählwerten.
    /// </summary>
    /// <param name="question">Der Text der Rückfrage.</param>
    public void ShowRestoreQuestion(string question)
    {
        RestoreQuestion = question;
        IsConfirmingDelete = false;
        IsPasswordStage = false;
        IsConfirmingRestore = true;
    }

    /// <summary>
    /// Zeigt statt der Zählwerte die Passwortabfrage einer verschlüsselten Sicherung.
    /// </summary>
    /// <param name="question">Der Text der Passwortabfrage.</param>
    public void ShowPasswordQuestion(string question)
    {
        RestoreQuestion = question;
        IsConfirmingDelete = false;
        IsPasswordStage = true;
        IsConfirmingRestore = true;
    }

    /// <summary>
    /// Blendet die Rückfrage zur Wiederherstellung aus und verwirft ein eingegebenes Passwort.
    /// </summary>
    public void HideRestoreQuestion()
    {
        IsConfirmingRestore = false;
        IsPasswordStage = false;
        RestorePassword = string.Empty;
    }

    /// <summary>
    /// Zeigt die Rückfrage zum Löschen.
    /// </summary>
    public void ShowDeleteQuestion()
    {
        DeleteQuestion = BackupTexts.FormatDeleteQuestion(Info.CreatedUtc);
        IsConfirmingRestore = false;
        IsConfirmingDelete = true;
    }

    /// <summary>
    /// Blendet die Rückfrage zum Löschen aus.
    /// </summary>
    public void HideDeleteQuestion()
    {
        IsConfirmingDelete = false;
    }

    /// <summary>
    /// Aktualisiert die Ausführbarkeit der Befehle, nachdem sich die Sperre geändert hat.
    /// </summary>
    public void RefreshCommandStates()
    {
        ((Command)RestoreCommand).ChangeCanExecute();
        ((Command)UnlockRestoreCommand).ChangeCanExecute();
        ((Command)ConfirmRestoreCommand).ChangeCanExecute();
        ((Command)ShareCommand).ChangeCanExecute();
        ((Command)DeleteCommand).ChangeCanExecute();
        ((Command)ConfirmDeleteCommand).ChangeCanExecute();
    }
}

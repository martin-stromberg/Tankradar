using System.Windows.Input;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Ein Tank- oder Ladevorgang in der Liste: Tankstelle, Datum, Beträge, Menge, Strecke und Verbrauch; „Bearbeiten“ öffnet das Formular,
/// „Löschen“ zeigt die Rückfrage im Eintrag, „Beleg anzeigen“ blendet das Belegfoto ein. Schaltflächen tragen ihren sichtbaren Text als Namen, der Kontext steht im Hinweis.
/// </summary>
public sealed class TankbookEntryItemViewModel : BaseViewModel
{
    private readonly IReceiptPhotoStore _photoStore;
    private bool _isConfirmingDelete;
    private bool _isReceiptVisible;
    private bool _isReceiptMissing;
    private string? _receiptPath;

    /// <summary>
    /// Erstellt den Eintrag.
    /// </summary>
    /// <param name="overview">Der Vorgang mit Strecke und Verbrauch.</param>
    /// <param name="showKind">Gibt an, ob die Art („Kraftstoff“/„Strom“) angezeigt wird (Hybridfahrzeuge oder abweichende Art).</param>
    /// <param name="photoStore">Die Ablage der Belegfotos.</param>
    /// <param name="edit">Öffnet das Formular zum Korrigieren dieses Vorgangs.</param>
    /// <param name="delete">Zeigt die Rückfrage zum Löschen.</param>
    /// <param name="confirmDelete">Löscht den Vorgang nach der Bestätigung.</param>
    /// <param name="canConfirmDelete">Gibt an, ob die Bestätigung gerade ausführbar ist.</param>
    public TankbookEntryItemViewModel(
        TankbookEntryOverview overview,
        bool showKind,
        IReceiptPhotoStore photoStore,
        Action<TankbookEntryItemViewModel> edit,
        Action<TankbookEntryItemViewModel> delete,
        Action<TankbookEntryItemViewModel> confirmDelete,
        Func<bool> canConfirmDelete)
    {
        ArgumentNullException.ThrowIfNull(overview);
        _photoStore = photoStore;
        Overview = overview;
        var entry = overview.Entry;
        DateText = TankbookEntryTexts.FormatDateTime(entry.OccurredAt);
        TotalText = TankbookEntryTexts.FormatAmount(entry.TotalAmount);
        UnitPriceText = TankbookEntryTexts.FormatUnitPrice(entry.UnitPrice, entry.Kind);
        QuantityText = TankbookEntryTexts.FormatQuantityLine(entry);
        DistanceText = overview.DistanceKm is { } distance && distance > 0 && overview.ConsumptionState == TankbookConsumptionState.Calculated
            ? TankbookEntryTexts.FormatDistanceLine(distance)
            : string.Empty;
        ConsumptionText = TankbookEntryTexts.GetConsumptionLine(overview);
        ConsumptionNote = TankbookEntryTexts.GetConsumptionNote(overview);
        NoteText = TankbookEntryTexts.FormatNote(entry.Note);
        KindText = showKind ? TankbookEntryTexts.GetKindLabel(entry.Kind) : string.Empty;
        EntryHint = TankbookEntryTexts.FormatEntryHint(entry);
        ConfirmDeleteHint = TankbookEntryTexts.FormatConfirmDeleteHint(entry);
        ReceiptHint = TankbookEntryTexts.FormatReceiptHint(entry);
        DeleteQuestion = TankbookEntryTexts.FormatDeleteQuestion(entry);
        EditCommand = new Command(() => edit(this));
        DeleteCommand = new Command(() => delete(this));
        ConfirmDeleteCommand = new Command(() => confirmDelete(this), canConfirmDelete);
        CancelDeleteCommand = new Command(HideDeleteConfirmation);
        ShowReceiptCommand = new Command(ToggleReceipt);
    }

    /// <summary>
    /// Der Vorgang mit Strecke und Verbrauch.
    /// </summary>
    public TankbookEntryOverview Overview { get; }

    /// <summary>
    /// Der Vorgang.
    /// </summary>
    public TankbookEntry Entry
    {
        get { return Overview.Entry; }
    }

    /// <summary>
    /// Die Kennung des Vorgangs (nur intern, wird nie angezeigt).
    /// </summary>
    public long Id
    {
        get { return Overview.Entry.Id; }
    }

    /// <summary>
    /// Der Name der Tankstelle bzw. des Ladeorts.
    /// </summary>
    public string StationName
    {
        get { return Overview.Entry.StationName; }
    }

    /// <summary>
    /// Datum und Uhrzeit, z. B. „18.05.2026, 14:30“.
    /// </summary>
    public string DateText { get; }

    /// <summary>
    /// Der Gesamtbetrag, z. B. „74,36 €“.
    /// </summary>
    public string TotalText { get; }

    /// <summary>
    /// Der Preis pro Einheit, z. B. „1,859 €/L“.
    /// </summary>
    public string UnitPriceText { get; }

    /// <summary>
    /// Die Menge, z. B. „Menge 40,00 L“.
    /// </summary>
    public string QuantityText { get; }

    /// <summary>
    /// Die Strecke seit dem vorherigen vollen Vorgang, z. B. „Strecke 500 km“; leer ohne berechneten Verbrauch.
    /// </summary>
    public string DistanceText { get; }

    /// <summary>
    /// Gibt an, ob eine Strecke angezeigt wird.
    /// </summary>
    public bool HasDistance
    {
        get { return DistanceText.Length > 0; }
    }

    /// <summary>
    /// Der Verbrauch, z. B. „Verbrauch 6,00 L/100 km“, oder der Grund, warum es keinen gibt.
    /// </summary>
    public string ConsumptionText { get; }

    /// <summary>
    /// Der Hinweis zur Annahme hinter einem Verbrauch ab Startstand; leer bei anderen Vorgängen.
    /// </summary>
    public string ConsumptionNote { get; }

    /// <summary>
    /// Gibt an, ob der Hinweis zur Annahme hinter dem Verbrauch angezeigt wird.
    /// </summary>
    public bool HasConsumptionNote
    {
        get { return ConsumptionNote.Length > 0; }
    }

    /// <summary>
    /// Die Notiz in Anführungszeichen; leer ohne Notiz.
    /// </summary>
    public string NoteText { get; }

    /// <summary>
    /// Gibt an, ob eine Notiz vorhanden ist.
    /// </summary>
    public bool HasNote
    {
        get { return NoteText.Length > 0; }
    }

    /// <summary>
    /// Gibt an, ob der Vorgang eine Teilbetankung ist.
    /// </summary>
    public bool IsPartial
    {
        get { return Overview.Entry.IsPartial; }
    }

    /// <summary>
    /// Die Art („Kraftstoff“ oder „Strom“); leer, wenn sie sich aus dem Fahrzeug ergibt.
    /// </summary>
    public string KindText { get; }

    /// <summary>
    /// Gibt an, ob die Art angezeigt wird.
    /// </summary>
    public bool HasKindText
    {
        get { return KindText.Length > 0; }
    }

    /// <summary>
    /// Hinweis von „Bearbeiten“ und „Löschen“ für die Sprachausgabe, z. B. „Vorgang vom 18.05.2026, 14:30, Alpha Tankstelle“.
    /// </summary>
    public string EntryHint { get; }

    /// <summary>
    /// Hinweis der Bestätigung der Löschrückfrage für die Sprachausgabe.
    /// </summary>
    public string ConfirmDeleteHint { get; }

    /// <summary>
    /// Hinweis von „Abbrechen“ der Löschrückfrage für die Sprachausgabe.
    /// </summary>
    public string CancelDeleteHint
    {
        get { return TankbookEntryTexts.CancelDeleteHint; }
    }

    /// <summary>
    /// Hinweis von „Beleg anzeigen“ für die Sprachausgabe.
    /// </summary>
    public string ReceiptHint { get; }

    /// <summary>
    /// Die Rückfrage vor dem Löschen.
    /// </summary>
    public string DeleteQuestion { get; }

    /// <summary>
    /// Gibt an, ob die Rückfrage zum Löschen angezeigt wird.
    /// </summary>
    public bool IsConfirmingDelete
    {
        get => _isConfirmingDelete;
        private set => SetProperty(ref _isConfirmingDelete, value);
    }

    /// <summary>
    /// Gibt an, ob ein Belegfoto hinterlegt ist.
    /// </summary>
    public bool HasReceipt
    {
        get { return Overview.Entry.ReceiptPhotoFileName is not null; }
    }

    /// <summary>
    /// Gibt an, ob das Belegfoto (bzw. der Hinweis auf die fehlende Datei) eingeblendet ist.
    /// </summary>
    public bool IsReceiptVisible
    {
        get => _isReceiptVisible;
        private set
        {
            if (SetProperty(ref _isReceiptVisible, value))
            {
                OnPropertyChanged(nameof(ReceiptButtonLabel));
                OnPropertyChanged(nameof(IsReceiptImageVisible));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob die Datei des eingeblendeten Belegfotos fehlt.
    /// </summary>
    public bool IsReceiptMissing
    {
        get => _isReceiptMissing;
        private set
        {
            if (SetProperty(ref _isReceiptMissing, value))
            {
                OnPropertyChanged(nameof(IsReceiptImageVisible));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob das Bild des Belegfotos angezeigt wird.
    /// </summary>
    public bool IsReceiptImageVisible
    {
        get { return IsReceiptVisible && !IsReceiptMissing; }
    }

    /// <summary>
    /// Der vollständige Pfad des eingeblendeten Belegfotos (Bildquelle der Anzeige); <see langword="null"/>, wenn keines eingeblendet ist.
    /// </summary>
    public string? ReceiptPath
    {
        get => _receiptPath;
        private set => SetProperty(ref _receiptPath, value);
    }

    /// <summary>
    /// Die Beschriftung „Beleg anzeigen“ bzw. „Beleg ausblenden“.
    /// </summary>
    public string ReceiptButtonLabel
    {
        get { return IsReceiptVisible ? TankbookEntryTexts.HideReceipt : TankbookEntryTexts.ShowReceipt; }
    }

    /// <summary>
    /// Befehl „Bearbeiten“.
    /// </summary>
    public ICommand EditCommand { get; }

    /// <summary>
    /// Befehl „Löschen“: zeigt die Rückfrage.
    /// </summary>
    public ICommand DeleteCommand { get; }

    /// <summary>
    /// Befehl „Ja, löschen“ der Rückfrage.
    /// </summary>
    public ICommand ConfirmDeleteCommand { get; }

    /// <summary>
    /// Befehl „Abbrechen“ der Rückfrage.
    /// </summary>
    public ICommand CancelDeleteCommand { get; }

    /// <summary>
    /// Befehl „Beleg anzeigen“ bzw. „Beleg ausblenden“.
    /// </summary>
    public ICommand ShowReceiptCommand { get; }

    /// <summary>
    /// Zeigt die Rückfrage zum Löschen.
    /// </summary>
    public void ShowDeleteConfirmation()
    {
        IsConfirmingDelete = true;
    }

    /// <summary>
    /// Blendet die Rückfrage zum Löschen aus.
    /// </summary>
    public void HideDeleteConfirmation()
    {
        IsConfirmingDelete = false;
    }

    /// <summary>
    /// Aktualisiert die Ausführbarkeit der Bestätigung, nachdem sich die Sperre während des Löschens geändert hat.
    /// </summary>
    public void RefreshCommandStates()
    {
        ((Command)ConfirmDeleteCommand).ChangeCanExecute();
    }

    private void ToggleReceipt()
    {
        if (IsReceiptVisible || Overview.Entry.ReceiptPhotoFileName is not { } fileName)
        {
            IsReceiptVisible = false;
            ReceiptPath = null;
            return;
        }

        var exists = _photoStore.Exists(fileName);
        IsReceiptMissing = !exists;
        ReceiptPath = exists ? _photoStore.GetFullPath(fileName) : null;
        IsReceiptVisible = true;
    }
}

using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Formular zum Erfassen und Korrigieren eines Tank- oder Ladevorgangs (Bereich in der Seite): Datum und Uhrzeit, Tankstelle (frei oder aus den bekannten
/// Tankstellen), Art, Menge, Preis pro Einheit und Gesamtbetrag mit Rechenhinweis, Kilometerstand, Teilbetankung, Notiz und Belegfoto. Alle Feldfehler
/// erscheinen gleichzeitig, der erste wird angesagt. Ein ausgewähltes, noch nicht gespeichertes Foto wird beim Abbrechen oder Ersetzen wieder gelöscht.
/// Protokolliert werden nie Namen oder Werte.
/// </summary>
public sealed class TankbookEntryFormViewModel : BaseViewModel
{
    private readonly ITankbookEntryService _service;
    private readonly IReceiptPhotoStore _photoStore;
    private readonly IReceiptPhotoPicker _photoPicker;
    private readonly TimeProvider _timeProvider;
    private readonly IScreenReaderAnnouncer _announcer;
    private readonly ILogger<TankbookEntryFormViewModel> _logger;
    private readonly Command _saveCommand;
    private readonly Command _pickPhotoCommand;
    private readonly Command _capturePhotoCommand;
    private Vehicle? _vehicle;
    private TankbookEntry? _editingEntry;
    private string? _pendingPhoto;
    private bool _isOpen;
    private bool _isSaving;
    private bool _isTakingPhoto;
    private int _session;
    private bool _isSelectingStation;
    private string _formTitle = TankbookEntryTexts.NewEntryTitle;
    private DateTime? _entryDate;
    private TimeSpan? _entryTime;
    private DateTime _maximumDate = DateTime.Today;
    private IReadOnlyList<ChoiceOptionViewModel<TankbookEntryKind>> _kindOptions = [];
    private TankbookEntryKind _selectedKind = TankbookEntryKind.Fuel;
    private IReadOnlyList<KnownStationOptionViewModel> _knownStations = [];
    private string _stationName = string.Empty;
    private string? _stationId;
    private string _quantity = string.Empty;
    private string _unitPrice = string.Empty;
    private string _totalAmount = string.Empty;
    private string _computedHint = string.Empty;
    private string _odometer = string.Empty;
    private string _odometerPlaceholder = string.Empty;
    private bool _isPartial;
    private string _note = string.Empty;
    private string? _photoFileName;
    private string? _photoPreviewPath;
    private string _dateError = string.Empty;
    private string _stationError = string.Empty;
    private string _quantityError = string.Empty;
    private string _unitPriceError = string.Empty;
    private string _totalAmountError = string.Empty;
    private string _odometerError = string.Empty;
    private string _noteError = string.Empty;
    private string _photoError = string.Empty;
    private string? _statusMessage;

    /// <summary>
    /// Erstellt das Formular.
    /// </summary>
    /// <param name="service">Der Dienst der Vorgänge.</param>
    /// <param name="photoStore">Die Ablage der Belegfotos.</param>
    /// <param name="photoPicker">Die Fotoauswahl.</param>
    /// <param name="timeProvider">Die Zeitquelle für die Vorbelegung von Datum und Uhrzeit.</param>
    /// <param name="announcer">Sagt Fehlermeldungen über die Sprachausgabe an.</param>
    /// <param name="logger">Logger (protokolliert nie Namen oder Werte).</param>
    public TankbookEntryFormViewModel(
        ITankbookEntryService service,
        IReceiptPhotoStore photoStore,
        IReceiptPhotoPicker photoPicker,
        TimeProvider timeProvider,
        IScreenReaderAnnouncer announcer,
        ILogger<TankbookEntryFormViewModel> logger)
    {
        _service = service;
        _photoStore = photoStore;
        _photoPicker = photoPicker;
        _timeProvider = timeProvider;
        _announcer = announcer;
        _logger = logger;
        LastTask = Task.CompletedTask;
        _saveCommand = new Command(() => LastTask = SaveAsync(), () => !IsSaving);
        SaveCommand = _saveCommand;
        CancelCommand = new Command(Cancel);
        _pickPhotoCommand = new Command(() => LastTask = TakePhotoAsync(capture: false), () => !_isTakingPhoto);
        _capturePhotoCommand = new Command(() => LastTask = TakePhotoAsync(capture: true), () => !_isTakingPhoto);
        PickPhotoCommand = _pickPhotoCommand;
        CapturePhotoCommand = _capturePhotoCommand;
        RemovePhotoCommand = new Command(RemovePhoto);
    }

    /// <summary>
    /// Wird nach erfolgreichem Speichern ausgelöst; die Seite lädt dann die Vorgänge neu.
    /// </summary>
    public event EventHandler? Saved;

    /// <summary>
    /// Der zuletzt gestartete Vorgang (für Tests und Abwarten).
    /// </summary>
    public Task LastTask { get; private set; }

    /// <summary>
    /// Gibt an, ob das Formular geöffnet ist.
    /// </summary>
    public bool IsOpen
    {
        get => _isOpen;
        private set
        {
            if (SetProperty(ref _isOpen, value))
            {
                OnPropertyChanged(nameof(IsClosed));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob das Formular geschlossen ist.
    /// </summary>
    public bool IsClosed
    {
        get { return !IsOpen; }
    }

    /// <summary>
    /// Gibt an, ob gerade gespeichert wird; „Speichern“ ist dann gesperrt.
    /// </summary>
    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            if (SetProperty(ref _isSaving, value))
            {
                _saveCommand.ChangeCanExecute();
            }
        }
    }

    /// <summary>
    /// Die Überschrift („Neuer Vorgang“ oder „Vorgang bearbeiten“).
    /// </summary>
    public string FormTitle
    {
        get => _formTitle;
        private set => SetProperty(ref _formTitle, value);
    }

    /// <summary>
    /// Die Kennung des korrigierten Vorgangs; <see langword="null"/> beim Erfassen.
    /// </summary>
    public long? EditingEntryId
    {
        get { return _editingEntry?.Id; }
    }

    /// <summary>
    /// Das gewählte Datum.
    /// </summary>
    public DateTime? EntryDate
    {
        get => _entryDate;
        set => SetProperty(ref _entryDate, value);
    }

    /// <summary>
    /// Die gewählte Uhrzeit.
    /// </summary>
    public TimeSpan? EntryTime
    {
        get => _entryTime;
        set => SetProperty(ref _entryTime, value);
    }

    /// <summary>
    /// Das späteste wählbare Datum (heute).
    /// </summary>
    public DateTime MaximumDate
    {
        get => _maximumDate;
        private set => SetProperty(ref _maximumDate, value);
    }

    /// <summary>
    /// Die wählbaren Arten; bei nur einer Art wird sie angezeigt, nicht gewählt.
    /// </summary>
    public IReadOnlyList<ChoiceOptionViewModel<TankbookEntryKind>> KindOptions
    {
        get => _kindOptions;
        private set
        {
            if (SetProperty(ref _kindOptions, value))
            {
                OnPropertyChanged(nameof(ShowKindChoice));
                OnPropertyChanged(nameof(ShowKindText));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob die Art gewählt werden kann (Hybridfahrzeuge).
    /// </summary>
    public bool ShowKindChoice
    {
        get { return KindOptions.Count > 1; }
    }

    /// <summary>
    /// Gibt an, ob die feste Art als Text angezeigt wird.
    /// </summary>
    public bool ShowKindText
    {
        get { return KindOptions.Count <= 1; }
    }

    /// <summary>
    /// Die gewählte Art.
    /// </summary>
    public TankbookEntryKind SelectedKind
    {
        get => _selectedKind;
        private set
        {
            if (SetProperty(ref _selectedKind, value))
            {
                OnPropertyChanged(nameof(KindText));
                OnPropertyChanged(nameof(QuantityUnit));
                OnPropertyChanged(nameof(UnitPriceUnit));
                OnPropertyChanged(nameof(UnitPriceLabel));
                OnPropertyChanged(nameof(PartialHintText));
                UpdateComputedHint();
            }
        }
    }

    /// <summary>
    /// Die Beschriftung der gewählten Art („Kraftstoff“ oder „Strom“).
    /// </summary>
    public string KindText
    {
        get { return TankbookEntryTexts.GetKindLabel(SelectedKind); }
    }

    /// <summary>
    /// Die Einheit der Menge („L“ oder „kWh“).
    /// </summary>
    public string QuantityUnit
    {
        get { return TankbookEntryTexts.GetQuantityUnit(SelectedKind); }
    }

    /// <summary>
    /// Die Einheit des Preises („€/L“ oder „€/kWh“).
    /// </summary>
    public string UnitPriceUnit
    {
        get { return TankbookEntryTexts.GetUnitPriceUnit(SelectedKind); }
    }

    /// <summary>
    /// Die Beschriftung des Preisfelds („Preis pro Liter“ oder „Preis pro kWh“).
    /// </summary>
    public string UnitPriceLabel
    {
        get { return TankbookEntryTexts.GetUnitPriceLabel(SelectedKind); }
    }

    /// <summary>
    /// Die bekannten Tankstellen als Chips.
    /// </summary>
    public IReadOnlyList<KnownStationOptionViewModel> KnownStations
    {
        get => _knownStations;
        private set
        {
            if (SetProperty(ref _knownStations, value))
            {
                OnPropertyChanged(nameof(HasKnownStations));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob es bekannte Tankstellen gibt.
    /// </summary>
    public bool HasKnownStations
    {
        get { return KnownStations.Count > 0; }
    }

    /// <summary>
    /// Der Name der Tankstelle bzw. des Ladeorts; eine Änderung von Hand löst die Zuordnung zu einer ausgewählten Favoriten-Tankstelle.
    /// </summary>
    public string StationName
    {
        get => _stationName;
        set
        {
            if (SetProperty(ref _stationName, value ?? string.Empty) && !_isSelectingStation)
            {
                OnStationNameEdited();
            }
        }
    }

    /// <summary>
    /// Die Kennung der ausgewählten Favoriten-Tankstelle (nur intern, wird nie angezeigt).
    /// </summary>
    public string? StationId
    {
        get => _stationId;
        private set => SetProperty(ref _stationId, value);
    }

    /// <summary>
    /// Die eingegebene Menge.
    /// </summary>
    public string Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value ?? string.Empty))
            {
                UpdateComputedHint();
            }
        }
    }

    /// <summary>
    /// Der eingegebene Preis pro Einheit.
    /// </summary>
    public string UnitPrice
    {
        get => _unitPrice;
        set
        {
            if (SetProperty(ref _unitPrice, value ?? string.Empty))
            {
                UpdateComputedHint();
            }
        }
    }

    /// <summary>
    /// Der eingegebene Gesamtbetrag.
    /// </summary>
    public string TotalAmount
    {
        get => _totalAmount;
        set
        {
            if (SetProperty(ref _totalAmount, value ?? string.Empty))
            {
                UpdateComputedHint();
            }
        }
    }

    /// <summary>
    /// Der Rechenhinweis für den aus zwei Werten berechneten dritten Wert, z. B. „Ergibt 74,36 €“; leer, wenn nichts berechnet wird.
    /// </summary>
    public string ComputedHint
    {
        get => _computedHint;
        private set
        {
            if (SetProperty(ref _computedHint, value))
            {
                OnPropertyChanged(nameof(HasComputedHint));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob ein Rechenhinweis angezeigt wird.
    /// </summary>
    public bool HasComputedHint
    {
        get { return ComputedHint.Length > 0; }
    }

    /// <summary>
    /// Der eingegebene Kilometerstand.
    /// </summary>
    public string Odometer
    {
        get => _odometer;
        set => SetProperty(ref _odometer, value ?? string.Empty);
    }

    /// <summary>
    /// Der Platzhalter des Kilometerstands mit dem letzten bekannten Stand.
    /// </summary>
    public string OdometerPlaceholder
    {
        get => _odometerPlaceholder;
        private set => SetProperty(ref _odometerPlaceholder, value);
    }

    /// <summary>
    /// Gibt an, ob es eine Teilbetankung ist.
    /// </summary>
    public bool IsPartial
    {
        get => _isPartial;
        set
        {
            if (SetProperty(ref _isPartial, value))
            {
                OnPropertyChanged(nameof(ShowPartialHint));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob der Hinweis zur Teilbetankung angezeigt wird (solange „Teilbetankung“ aktiv ist).
    /// </summary>
    public bool ShowPartialHint
    {
        get { return IsPartial; }
    }

    /// <summary>
    /// Der Hinweis zur Teilbetankung.
    /// </summary>
    public string PartialHintText
    {
        get { return TankbookEntryTexts.GetPartialHint(SelectedKind); }
    }

    /// <summary>
    /// Die Notiz.
    /// </summary>
    public string Note
    {
        get => _note;
        set => SetProperty(ref _note, value ?? string.Empty);
    }

    /// <summary>
    /// Der Dateiname des aktuellen Belegfotos (gespeichert oder neu ausgewählt); <see langword="null"/> ohne Foto.
    /// </summary>
    public string? PhotoFileName
    {
        get => _photoFileName;
        private set
        {
            if (SetProperty(ref _photoFileName, value))
            {
                OnPropertyChanged(nameof(HasPhoto));
                OnPropertyChanged(nameof(HasNoPhoto));
                PhotoPreviewPath = value is not null && _photoStore.GetFullPath(value) is { } path && File.Exists(path) ? path : null;
            }
        }
    }

    /// <summary>
    /// Gibt an, ob ein Belegfoto vorhanden ist.
    /// </summary>
    public bool HasPhoto
    {
        get { return PhotoFileName is not null; }
    }

    /// <summary>
    /// Gibt an, ob kein Belegfoto vorhanden ist.
    /// </summary>
    public bool HasNoPhoto
    {
        get { return PhotoFileName is null; }
    }

    /// <summary>
    /// Der vollständige Pfad des Belegfotos für die Vorschau; <see langword="null"/> ohne vorhandene Datei.
    /// </summary>
    public string? PhotoPreviewPath
    {
        get => _photoPreviewPath;
        private set => SetProperty(ref _photoPreviewPath, value);
    }

    /// <summary>
    /// Gibt an, ob das Gerät Fotos aufnehmen kann („Foto aufnehmen“ sonst ausgeblendet).
    /// </summary>
    public bool IsCaptureSupported
    {
        get { return _photoPicker.IsCaptureSupported; }
    }

    /// <summary>
    /// Die Fehlermeldung an Datum und Uhrzeit; leer, wenn keine anliegt.
    /// </summary>
    public string DateError
    {
        get => _dateError;
        private set
        {
            if (SetProperty(ref _dateError, value))
            {
                OnPropertyChanged(nameof(HasDateError));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob an Datum und Uhrzeit ein Fehler anliegt.
    /// </summary>
    public bool HasDateError
    {
        get { return DateError.Length > 0; }
    }

    /// <summary>
    /// Die Fehlermeldung an der Tankstelle; leer, wenn keine anliegt.
    /// </summary>
    public string StationError
    {
        get => _stationError;
        private set
        {
            if (SetProperty(ref _stationError, value))
            {
                OnPropertyChanged(nameof(HasStationError));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob an der Tankstelle ein Fehler anliegt.
    /// </summary>
    public bool HasStationError
    {
        get { return StationError.Length > 0; }
    }

    /// <summary>
    /// Die Fehlermeldung an der Menge; leer, wenn keine anliegt.
    /// </summary>
    public string QuantityError
    {
        get => _quantityError;
        private set
        {
            if (SetProperty(ref _quantityError, value))
            {
                OnPropertyChanged(nameof(HasQuantityError));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob an der Menge ein Fehler anliegt.
    /// </summary>
    public bool HasQuantityError
    {
        get { return QuantityError.Length > 0; }
    }

    /// <summary>
    /// Die Fehlermeldung am Preis pro Einheit; leer, wenn keine anliegt.
    /// </summary>
    public string UnitPriceError
    {
        get => _unitPriceError;
        private set
        {
            if (SetProperty(ref _unitPriceError, value))
            {
                OnPropertyChanged(nameof(HasUnitPriceError));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob am Preis pro Einheit ein Fehler anliegt.
    /// </summary>
    public bool HasUnitPriceError
    {
        get { return UnitPriceError.Length > 0; }
    }

    /// <summary>
    /// Die Fehlermeldung am Gesamtbetrag (auch fehlende Werte und Abweichungen); leer, wenn keine anliegt.
    /// </summary>
    public string TotalAmountError
    {
        get => _totalAmountError;
        private set
        {
            if (SetProperty(ref _totalAmountError, value))
            {
                OnPropertyChanged(nameof(HasTotalAmountError));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob am Gesamtbetrag ein Fehler anliegt.
    /// </summary>
    public bool HasTotalAmountError
    {
        get { return TotalAmountError.Length > 0; }
    }

    /// <summary>
    /// Die Fehlermeldung am Kilometerstand; leer, wenn keine anliegt.
    /// </summary>
    public string OdometerError
    {
        get => _odometerError;
        private set
        {
            if (SetProperty(ref _odometerError, value))
            {
                OnPropertyChanged(nameof(HasOdometerError));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob am Kilometerstand ein Fehler anliegt.
    /// </summary>
    public bool HasOdometerError
    {
        get { return OdometerError.Length > 0; }
    }

    /// <summary>
    /// Die Fehlermeldung an der Notiz; leer, wenn keine anliegt.
    /// </summary>
    public string NoteError
    {
        get => _noteError;
        private set
        {
            if (SetProperty(ref _noteError, value))
            {
                OnPropertyChanged(nameof(HasNoteError));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob an der Notiz ein Fehler anliegt.
    /// </summary>
    public bool HasNoteError
    {
        get { return NoteError.Length > 0; }
    }

    /// <summary>
    /// Die Fehlermeldung zum Belegfoto; leer, wenn keine anliegt.
    /// </summary>
    public string PhotoError
    {
        get => _photoError;
        private set
        {
            if (SetProperty(ref _photoError, value))
            {
                OnPropertyChanged(nameof(HasPhotoError));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob zum Belegfoto ein Fehler anliegt.
    /// </summary>
    public bool HasPhotoError
    {
        get { return PhotoError.Length > 0; }
    }

    /// <summary>
    /// Eine Meldung ohne Feldbezug (z. B. Speicherfehler); <see langword="null"/>, wenn keine anliegt.
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
    /// Befehl „Speichern“.
    /// </summary>
    public ICommand SaveCommand { get; }

    /// <summary>
    /// Befehl „Abbrechen“: schließt das Formular ohne zu speichern und verwirft ein neu ausgewähltes Foto.
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// Befehl „Foto auswählen“.
    /// </summary>
    public ICommand PickPhotoCommand { get; }

    /// <summary>
    /// Befehl „Foto aufnehmen“.
    /// </summary>
    public ICommand CapturePhotoCommand { get; }

    /// <summary>
    /// Befehl „Foto entfernen“.
    /// </summary>
    public ICommand RemovePhotoCommand { get; }

    /// <summary>
    /// Öffnet das leere Formular zum Erfassen: Datum und Uhrzeit jetzt (minutengenau), Art nach Fahrzeug, Teilbetankung aus.
    /// </summary>
    /// <param name="vehicle">Das Fahrzeug.</param>
    /// <param name="lastOdometerKm">Der Kilometerstand des letzten Vorgangs; <see langword="null"/> ohne Vorgänge (dann der Start-Kilometerstand).</param>
    public void BeginCreate(Vehicle vehicle, int? lastOdometerKm)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        DiscardPendingPhoto();
        _session++;
        _vehicle = vehicle;
        _editingEntry = null;
        var now = Now();
        MaximumDate = now.Date;
        EntryDate = now.Date;
        EntryTime = new TimeSpan(now.Hour, now.Minute, 0);
        SetKindOptions(vehicle.FuelKind, null);
        SetStationSilently(string.Empty, null);
        SetAmountsSilently(string.Empty, string.Empty, string.Empty);
        Odometer = string.Empty;
        OdometerPlaceholder = TankbookEntryTexts.FormatOdometerPlaceholder(lastOdometerKm ?? vehicle.StartOdometerKm);
        IsPartial = false;
        Note = string.Empty;
        PhotoFileName = null;
        ClearMessages();
        FormTitle = TankbookEntryTexts.NewEntryTitle;
        IsOpen = true;
    }

    /// <summary>
    /// Öffnet das Formular zum Korrigieren mit den gespeicherten Werten (Dezimalzahlen deutsch formatiert) und dem vorhandenen Foto.
    /// </summary>
    /// <param name="vehicle">Das Fahrzeug.</param>
    /// <param name="entry">Der Vorgang.</param>
    public void BeginEdit(Vehicle vehicle, TankbookEntry entry)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        ArgumentNullException.ThrowIfNull(entry);
        DiscardPendingPhoto();
        _session++;
        _vehicle = vehicle;
        _editingEntry = entry;
        MaximumDate = Now().Date > entry.OccurredAt.Date ? Now().Date : entry.OccurredAt.Date;
        EntryDate = entry.OccurredAt.Date;
        EntryTime = new TimeSpan(entry.OccurredAt.Hour, entry.OccurredAt.Minute, 0);
        SetKindOptions(vehicle.FuelKind, entry.Kind);
        SetStationSilently(entry.StationName, entry.StationId);
        SetAmountsSilently(
            TankbookEntryTexts.FormatDecimalInput(entry.Quantity),
            TankbookEntryTexts.FormatDecimalInput(entry.UnitPrice),
            TankbookEntryTexts.FormatAmountInput(entry.TotalAmount));
        Odometer = TankbookTexts.FormatOdometerInput(entry.OdometerKm);
        OdometerPlaceholder = TankbookEntryTexts.FormatOdometerPlaceholder(entry.OdometerKm);
        IsPartial = entry.IsPartial;
        Note = entry.Note ?? string.Empty;
        PhotoFileName = entry.ReceiptPhotoFileName;
        ClearMessages();
        FormTitle = TankbookEntryTexts.EditEntryTitle;
        IsOpen = true;
    }

    /// <summary>
    /// Übernimmt die bekannten Tankstellen als Chips; eine ausgewählte Favoriten-Tankstelle bleibt ausgewählt.
    /// </summary>
    /// <param name="stations">Die bekannten Tankstellen.</param>
    public void SetKnownStations(IReadOnlyList<KnownStation> stations)
    {
        ArgumentNullException.ThrowIfNull(stations);
        var labels = TankbookEntryTexts.FormatKnownStationLabels(stations);
        KnownStations = stations
            .Select((station, index) => new KnownStationOptionViewModel(station, labels[index], index, OnKnownStationSelected))
            .ToList();
        MarkSelectedStation();
    }

    /// <summary>
    /// Schließt das Formular und verwirft ein neu ausgewähltes, noch nicht gespeichertes Foto.
    /// </summary>
    public void Cancel()
    {
        DiscardPendingPhoto();
        _session++;
        IsOpen = false;
        ClearMessages();
    }

    private static DateTime? Combine(DateTime? date, TimeSpan? time)
    {
        return date is { } day && time is { } clock ? day.Date + clock : null;
    }

    private DateTime Now()
    {
        return _timeProvider.GetLocalNow().DateTime;
    }

    private void SetKindOptions(VehicleFuelKind fuelKind, TankbookEntryKind? storedKind)
    {
        var kinds = new[] { TankbookEntryKind.Fuel, TankbookEntryKind.Electricity }
            .Where(kind => TankbookEntryValidator.IsKindAllowed(fuelKind, kind) || kind == storedKind)
            .ToList();
        var selected = storedKind ?? kinds[0];
        KindOptions = kinds
            .Select(kind => new ChoiceOptionViewModel<TankbookEntryKind>(kind, TankbookEntryTexts.GetKindLabel(kind), OnKindSelected))
            .ToList();
        foreach (var option in KindOptions)
        {
            option.SetSelectedSilently(option.Value == selected);
        }

        SelectedKind = selected;
    }

    private void OnKindSelected(ChoiceOptionViewModel<TankbookEntryKind> selected)
    {
        foreach (var option in KindOptions)
        {
            if (!ReferenceEquals(option, selected))
            {
                option.SetSelectedSilently(false);
            }
        }

        SelectedKind = selected.Value;
    }

    private void OnKnownStationSelected(KnownStationOptionViewModel selected)
    {
        foreach (var option in KnownStations)
        {
            if (!ReferenceEquals(option, selected))
            {
                option.SetSelectedSilently(false);
            }
        }

        SetStationSilently(selected.Station.Name, selected.Station.StationId);
        StationError = string.Empty;
    }

    private void OnStationNameEdited()
    {
        // Ein von Hand geänderter Name gehört nicht mehr zur ausgewählten Favoriten-Tankstelle.
        StationId = null;
        foreach (var option in KnownStations)
        {
            option.SetSelectedSilently(string.Equals(option.Station.Name, StationName.Trim(), StringComparison.Ordinal) && option.Station.StationId is null);
        }
    }

    private void SetStationSilently(string name, string? stationId)
    {
        _isSelectingStation = true;
        try
        {
            StationName = name;
            StationId = stationId;
        }
        finally
        {
            _isSelectingStation = false;
        }

        MarkSelectedStation();
    }

    private void MarkSelectedStation()
    {
        foreach (var option in KnownStations)
        {
            var matches = string.Equals(option.Station.Name, StationName, StringComparison.Ordinal)
                && string.Equals(option.Station.StationId, StationId, StringComparison.Ordinal);
            option.SetSelectedSilently(matches && StationName.Length > 0);
        }
    }

    private void SetAmountsSilently(string quantity, string unitPrice, string totalAmount)
    {
        _quantity = quantity;
        _unitPrice = unitPrice;
        _totalAmount = totalAmount;
        OnPropertyChanged(nameof(Quantity));
        OnPropertyChanged(nameof(UnitPrice));
        OnPropertyChanged(nameof(TotalAmount));
        UpdateComputedHint();
    }

    private void UpdateComputedHint()
    {
        var errors = new[]
        {
            DecimalInputParser.Parse(Quantity, TankbookEntryLimits.MaxQuantityDecimals, TankbookEntryLimits.MaxQuantityExclusive, out var quantity),
            DecimalInputParser.Parse(UnitPrice, TankbookEntryLimits.MaxUnitPriceDecimals, TankbookEntryLimits.MaxUnitPriceExclusive, out var unitPrice),
            DecimalInputParser.Parse(TotalAmount, TankbookEntryLimits.MaxTotalAmountDecimals, TankbookEntryLimits.MaxTotalAmountExclusive, out var totalAmount),
        };
        if (errors.Any(error => error != TankbookEntryFieldError.None))
        {
            ComputedHint = string.Empty;
            return;
        }

        var completion = AmountCalculator.Complete(quantity, unitPrice, totalAmount);
        ComputedHint = completion.ComputedField switch
        {
            AmountField.Quantity when completion.Quantity is { } value => TankbookEntryTexts.FormatComputedHint(AmountField.Quantity, value, SelectedKind),
            AmountField.UnitPrice when completion.UnitPrice is { } value => TankbookEntryTexts.FormatComputedHint(AmountField.UnitPrice, value, SelectedKind),
            AmountField.TotalAmount when completion.TotalAmount is { } value => TankbookEntryTexts.FormatComputedHint(AmountField.TotalAmount, value, SelectedKind),
            _ => string.Empty,
        };
    }

    private async Task SaveAsync()
    {
        if (IsSaving || _vehicle is null)
        {
            return;
        }

        var validation = TankbookEntryValidator.ValidateForm(
            Combine(EntryDate, EntryTime),
            Now(),
            SelectedKind,
            StationName,
            StationId,
            Quantity,
            UnitPrice,
            TotalAmount,
            Odometer,
            IsPartial,
            Note,
            PhotoFileName);
        DecimalInputParser.Parse(TotalAmount, TankbookEntryLimits.MaxTotalAmountDecimals, TankbookEntryLimits.MaxTotalAmountExclusive, out var enteredTotal);
        DateError = TankbookEntryTexts.GetOccurredAtError(validation.OccurredAtError);
        StationError = TankbookEntryTexts.GetStationError(validation.StationError);
        QuantityError = TankbookEntryTexts.GetQuantityError(validation.QuantityError, SelectedKind);
        UnitPriceError = TankbookEntryTexts.GetUnitPriceError(validation.UnitPriceError, SelectedKind);
        TotalAmountError = TankbookEntryTexts.GetTotalAmountError(validation.TotalAmountError, SelectedKind, validation.ExpectedTotal, enteredTotal);
        OdometerError = TankbookEntryTexts.GetOdometerError(validation.OdometerError);
        NoteError = TankbookEntryTexts.GetNoteError(validation.NoteError);
        StatusMessage = null;
        if (validation.Input is not { } input)
        {
            _announcer.Announce(new[] { DateError, StationError, QuantityError, UnitPriceError, TotalAmountError, OdometerError, NoteError }.First(error => error.Length > 0));
            return;
        }

        // Der berechnete dritte Wert wird ins leere Feld übernommen.
        SetAmountsSilently(
            Quantity.Trim().Length == 0 ? TankbookEntryTexts.FormatDecimalInput(input.Quantity) : Quantity,
            UnitPrice.Trim().Length == 0 ? TankbookEntryTexts.FormatDecimalInput(input.UnitPrice) : UnitPrice,
            TotalAmount.Trim().Length == 0 ? TankbookEntryTexts.FormatAmountInput(input.TotalAmount) : TotalAmount);

        var saved = false;
        IsSaving = true;
        try
        {
            var result = _editingEntry is { } entry
                ? await _service.UpdateEntryAsync(entry.Id, input).ConfigureAwait(true)
                : await _service.CreateEntryAsync(_vehicle.Id, input).ConfigureAwait(true);
            switch (result.Result)
            {
                case TankbookEntryResult.Ok:
                    // Das neu ausgewählte Foto gehört jetzt zum gespeicherten Vorgang.
                    _pendingPhoto = null;
                    _session++;
                    IsOpen = false;
                    ClearMessages();
                    saved = true;
                    break;
                case TankbookEntryResult.OdometerBelowVehicleStart:
                case TankbookEntryResult.OdometerBelowPrevious:
                case TankbookEntryResult.OdometerAboveNext:
                case TankbookEntryResult.InvalidOdometer:
                    OdometerError = TankbookEntryTexts.GetResultMessage(result.Result, result.OdometerBoundKm);
                    _announcer.Announce(OdometerError);
                    break;
                case TankbookEntryResult.InvalidOccurredAt:
                    DateError = TankbookEntryTexts.GetResultMessage(result.Result, null);
                    _announcer.Announce(DateError);
                    break;
                default:
                    StatusMessage = TankbookEntryTexts.GetResultMessage(result.Result, result.OdometerBoundKm);
                    _announcer.Announce(StatusMessage);
                    break;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Der Vorgang konnte nicht gespeichert werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = TankbookTexts.SaveFailed;
            _announcer.Announce(StatusMessage);
        }
        finally
        {
            IsSaving = false;
        }

        if (saved)
        {
            Saved?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task TakePhotoAsync(bool capture)
    {
        if (_isTakingPhoto)
        {
            return;
        }

        PhotoError = string.Empty;
        var session = _session;
        SetTakingPhoto(true);
        try
        {
            var photo = capture
                ? await _photoPicker.CaptureAsync().ConfigureAwait(true)
                : await _photoPicker.PickAsync().ConfigureAwait(true);
            if (photo is null)
            {
                return;
            }

            ReceiptPhotoImportResult result;
            await using (var stream = await photo.OpenReadAsync().ConfigureAwait(true))
            {
                result = await _photoStore.ImportAsync(stream, photo.Extension).ConfigureAwait(true);
            }

            if (session != _session || !IsOpen)
            {
                // Das Formular wurde inzwischen abgebrochen, gespeichert oder für einen anderen Vorgang geöffnet: Das Foto gehört zu keinem Formular mehr.
                if (result.FileName is { } orphan)
                {
                    _photoStore.Delete(orphan);
                }

                return;
            }

            if (result.FileName is not { } fileName)
            {
                PhotoError = TankbookEntryTexts.GetPhotoError(result.Error);
                _announcer.Announce(PhotoError);
                return;
            }

            DiscardPendingPhoto();
            _pendingPhoto = fileName;
            PhotoFileName = fileName;
        }
        catch (ReceiptPhotoAccessException ex)
        {
            // Berechtigung verweigert oder Funktion nicht verfügbar: eigene Meldung mit Hinweis auf die Einstellungen.
            _logger.LogWarning("Der Zugriff auf das Belegfoto ist nicht möglich ({ExceptionType}, {Problem}).", ex.GetType().Name, ex.Problem);
            PhotoError = TankbookEntryTexts.GetPhotoAccessError(ex.Problem, capture);
            _announcer.Announce(PhotoError);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Das Belegfoto konnte nicht übernommen werden ({ExceptionType}).", ex.GetType().Name);
            PhotoError = TankbookEntryTexts.PhotoFailed;
            _announcer.Announce(PhotoError);
        }
        finally
        {
            SetTakingPhoto(false);
        }
    }

    private void SetTakingPhoto(bool value)
    {
        _isTakingPhoto = value;
        _pickPhotoCommand.ChangeCanExecute();
        _capturePhotoCommand.ChangeCanExecute();
    }

    private void RemovePhoto()
    {
        if (PhotoFileName is not null && string.Equals(PhotoFileName, _pendingPhoto, StringComparison.Ordinal))
        {
            DiscardPendingPhoto();
        }

        PhotoFileName = null;
        PhotoError = string.Empty;
    }

    private void DiscardPendingPhoto()
    {
        if (_pendingPhoto is { } pending)
        {
            _pendingPhoto = null;
            _photoStore.Delete(pending);
        }
    }

    private void ClearMessages()
    {
        DateError = string.Empty;
        StationError = string.Empty;
        QuantityError = string.Empty;
        UnitPriceError = string.Empty;
        TotalAmountError = string.Empty;
        OdometerError = string.Empty;
        NoteError = string.Empty;
        PhotoError = string.Empty;
        StatusMessage = null;
    }
}

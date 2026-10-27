using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services;
using Tankradar.MAUI.Services.Backup;
using Tankradar.MAUI.Services.Navigation;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// ViewModel des Tabs „Tankbuch“: Tank- und Ladevorgänge des gewählten Fahrzeugs mit Fahrzeugkopf und Fahrzeugwechsel, Liste der Vorgänge (neueste zuerst)
/// mit Verbrauch, Formular zum Erfassen und Korrigieren (in der Seite) und Löschen nach einer Rückfrage im Eintrag. Das Fahrzeug wird über seinen Namen gewählt,
/// nie über eine angezeigte Kennung; die Auswahl wird gerätelokal gemerkt. Ohne Fahrzeug zeigt der Kopf einen Hinweis mit dem Weg zum Anlegen.
/// Protokolliert werden nie Namen oder Werte.
/// </summary>
public sealed class TankbookEntriesViewModel : BaseViewModel
{
    private readonly IVehicleService _vehicleService;
    private readonly ITankbookEntryService _entryService;
    private readonly IReceiptPhotoStore _photoStore;
    private readonly ITankbookNavigator _navigator;
    private readonly IScreenReaderAnnouncer _announcer;
    private readonly IVehicleSelectionStore _selectionStore;
    private readonly ILogger<TankbookEntriesViewModel> _logger;
    private readonly Command _toggleVehiclePickerCommand;
    private IReadOnlyList<VehicleOptionViewModel> _vehicleOptions = [];
    private bool _isVehiclePickerOpen;
    private long? _vehicleId;
    private Vehicle? _vehicle;
    private IReadOnlyList<TankbookEntryItemViewModel> _entries = [];
    private bool _isLoaded;
    private bool _isDeleting;
    private string? _statusMessage;

    /// <summary>
    /// Erstellt das ViewModel mit dem Seitentitel „Tankbuch“.
    /// </summary>
    /// <param name="vehicleService">Der Fahrzeugdienst.</param>
    /// <param name="entryService">Der Dienst der Vorgänge.</param>
    /// <param name="photoStore">Die Ablage der Belegfotos (Anzeige der Belege).</param>
    /// <param name="form">Das Formular zum Erfassen und Korrigieren.</param>
    /// <param name="statistics">Der Bereich „Auswertung“.</param>
    /// <param name="export">Der Bereich „Export“.</param>
    /// <param name="restoreNotifier">Meldet, dass die Daten aus einer Sicherung wiederhergestellt wurden; die Seite wird dann neu geladen.</param>
    /// <param name="navigator">Öffnet die Fahrzeugverwaltung.</param>
    /// <param name="announcer">Sagt das gewählte Fahrzeug, Rückfragen und Meldungen über die Sprachausgabe an.</param>
    /// <param name="selectionStore">Merkt sich gerätelokal die Kennung des gewählten Fahrzeugs.</param>
    /// <param name="logger">Logger (protokolliert nie Namen oder Werte).</param>
    public TankbookEntriesViewModel(
        IVehicleService vehicleService,
        ITankbookEntryService entryService,
        IReceiptPhotoStore photoStore,
        TankbookEntryFormViewModel form,
        TankbookStatisticsViewModel statistics,
        TankbookExportViewModel export,
        IRestoreNotifier restoreNotifier,
        ITankbookNavigator navigator,
        IScreenReaderAnnouncer announcer,
        IVehicleSelectionStore selectionStore,
        ILogger<TankbookEntriesViewModel> logger)
    {
        _vehicleService = vehicleService;
        _entryService = entryService;
        _photoStore = photoStore;
        _navigator = navigator;
        _announcer = announcer;
        _selectionStore = selectionStore;
        _logger = logger;
        Form = form;
        Statistics = statistics;
        Export = export;
        restoreNotifier.Restored += OnRestored;
        Form.Saved += OnFormSaved;
        Title = TankbookTexts.PageTitle;
        LastTask = Task.CompletedTask;
        AddEntryCommand = new Command(BeginCreate);
        _toggleVehiclePickerCommand = new Command(() => IsVehiclePickerOpen = !IsVehiclePickerOpen, () => HasVehicles);
        ToggleVehiclePickerCommand = _toggleVehiclePickerCommand;
        ManageVehiclesCommand = new Command(() => LastTask = OpenVehicleManagementAsync(false));
        AddVehicleCommand = new Command(() => LastTask = OpenVehicleManagementAsync(true));
    }

    /// <summary>
    /// Der zuletzt gestartete Vorgang (für Tests und Abwarten).
    /// </summary>
    public Task LastTask { get; private set; }

    /// <summary>
    /// Das Formular zum Erfassen und Korrigieren.
    /// </summary>
    public TankbookEntryFormViewModel Form { get; }

    /// <summary>
    /// Der Bereich „Auswertung“ des Fahrzeugs.
    /// </summary>
    public TankbookStatisticsViewModel Statistics { get; }

    /// <summary>
    /// Der Bereich „Export“ des Fahrzeugs.
    /// </summary>
    public TankbookExportViewModel Export { get; }

    /// <summary>
    /// Das Fahrzeug; <see langword="null"/>, solange es nicht geladen ist.
    /// </summary>
    public Vehicle? Vehicle
    {
        get => _vehicle;
        private set
        {
            if (SetProperty(ref _vehicle, value))
            {
                OnPropertyChanged(nameof(VehicleName));
                OnPropertyChanged(nameof(VehicleDetails));
                OnPropertyChanged(nameof(HasVehicle));
                OnPropertyChanged(nameof(AddEntryHint));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob das Fahrzeug geladen ist.
    /// </summary>
    public bool HasVehicle
    {
        get { return Vehicle is not null; }
    }

    /// <summary>
    /// Der Name des Fahrzeugs.
    /// </summary>
    public string VehicleName
    {
        get { return Vehicle?.Name ?? string.Empty; }
    }

    /// <summary>
    /// Die Angaben des Fahrzeugs, z. B. „Diesel • Start 10.000 km“.
    /// </summary>
    public string VehicleDetails
    {
        get { return Vehicle is { } vehicle ? TankbookTexts.FormatVehicleDetails(vehicle) : string.Empty; }
    }

    /// <summary>
    /// Hinweis von „Vorgang erfassen“ für die Sprachausgabe, z. B. „Fahrzeug „Golf““.
    /// </summary>
    public string AddEntryHint
    {
        get { return Vehicle is { } vehicle ? TankbookTexts.FormatVehicleHint(vehicle.Name) : string.Empty; }
    }

    /// <summary>
    /// Die Vorgänge, neueste zuerst.
    /// </summary>
    public IReadOnlyList<TankbookEntryItemViewModel> Entries
    {
        get => _entries;
        private set
        {
            if (SetProperty(ref _entries, value))
            {
                OnPropertyChanged(nameof(HasEntries));
                OnPropertyChanged(nameof(ShowEmptyHint));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob es Vorgänge gibt.
    /// </summary>
    public bool HasEntries
    {
        get { return Entries.Count > 0; }
    }

    /// <summary>
    /// Gibt an, ob der Hinweis „Noch keine Tank- oder Ladevorgänge“ angezeigt wird (geladen und leer).
    /// </summary>
    public bool ShowEmptyHint
    {
        get { return IsLoaded && Vehicle is not null && Entries.Count == 0; }
    }

    /// <summary>
    /// Gibt an, ob die Vorgänge mindestens einmal geladen wurden.
    /// </summary>
    public bool IsLoaded
    {
        get => _isLoaded;
        private set
        {
            if (SetProperty(ref _isLoaded, value))
            {
                OnPropertyChanged(nameof(ShowEmptyHint));
                OnPropertyChanged(nameof(ShowNoVehiclesHint));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob gerade ein Vorgang gelöscht wird; die Bestätigung ist dann gesperrt.
    /// </summary>
    public bool IsDeleting
    {
        get => _isDeleting;
        private set
        {
            if (SetProperty(ref _isDeleting, value))
            {
                foreach (var item in Entries)
                {
                    item.RefreshCommandStates();
                }
            }
        }
    }

    /// <summary>
    /// Ein Hinweis oder eine Fehlermeldung ohne Feldbezug; <see langword="null"/>, wenn keine anliegt.
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
    /// Befehl „Vorgang erfassen“.
    /// </summary>
    public ICommand AddEntryCommand { get; }

    /// <summary>
    /// Die Kennung des angezeigten Fahrzeugs; <see langword="null"/>, solange keines gewählt ist.
    /// </summary>
    public long? VehicleId
    {
        get { return _vehicleId; }
    }

    /// <summary>
    /// Die Fahrzeuge nach Namen geordnet als Chips der Auswahl im Kopf; das angezeigte Fahrzeug ist ausgewählt.
    /// </summary>
    public IReadOnlyList<VehicleOptionViewModel> VehicleOptions
    {
        get => _vehicleOptions;
        private set
        {
            if (SetProperty(ref _vehicleOptions, value))
            {
                OnPropertyChanged(nameof(HasVehicles));
                OnPropertyChanged(nameof(ShowNoVehiclesHint));
                _toggleVehiclePickerCommand.ChangeCanExecute();
            }
        }
    }

    /// <summary>
    /// Gibt an, ob die Auswahl der Fahrzeuge im Kopf aufgeklappt ist.
    /// </summary>
    public bool IsVehiclePickerOpen
    {
        get => _isVehiclePickerOpen;
        private set => SetProperty(ref _isVehiclePickerOpen, value);
    }

    /// <summary>
    /// Gibt an, ob es Fahrzeuge gibt.
    /// </summary>
    public bool HasVehicles
    {
        get { return VehicleOptions.Count > 0; }
    }

    /// <summary>
    /// Gibt an, ob der Hinweis „Noch kein Fahrzeug angelegt“ mit „Fahrzeug hinzufügen“ angezeigt wird (geladen und ohne Fahrzeuge).
    /// </summary>
    public bool ShowNoVehiclesHint
    {
        get { return IsLoaded && !HasVehicles; }
    }

    /// <summary>
    /// Befehl der Auswahl im Kopf: klappt die Chips der Fahrzeuge auf oder zu.
    /// </summary>
    public ICommand ToggleVehiclePickerCommand { get; }

    /// <summary>
    /// Befehl „Fahrzeuge verwalten“: öffnet die Fahrzeugverwaltung.
    /// </summary>
    public ICommand ManageVehiclesCommand { get; }

    /// <summary>
    /// Befehl „Fahrzeug hinzufügen“ des Leerzustands: öffnet die Fahrzeugverwaltung mit dem Formular „Neues Fahrzeug“.
    /// </summary>
    public ICommand AddVehicleCommand { get; }

    /// <summary>
    /// Lädt Fahrzeuge und Vorgänge beim Erscheinen der Seite und sagt das gewählte Fahrzeug an.
    /// </summary>
    public override void OnAppearing()
    {
        LastTask = LoadAndAnnounceAsync();
    }

    /// <summary>
    /// Lädt die Fahrzeuge und bestimmt das angezeigte Fahrzeug (gemerkt, sonst das erste nach Name); weicht es von der gemerkten Kennung ab, wird die neue gemerkt.
    /// Lädt dann dessen Vorgänge, die bekannten Tankstellen und die Auswertung. Ohne Fahrzeug bleibt der Leerzustand.
    /// </summary>
    /// <returns>Ein Task, der nach dem Laden abgeschlossen ist.</returns>
    public Task LoadAsync()
    {
        return RunGuardedAsync(async () =>
        {
            var vehicles = await _vehicleService.GetVehiclesAsync().ConfigureAwait(true);
            var storedId = _selectionStore.Get();
            var vehicle = VehicleSelectionResolver.Resolve(vehicles, storedId);
            if (vehicle is not null && vehicle.Id != storedId)
            {
                _selectionStore.Set(vehicle.Id);
            }

            VehicleOptions = vehicles
                .Select((candidate, index) => new VehicleOptionViewModel(candidate, index, OnVehicleSelected))
                .ToList();
            MarkSelectedVehicle(vehicle?.Id);
            if (vehicle is null)
            {
                ResetVehicle(null);
                IsVehiclePickerOpen = false;
                IsLoaded = true;
                return;
            }

            if (vehicle.Id != _vehicleId)
            {
                ResetVehicle(vehicle.Id);
            }

            await LoadEntriesAsync(vehicle).ConfigureAwait(true);
        });
    }

    /// <summary>
    /// Wechselt zu einem Fahrzeug: schließt Auswahl und Formular, merkt sich die Kennung, lädt Vorgänge, Auswertung und Export neu (ohne die Fahrzeugliste
    /// erneut zu laden) und sagt das Fahrzeug an.
    /// </summary>
    /// <param name="vehicleId">Die Kennung des Fahrzeugs.</param>
    /// <returns>Ein Task, der nach dem Wechsel abgeschlossen ist.</returns>
    public async Task SelectVehicleAsync(long vehicleId)
    {
        IsVehiclePickerOpen = false;
        _selectionStore.Set(vehicleId);
        if (VehicleOptions.FirstOrDefault(option => option.Vehicle.Id == vehicleId) is not { } selected)
        {
            await LoadAndAnnounceAsync().ConfigureAwait(true);
            return;
        }

        MarkSelectedVehicle(vehicleId);
        ResetVehicle(vehicleId);
        await RunGuardedAsync(() => LoadEntriesAsync(selected.Vehicle)).ConfigureAwait(true);
        Announce(Vehicle);
    }

    /// <summary>
    /// Führt einen Ladeschritt mit Busy-Anzeige aus; ein Fehler wird protokolliert und als Ladefehler gemeldet.
    /// </summary>
    /// <param name="load">Der auszuführende Ladeschritt.</param>
    private async Task RunGuardedAsync(Func<Task> load)
    {
        IsBusy = true;
        try
        {
            await load().ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ReportLoadFailed(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadAndAnnounceAsync()
    {
        await LoadAsync().ConfigureAwait(true);
        Announce(Vehicle);
    }

    private async Task LoadEntriesAsync(Vehicle vehicle)
    {
        var overviews = await _entryService.GetEntriesAsync(vehicle.Id).ConfigureAwait(true);
        var stations = await _entryService.GetKnownStationsAsync().ConfigureAwait(true);
        Vehicle = vehicle;
        Export.SetVehicle(vehicle);
        Form.SetKnownStations(stations);
        Entries = overviews
            .Select(overview => new TankbookEntryItemViewModel(
                overview,
                vehicle.FuelKind == VehicleFuelKind.Hybrid || !TankbookEntryValidator.IsKindAllowed(vehicle.FuelKind, overview.Entry.Kind),
                _photoStore,
                BeginEdit,
                BeginDelete,
                item => LastTask = DeleteAsync(item),
                () => !IsDeleting))
            .ToList();
        IsLoaded = true;
        if (StatusMessage == TankbookEntryTexts.LoadFailed)
        {
            StatusMessage = null;
        }

        await Statistics.LoadAsync(vehicle.Id).ConfigureAwait(true);
    }

    private void ReportLoadFailed(Exception ex)
    {
        _logger.LogWarning("Die Vorgänge konnten nicht geladen werden ({ExceptionType}).", ex.GetType().Name);
        StatusMessage = TankbookEntryTexts.LoadFailed;
        _announcer.Announce(StatusMessage);
    }

    private void ResetVehicle(long? vehicleId)
    {
        _vehicleId = vehicleId;
        Form.Cancel();
        Statistics.Reset();
        Export.Reset();
        Vehicle = null;
        Entries = [];
        IsLoaded = false;
        StatusMessage = null;
    }

    private void MarkSelectedVehicle(long? vehicleId)
    {
        foreach (var option in VehicleOptions)
        {
            option.SetSelectedSilently(option.Vehicle.Id == vehicleId);
        }
    }

    private void OnVehicleSelected(VehicleOptionViewModel option)
    {
        LastTask = SelectVehicleAsync(option.Vehicle.Id);
    }

    private void Announce(Vehicle? vehicle)
    {
        if (vehicle is not null)
        {
            _announcer.Announce(TankbookTexts.FormatVehicleHint(vehicle.Name));
        }
    }

    private async Task OpenVehicleManagementAsync(bool startCreate)
    {
        IsVehiclePickerOpen = false;
        try
        {
            await _navigator.OpenVehicleManagementAsync(startCreate).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Die Fahrzeugverwaltung konnte nicht geöffnet werden ({ExceptionType}).", ex.GetType().Name);
        }
    }

    private void BeginCreate()
    {
        if (Vehicle is not { } vehicle)
        {
            return;
        }

        CloseConfirmations(null);
        StatusMessage = null;
        Form.BeginCreate(vehicle, Entries.Count > 0 ? Entries[0].Entry.OdometerKm : null);
    }

    private void BeginEdit(TankbookEntryItemViewModel item)
    {
        if (Vehicle is not { } vehicle)
        {
            return;
        }

        CloseConfirmations(null);
        StatusMessage = null;
        Form.BeginEdit(vehicle, item.Entry);
    }

    private void BeginDelete(TankbookEntryItemViewModel item)
    {
        Form.Cancel();
        StatusMessage = null;
        CloseConfirmations(item);
        item.ShowDeleteConfirmation();
        _announcer.Announce(item.DeleteQuestion);
    }

    private async Task DeleteAsync(TankbookEntryItemViewModel item)
    {
        if (IsDeleting)
        {
            return;
        }

        var reload = false;
        IsDeleting = true;
        try
        {
            var result = await _entryService.DeleteEntryAsync(item.Id).ConfigureAwait(true);
            if (result is TankbookEntryResult.Ok or TankbookEntryResult.EntryNotFound)
            {
                item.HideDeleteConfirmation();
                reload = true;
            }
            else
            {
                StatusMessage = TankbookEntryTexts.GetResultMessage(result, null);
                _announcer.Announce(StatusMessage);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Der Vorgang konnte nicht gelöscht werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = TankbookTexts.SaveFailed;
            _announcer.Announce(StatusMessage);
        }
        finally
        {
            IsDeleting = false;
        }

        if (reload)
        {
            await LoadAsync().ConfigureAwait(true);
        }
    }

    private void OnFormSaved(object? sender, EventArgs e)
    {
        LastTask = LoadAsync();
    }

    private void OnRestored(object? sender, EventArgs e)
    {
        _selectionStore.Clear();
        LastTask = LoadAsync();
    }

    private void CloseConfirmations(TankbookEntryItemViewModel? except)
    {
        foreach (var item in Entries)
        {
            if (!ReferenceEquals(item, except))
            {
                item.HideDeleteConfirmation();
            }
        }
    }
}

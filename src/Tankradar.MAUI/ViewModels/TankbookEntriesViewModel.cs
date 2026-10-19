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
/// ViewModel der Seite „Tank- und Ladevorgänge“ eines Fahrzeugs: Fahrzeugkopf, Liste der Vorgänge (neueste zuerst) mit Verbrauch, Formular zum Erfassen und
/// Korrigieren (in der Seite) und Löschen nach einer Rückfrage im Eintrag. Das Fahrzeug wird über die Navigation übergeben, nie über eine angezeigte Kennung.
/// Protokolliert werden nie Namen oder Werte.
/// </summary>
public sealed class TankbookEntriesViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IVehicleService _vehicleService;
    private readonly ITankbookEntryService _entryService;
    private readonly IReceiptPhotoStore _photoStore;
    private readonly ITankbookNavigator _navigator;
    private readonly IScreenReaderAnnouncer _announcer;
    private readonly ILogger<TankbookEntriesViewModel> _logger;
    private long? _vehicleId;
    private Vehicle? _vehicle;
    private IReadOnlyList<TankbookEntryItemViewModel> _entries = [];
    private bool _isLoaded;
    private bool _isDeleting;
    private string? _statusMessage;

    /// <summary>
    /// Erstellt das ViewModel mit dem Seitentitel „Tank- und Ladevorgänge“.
    /// </summary>
    /// <param name="vehicleService">Der Fahrzeugdienst.</param>
    /// <param name="entryService">Der Dienst der Vorgänge.</param>
    /// <param name="photoStore">Die Ablage der Belegfotos (Anzeige der Belege).</param>
    /// <param name="form">Das Formular zum Erfassen und Korrigieren.</param>
    /// <param name="statistics">Der Bereich „Auswertung“.</param>
    /// <param name="export">Der Bereich „Export“.</param>
    /// <param name="restoreNotifier">Meldet, dass die Daten aus einer Sicherung wiederhergestellt wurden; die Seite wird dann neu geladen.</param>
    /// <param name="navigator">Schließt die Seite, wenn das Fahrzeug nicht mehr existiert.</param>
    /// <param name="announcer">Sagt Rückfragen und Meldungen über die Sprachausgabe an.</param>
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
        ILogger<TankbookEntriesViewModel> logger)
    {
        _vehicleService = vehicleService;
        _entryService = entryService;
        _photoStore = photoStore;
        _navigator = navigator;
        _announcer = announcer;
        _logger = logger;
        Form = form;
        Statistics = statistics;
        Export = export;
        restoreNotifier.Restored += OnRestored;
        Form.Saved += OnFormSaved;
        Title = TankbookEntryTexts.PageTitle;
        LastTask = Task.CompletedTask;
        AddEntryCommand = new Command(BeginCreate);
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
    /// Übernimmt die Kennung des Fahrzeugs aus der Shell-Navigation.
    /// </summary>
    /// <param name="query">Die Navigationsparameter.</param>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue(ShellTankbookNavigator.VehicleIdParameter, out var value) && value is long vehicleId)
        {
            Show(vehicleId);
        }
    }

    /// <summary>
    /// Wechselt zu einem Fahrzeug und setzt Liste und Formular zurück.
    /// </summary>
    /// <param name="vehicleId">Die Kennung des Fahrzeugs.</param>
    public void Show(long vehicleId)
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

    /// <summary>
    /// Lädt Fahrzeug und Vorgänge beim Erscheinen der Seite.
    /// </summary>
    public override void OnAppearing()
    {
        LastTask = LoadAsync();
    }

    /// <summary>
    /// Lädt Fahrzeug, Vorgänge und bekannte Tankstellen. Gibt es das Fahrzeug nicht mehr, wird das gemeldet und die Seite geschlossen.
    /// </summary>
    /// <returns>Ein Task, der nach dem Laden abgeschlossen ist.</returns>
    public async Task LoadAsync()
    {
        if (_vehicleId is not { } vehicleId)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var vehicle = await _vehicleService.GetVehicleAsync(vehicleId).ConfigureAwait(true);
            if (vehicle is null)
            {
                Vehicle = null;
                Entries = [];
                Export.SetVehicle(null);
                Form.Cancel();
                StatusMessage = TankbookEntryTexts.VehicleNotFound;
                _announcer.Announce(StatusMessage);
                await _navigator.CloseAsync().ConfigureAwait(true);
                return;
            }

            var overviews = await _entryService.GetEntriesAsync(vehicleId).ConfigureAwait(true);
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

            await Statistics.LoadAsync(vehicleId).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Die Vorgänge konnten nicht geladen werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = TankbookEntryTexts.LoadFailed;
            _announcer.Announce(StatusMessage);
        }
        finally
        {
            IsBusy = false;
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

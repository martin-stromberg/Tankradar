using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services;
using Tankradar.MAUI.Services.Navigation;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// ViewModel für den Bereich „Tankbuch“: Fahrzeugverwaltung mit Liste, Formular zum Anlegen und Bearbeiten (in der Seite, mit feldbezogenen Fehlermeldungen)
/// und Löschen nach einer Rückfrage im jeweiligen Eintrag, die vorhandene Tank- und Ladevorgänge ausdrücklich nennt (beim Bestätigen neu gezählt);
/// je Fahrzeug öffnet „Tank- und Ladevorgänge“ dessen Vorgänge. Protokolliert werden nie Namen oder Werte.
/// </summary>
public class TankbookViewModel : BaseViewModel
{
    private readonly IVehicleService _service;
    private readonly ITankbookNavigator _navigator;
    private readonly ILogger<TankbookViewModel> _logger;
    private readonly IScreenReaderAnnouncer _announcer;
    private readonly Command _saveCommand;
    private bool _isSaving;
    private IReadOnlyList<VehicleItemViewModel> _vehicles = [];
    private bool _isLoaded;
    private bool _isFormOpen;
    private string _formTitle = TankbookTexts.NewVehicleTitle;
    private string _formName = string.Empty;
    private string _formOdometer = string.Empty;
    private string _formConsumption = string.Empty;
    private VehicleFuelKind _selectedFuelKind = VehicleFuelKind.Petrol;
    private string _nameError = string.Empty;
    private string _odometerError = string.Empty;
    private string _consumptionError = string.Empty;
    private string? _statusMessage;

    /// <summary>
    /// Erstellt das ViewModel mit dem Seitentitel „Tankbuch“.
    /// </summary>
    /// <param name="service">Der Fahrzeugdienst.</param>
    /// <param name="navigator">Öffnet die Tank- und Ladevorgänge eines Fahrzeugs.</param>
    /// <param name="logger">Logger (protokolliert nie Namen oder Werte).</param>
    /// <param name="announcer">Sagt erscheinende Rückfragen und Fehlermeldungen über die Sprachausgabe an.</param>
    public TankbookViewModel(IVehicleService service, ITankbookNavigator navigator, ILogger<TankbookViewModel> logger, IScreenReaderAnnouncer announcer)
    {
        _service = service;
        _navigator = navigator;
        _logger = logger;
        _announcer = announcer;
        Title = TankbookTexts.PageTitle;
        FuelKindOptions = new[] { VehicleFuelKind.Petrol, VehicleFuelKind.Diesel, VehicleFuelKind.Electric, VehicleFuelKind.Hybrid }
            .Select(value => new ChoiceOptionViewModel<VehicleFuelKind>(value, TankbookTexts.GetFuelKindLabel(value), OnFuelKindSelected))
            .ToList();
        LastTask = Task.CompletedTask;
        AddVehicleCommand = new Command(BeginCreate);
        _saveCommand = new Command(() => LastTask = SaveAsync(), () => !IsSaving);
        SaveCommand = _saveCommand;
        CancelCommand = new Command(() =>
        {
            IsFormOpen = false;
            ClearFieldErrors();
        });
    }

    /// <summary>
    /// Der zuletzt gestartete Vorgang (für Tests und Abwarten).
    /// </summary>
    public Task LastTask { get; private set; }

    /// <summary>
    /// Gibt an, ob gerade eine Änderung gespeichert oder ein Fahrzeug gelöscht wird; „Speichern“ und die Bestätigung des Löschens sind dann gesperrt.
    /// Unabhängig von <see cref="BaseViewModel.IsBusy"/>, das nur das Laden anzeigt.
    /// </summary>
    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            if (SetProperty(ref _isSaving, value))
            {
                _saveCommand.ChangeCanExecute();
                foreach (var item in Vehicles)
                {
                    item.RefreshCommandStates();
                }
            }
        }
    }

    /// <summary>
    /// Die Fahrzeuge nach Namen geordnet.
    /// </summary>
    public IReadOnlyList<VehicleItemViewModel> Vehicles
    {
        get => _vehicles;
        private set
        {
            if (SetProperty(ref _vehicles, value))
            {
                OnPropertyChanged(nameof(HasVehicles));
                OnPropertyChanged(nameof(ShowEmptyHint));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob es Fahrzeuge gibt.
    /// </summary>
    public bool HasVehicles => Vehicles.Count > 0;

    /// <summary>
    /// Gibt an, ob der Hinweis „Noch keine Fahrzeuge“ angezeigt wird (geladen und leer).
    /// </summary>
    public bool ShowEmptyHint => IsLoaded && Vehicles.Count == 0;

    /// <summary>
    /// Gibt an, ob die Fahrzeuge mindestens einmal geladen wurden.
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
    /// Gibt an, ob das Formular zum Anlegen oder Bearbeiten geöffnet ist.
    /// </summary>
    public bool IsFormOpen
    {
        get => _isFormOpen;
        private set
        {
            if (SetProperty(ref _isFormOpen, value))
            {
                OnPropertyChanged(nameof(IsFormClosed));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob das Formular geschlossen ist.
    /// </summary>
    public bool IsFormClosed => !IsFormOpen;

    /// <summary>
    /// Die Überschrift des Formulars („Neues Fahrzeug“ oder „Fahrzeug bearbeiten“).
    /// </summary>
    public string FormTitle
    {
        get => _formTitle;
        private set => SetProperty(ref _formTitle, value);
    }

    /// <summary>
    /// Die Kennung des bearbeiteten Fahrzeugs; <see langword="null"/> beim Anlegen.
    /// </summary>
    public long? EditingVehicleId { get; private set; }

    /// <summary>
    /// Der im Formular eingegebene Name.
    /// </summary>
    public string FormName
    {
        get => _formName;
        set => SetProperty(ref _formName, value ?? string.Empty);
    }

    /// <summary>
    /// Der im Formular eingegebene Start-Kilometerstand.
    /// </summary>
    public string FormOdometer
    {
        get => _formOdometer;
        set => SetProperty(ref _formOdometer, value ?? string.Empty);
    }

    /// <summary>
    /// Der im Formular eingegebene optionale Startverbrauch.
    /// </summary>
    public string FormConsumption
    {
        get => _formConsumption;
        set => SetProperty(ref _formConsumption, value ?? string.Empty);
    }

    /// <summary>
    /// Die Chips der Kraftstoffart in der Reihenfolge Benzin, Diesel, Elektro, Hybrid.
    /// </summary>
    public IReadOnlyList<ChoiceOptionViewModel<VehicleFuelKind>> FuelKindOptions { get; }

    /// <summary>
    /// Die im Formular gewählte Kraftstoffart.
    /// </summary>
    public VehicleFuelKind SelectedFuelKind
    {
        get => _selectedFuelKind;
        private set
        {
            if (SetProperty(ref _selectedFuelKind, value))
            {
                OnPropertyChanged(nameof(ConsumptionUnit));
                OnPropertyChanged(nameof(ConsumptionPlaceholder));
                OnPropertyChanged(nameof(ConsumptionDescription));
            }
        }
    }

    /// <summary>
    /// Die Einheit des Verbrauchsfelds („L/100 km“ oder „kWh/100 km“).
    /// </summary>
    public string ConsumptionUnit
    {
        get { return TankbookTexts.GetConsumptionUnit(SelectedFuelKind); }
    }

    /// <summary>
    /// Der Platzhalter des Verbrauchsfelds passend zur Kraftstoffart.
    /// </summary>
    public string ConsumptionPlaceholder
    {
        get { return TankbookTexts.GetConsumptionPlaceholder(SelectedFuelKind); }
    }

    /// <summary>
    /// Die Beschreibung des Verbrauchsfelds für die Sprachausgabe (Beschriftung samt Einheit).
    /// </summary>
    public string ConsumptionDescription
    {
        get { return TankbookTexts.FormatConsumptionDescription(SelectedFuelKind); }
    }

    /// <summary>
    /// Die Fehlermeldung am Namen; leer, wenn keine anliegt.
    /// </summary>
    public string NameError
    {
        get => _nameError;
        private set
        {
            if (SetProperty(ref _nameError, value))
            {
                OnPropertyChanged(nameof(HasNameError));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob am Namen ein Fehler anliegt.
    /// </summary>
    public bool HasNameError => NameError.Length > 0;

    /// <summary>
    /// Die Fehlermeldung am Start-Kilometerstand; leer, wenn keine anliegt.
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
    /// Gibt an, ob am Start-Kilometerstand ein Fehler anliegt.
    /// </summary>
    public bool HasOdometerError => OdometerError.Length > 0;

    /// <summary>
    /// Die Fehlermeldung am Startverbrauch; leer, wenn keine anliegt.
    /// </summary>
    public string ConsumptionError
    {
        get => _consumptionError;
        private set
        {
            if (SetProperty(ref _consumptionError, value))
            {
                OnPropertyChanged(nameof(HasConsumptionError));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob am Startverbrauch ein Fehler anliegt.
    /// </summary>
    public bool HasConsumptionError => ConsumptionError.Length > 0;

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
    public bool HasStatusMessage => StatusMessage is { Length: > 0 };

    /// <summary>
    /// Befehl „Fahrzeug hinzufügen“: öffnet das leere Formular mit vorgewähltem Benzin.
    /// </summary>
    public ICommand AddVehicleCommand { get; }

    /// <summary>
    /// Befehl „Speichern“ des Formulars.
    /// </summary>
    public ICommand SaveCommand { get; }

    /// <summary>
    /// Befehl „Abbrechen“ des Formulars.
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// Lädt die Fahrzeuge beim Erscheinen der Seite.
    /// </summary>
    public override void OnAppearing()
    {
        StatusMessage = null;
        LastTask = LoadAsync();
    }

    /// <summary>
    /// Lädt die Fahrzeuge.
    /// </summary>
    /// <returns>Ein Task, der nach dem Laden abgeschlossen ist.</returns>
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var vehicles = await _service.GetVehiclesAsync().ConfigureAwait(true);
            Vehicles = vehicles
                .Select(vehicle => new VehicleItemViewModel(
                    vehicle,
                    BeginEdit,
                    OpenEntries,
                    item => LastTask = BeginDeleteAsync(item),
                    item => LastTask = DeleteAsync(item),
                    () => !IsSaving))
                .ToList();
            IsLoaded = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Fahrzeuge konnten nicht geladen werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = TankbookTexts.LoadFailed;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void BeginCreate()
    {
        EditingVehicleId = null;
        FormName = string.Empty;
        FormOdometer = string.Empty;
        FormConsumption = string.Empty;
        SelectFuelKindSilently(VehicleFuelKind.Petrol);
        ClearFieldErrors();
        StatusMessage = null;
        CloseConfirmations(null);
        FormTitle = TankbookTexts.NewVehicleTitle;
        IsFormOpen = true;
    }

    private void BeginEdit(VehicleItemViewModel item)
    {
        var vehicle = item.Vehicle;
        EditingVehicleId = item.Id;
        FormName = vehicle.Name;
        FormOdometer = TankbookTexts.FormatOdometerInput(vehicle.StartOdometerKm);
        FormConsumption = TankbookTexts.FormatConsumptionInput(vehicle.StartConsumption);
        SelectFuelKindSilently(vehicle.FuelKind);
        ClearFieldErrors();
        StatusMessage = null;
        CloseConfirmations(null);
        FormTitle = TankbookTexts.EditVehicleTitle;
        IsFormOpen = true;
    }

    private void SelectFuelKindSilently(VehicleFuelKind fuelKind)
    {
        foreach (var option in FuelKindOptions)
        {
            option.SetSelectedSilently(option.Value == fuelKind);
        }

        SelectedFuelKind = fuelKind;
    }

    private void OnFuelKindSelected(ChoiceOptionViewModel<VehicleFuelKind> selected)
    {
        foreach (var option in FuelKindOptions)
        {
            if (!ReferenceEquals(option, selected))
            {
                option.SetSelectedSilently(false);
            }
        }

        SelectedFuelKind = selected.Value;
    }

    private async Task SaveAsync()
    {
        if (IsSaving)
        {
            return;
        }

        var validation = VehicleValidator.ValidateForm(FormName, SelectedFuelKind, FormOdometer, FormConsumption);
        NameError = TankbookTexts.GetNameError(validation.NameError);
        OdometerError = TankbookTexts.GetOdometerError(validation.OdometerError);
        ConsumptionError = TankbookTexts.GetConsumptionError(validation.ConsumptionError);
        if (validation.Input is not { } input)
        {
            _announcer.Announce(new[] { NameError, OdometerError, ConsumptionError }.First(error => error.Length > 0));
            return;
        }

        StatusMessage = null;
        var reload = false;
        IsSaving = true;
        try
        {
            var result = EditingVehicleId is { } vehicleId
                ? await _service.UpdateVehicleAsync(vehicleId, input).ConfigureAwait(true)
                : (await _service.CreateVehicleAsync(input).ConfigureAwait(true)).Result;
            switch (result)
            {
                case VehicleResult.Ok:
                    IsFormOpen = false;
                    reload = true;
                    break;
                case VehicleResult.DuplicateName:
                    NameError = TankbookTexts.DuplicateName;
                    _announcer.Announce(NameError);
                    break;
                case VehicleResult.StartOdometerAboveEntries:
                    OdometerError = TankbookTexts.StartOdometerAboveEntries;
                    _announcer.Announce(OdometerError);
                    break;
                case VehicleResult.VehicleNotFound:
                    IsFormOpen = false;
                    StatusMessage = TankbookTexts.GetResultMessage(result);
                    reload = true;
                    break;
                default:
                    StatusMessage = TankbookTexts.GetResultMessage(result);
                    break;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Das Fahrzeug konnte nicht gespeichert werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = TankbookTexts.SaveFailed;
        }
        finally
        {
            IsSaving = false;
        }

        if (reload)
        {
            await LoadAsync().ConfigureAwait(true);
        }
    }

    private async Task BeginDeleteAsync(VehicleItemViewModel item)
    {
        IsFormOpen = false;
        ClearFieldErrors();
        StatusMessage = null;
        CloseConfirmations(item);
        try
        {
            var count = await _service.CountEntriesAsync(item.Id).ConfigureAwait(true);
            item.ShowDeleteConfirmation(count);
            _announcer.Announce(item.DeleteQuestion);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Die Vorgänge des Fahrzeugs konnten nicht gezählt werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = TankbookTexts.LoadFailed;
        }
    }

    private async Task DeleteAsync(VehicleItemViewModel item)
    {
        if (IsSaving)
        {
            return;
        }

        var reload = false;
        IsSaving = true;
        try
        {
            var result = await _service.DeleteVehicleAsync(item.Id, item.ShownEntryCount).ConfigureAwait(true);
            switch (result.Result)
            {
                case VehicleResult.Ok:
                case VehicleResult.VehicleNotFound:
                    item.HideDeleteConfirmation();
                    reload = true;
                    break;
                case VehicleResult.EntryCountChanged:
                    // Inzwischen hinzugekommene Vorgänge werden in der Rückfrage genannt, bevor sie mitgelöscht werden.
                    item.ShowDeleteConfirmation(result.EntryCount);
                    _announcer.Announce(item.DeleteQuestion);
                    break;
                default:
                    StatusMessage = TankbookTexts.GetResultMessage(result.Result);
                    break;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Das Fahrzeug konnte nicht gelöscht werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = TankbookTexts.SaveFailed;
        }
        finally
        {
            IsSaving = false;
        }

        if (reload)
        {
            await LoadAsync().ConfigureAwait(true);
        }
    }

    private void ClearFieldErrors()
    {
        NameError = string.Empty;
        OdometerError = string.Empty;
        ConsumptionError = string.Empty;
    }

    private void CloseConfirmations(VehicleItemViewModel? except)
    {
        foreach (var item in Vehicles)
        {
            if (!ReferenceEquals(item, except))
            {
                item.HideDeleteConfirmation();
            }
        }
    }

    private void OpenEntries(VehicleItemViewModel item)
    {
        IsFormOpen = false;
        ClearFieldErrors();
        CloseConfirmations(null);
        LastTask = _navigator.OpenEntriesAsync(item.Id);
    }
}

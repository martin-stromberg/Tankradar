using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services;
using Tankradar.MAUI.Services.Backup;
using Tankradar.MAUI.Services.Export;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Zustand des Bereichs „Export“ des Tabs „Tankbuch“: Format und Zeitraum wählen, <b>Exportieren</b> erzeugt die Datei und übergibt sie an die
/// Systemfunktion Teilen/Speichern. Ein Export entsteht nur auf Veranlassung des Anwenders. Protokolliert werden nie Namen oder Werte, nur Ausnahmetypen.
/// </summary>
public sealed class TankbookExportViewModel : BaseViewModel
{
    private readonly ITankbookExportService _service;
    private readonly IBackupSharer _sharer;
    private readonly TimeProvider _timeProvider;
    private readonly IScreenReaderAnnouncer _announcer;
    private readonly ILogger<TankbookExportViewModel> _logger;
    private Vehicle? _vehicle;
    private TankbookExportFormat _selectedFormat = TankbookExportFormat.Csv;
    private TankbookExportRangeKind _selectedRange = TankbookExportRangeKind.All;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private DateTime _maximumDate;
    private string? _rangeError;
    private string? _statusMessage;
    private bool _statusIsError;

    /// <summary>
    /// Erstellt das ViewModel.
    /// </summary>
    /// <param name="service">Der Exportdienst.</param>
    /// <param name="sharer">Die Weitergabe über die Systemfunktion Teilen/Speichern (im Testmodus ein Kopieren in ein Testverzeichnis).</param>
    /// <param name="timeProvider">Die Zeitquelle für „heute“.</param>
    /// <param name="announcer">Sagt Meldungen über die Sprachausgabe an.</param>
    /// <param name="logger">Logger (protokolliert nie Namen oder Werte).</param>
    public TankbookExportViewModel(
        ITankbookExportService service,
        IBackupSharer sharer,
        TimeProvider timeProvider,
        IScreenReaderAnnouncer announcer,
        ILogger<TankbookExportViewModel> logger)
    {
        _service = service;
        _sharer = sharer;
        _timeProvider = timeProvider;
        _announcer = announcer;
        _logger = logger;
        Title = TankbookExportTexts.Heading;
        FormatOptions = Enum.GetValues<TankbookExportFormat>()
            .Select(value => new ChoiceOptionViewModel<TankbookExportFormat>(value, TankbookExportTexts.GetFormatLabel(value), OnFormatSelected, ToAutomationKey(value)))
            .ToList();
        RangeOptions = Enum.GetValues<TankbookExportRangeKind>()
            .Select(value => new ChoiceOptionViewModel<TankbookExportRangeKind>(value, TankbookExportTexts.GetRangeLabel(value), OnRangeSelected, ToAutomationKey(value)))
            .ToList();
        LastTask = Task.CompletedTask;
        ExportCommand = new Command(() => LastTask = ExportAsync());
        ResetSelection();
    }

    /// <summary>
    /// Der zuletzt gestartete Vorgang (für Tests und Abwarten).
    /// </summary>
    public Task LastTask { get; private set; }

    /// <summary>
    /// Die wählbaren Formate (CSV, PDF).
    /// </summary>
    public IReadOnlyList<ChoiceOptionViewModel<TankbookExportFormat>> FormatOptions { get; }

    /// <summary>
    /// Die wählbaren Zeiträume.
    /// </summary>
    public IReadOnlyList<ChoiceOptionViewModel<TankbookExportRangeKind>> RangeOptions { get; }

    /// <summary>
    /// Das gewählte Format.
    /// </summary>
    public TankbookExportFormat SelectedFormat
    {
        get { return _selectedFormat; }
        private set
        {
            if (SetProperty(ref _selectedFormat, value))
            {
                OnPropertyChanged(nameof(StartHint));
            }
        }
    }

    /// <summary>
    /// Der gewählte Zeitraum.
    /// </summary>
    public TankbookExportRangeKind SelectedRange
    {
        get { return _selectedRange; }
        private set
        {
            if (SetProperty(ref _selectedRange, value))
            {
                OnPropertyChanged(nameof(IsCustomRange));
                OnPropertyChanged(nameof(StartHint));
                RangeError = null;
            }
        }
    }

    /// <summary>
    /// Gibt an, ob ein eigener Zeitraum gewählt ist; nur dann werden die Datumsfelder angezeigt.
    /// </summary>
    public bool IsCustomRange
    {
        get { return SelectedRange == TankbookExportRangeKind.Custom; }
    }

    /// <summary>
    /// Der erste Tag des eigenen Zeitraums.
    /// </summary>
    public DateTime? FromDate
    {
        get { return _fromDate; }
        set
        {
            if (SetProperty(ref _fromDate, value))
            {
                RangeError = null;
            }
        }
    }

    /// <summary>
    /// Der letzte Tag des eigenen Zeitraums.
    /// </summary>
    public DateTime? ToDate
    {
        get { return _toDate; }
        set
        {
            if (SetProperty(ref _toDate, value))
            {
                RangeError = null;
            }
        }
    }

    /// <summary>
    /// Das späteste wählbare Datum: heute.
    /// </summary>
    public DateTime MaximumDate
    {
        get { return _maximumDate; }
        private set { SetProperty(ref _maximumDate, value); }
    }

    /// <summary>
    /// Der Fehler am eigenen Zeitraum; <see langword="null"/>, wenn keiner anliegt.
    /// </summary>
    public string? RangeError
    {
        get { return _rangeError; }
        private set
        {
            if (SetProperty(ref _rangeError, value))
            {
                OnPropertyChanged(nameof(HasRangeError));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob ein Fehler am Zeitraum anliegt.
    /// </summary>
    public bool HasRangeError
    {
        get { return RangeError is { Length: > 0 }; }
    }

    /// <summary>
    /// Die Rückmeldung zum letzten Export; <see langword="null"/>, wenn keine anliegt.
    /// </summary>
    public string? StatusMessage
    {
        get { return _statusMessage; }
        private set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob eine Rückmeldung anliegt.
    /// </summary>
    public bool HasStatusMessage
    {
        get { return StatusMessage is { Length: > 0 }; }
    }

    /// <summary>
    /// Gibt an, ob die Rückmeldung einen Fehler meldet (rote Darstellung).
    /// </summary>
    public bool StatusIsError
    {
        get { return _statusIsError; }
        private set { SetProperty(ref _statusIsError, value); }
    }

    /// <summary>
    /// Gibt an, ob ein Fahrzeug zugeordnet ist; ohne Fahrzeug (z. B. nach einer Wiederherstellung) wird der Bereich ausgeblendet.
    /// </summary>
    public bool HasVehicle
    {
        get { return _vehicle is not null; }
    }

    /// <summary>
    /// Hinweis von <b>Exportieren</b> für die Sprachausgabe, z. B. „Fahrzeug „Golf“, CSV, Alle Vorgänge“.
    /// </summary>
    public string StartHint
    {
        get { return _vehicle is { } vehicle ? TankbookExportTexts.FormatStartHint(vehicle.Name, SelectedFormat, SelectedRange) : string.Empty; }
    }

    /// <summary>
    /// Befehl <b>Exportieren</b>.
    /// </summary>
    public ICommand ExportCommand { get; }

    /// <summary>
    /// Übernimmt das Fahrzeug der Seite. Wechselt das Fahrzeug, werden die Auswahl zurückgesetzt; eine alte Rückmeldung wird immer entfernt.
    /// Es entsteht keine Datei.
    /// </summary>
    /// <param name="vehicle">Das Fahrzeug; <see langword="null"/>, wenn es nicht (mehr) existiert.</param>
    public void SetVehicle(Vehicle? vehicle)
    {
        var changed = _vehicle?.Id != vehicle?.Id;
        _vehicle = vehicle;
        if (changed)
        {
            ResetSelection();
        }

        MaximumDate = Today();
        if (ToDate is { } to && to.Date > MaximumDate)
        {
            ToDate = MaximumDate;
        }

        ClearMessages();
        OnPropertyChanged(nameof(StartHint));
        OnPropertyChanged(nameof(HasVehicle));
    }

    /// <summary>
    /// Setzt den Bereich zurück (kein Altzustand beim Wechsel des Fahrzeugs).
    /// </summary>
    public void Reset()
    {
        _vehicle = null;
        ResetSelection();
        ClearMessages();
        OnPropertyChanged(nameof(StartHint));
        OnPropertyChanged(nameof(HasVehicle));
    }

    private async Task ExportAsync()
    {
        if (IsBusy || _vehicle is not { } vehicle)
        {
            return;
        }

        IsBusy = true;
        try
        {
            ClearMessages();
            var today = Today();
            var (from, to) = TankbookExportRangeResolver.Resolve(SelectedRange, today, FromDate ?? today, ToDate ?? today);
            var rangeError = TankbookExportRangeResolver.Validate(from, to, today);
            if (rangeError != TankbookExportRangeError.None)
            {
                var message = TankbookExportTexts.GetRangeErrorMessage(rangeError);
                RangeError = message;
                _announcer.Announce(message);
                return;
            }

            var result = await _service.CreateAsync(vehicle.Id, new TankbookExportRequest(SelectedFormat, from, to)).ConfigureAwait(true);
            if (result.Status != TankbookExportStatus.Exported || result.FilePath is null)
            {
                ShowStatus(
                    TankbookExportTexts.GetStatusMessage(result.Status, SelectedRange == TankbookExportRangeKind.All),
                    result.Status is TankbookExportStatus.Failed or TankbookExportStatus.InvalidRange);
                return;
            }

            await ShareAsync(result).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Der Export ist fehlgeschlagen ({ExceptionType}).", ex.GetType().Name);
            ShowStatus(TankbookExportTexts.CreateFailed, isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ShareAsync(TankbookExportResult result)
    {
        try
        {
            await _sharer.ShareAsync(result.FilePath!, TankbookExportTexts.ShareTitle).ConfigureAwait(true);
            ShowStatus(TankbookExportTexts.FormatReady(result.FileName!), isError: false);
        }
        catch (OperationCanceledException)
        {
            // Der Anwender hat den Dialog Teilen/Speichern abgebrochen; das ist kein Fehler.
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Der Export konnte nicht weitergegeben werden ({ExceptionType}).", ex.GetType().Name);
            ShowStatus(TankbookExportTexts.ShareFailed, isError: true);
        }
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusIsError = isError;
        StatusMessage = message;
        _announcer.Announce(message);
    }

    private void ClearMessages()
    {
        StatusMessage = null;
        StatusIsError = false;
        RangeError = null;
    }

    private void ResetSelection()
    {
        var today = Today();
        foreach (var option in FormatOptions)
        {
            option.SetSelectedSilently(option.Value == TankbookExportFormat.Csv);
        }

        foreach (var option in RangeOptions)
        {
            option.SetSelectedSilently(option.Value == TankbookExportRangeKind.All);
        }

        SelectedFormat = TankbookExportFormat.Csv;
        SelectedRange = TankbookExportRangeKind.All;
        MaximumDate = today;
        FromDate = today.AddMonths(-1);
        ToDate = today;
    }

    private static string ToAutomationKey(Enum value)
    {
        var name = value.ToString();
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    private DateTime Today()
    {
        return _timeProvider.GetLocalNow().DateTime.Date;
    }

    private void OnFormatSelected(ChoiceOptionViewModel<TankbookExportFormat> selected)
    {
        foreach (var option in FormatOptions)
        {
            if (!ReferenceEquals(option, selected))
            {
                option.SetSelectedSilently(false);
            }
        }

        SelectedFormat = selected.Value;
    }

    private void OnRangeSelected(ChoiceOptionViewModel<TankbookExportRangeKind> selected)
    {
        foreach (var option in RangeOptions)
        {
            if (!ReferenceEquals(option, selected))
            {
                option.SetSelectedSilently(false);
            }
        }

        SelectedRange = selected.Value;
    }
}

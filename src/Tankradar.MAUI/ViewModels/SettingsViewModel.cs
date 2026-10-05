using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// ViewModel für den Bereich „Optionen“: lädt die Einstellungen und speichert jede Änderung sofort.
/// </summary>
public class SettingsViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SettingsViewModel> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _isLoading;
    private string? _statusMessage;
    private bool _isLoaded;

    /// <summary>
    /// Erstellt ein neues <see cref="SettingsViewModel"/> mit dem Seitentitel „Optionen“.
    /// </summary>
    /// <param name="settingsService">Dienst zum Laden und Speichern der Einstellungen.</param>
    /// <param name="logger">Logger für Lade- und Speicherfehler.</param>
    /// <param name="dataSource">Die Karte „Datenquelle“ (Quellenangabe und Preisdienst-Prüfung); optional, damit die Einstellungen auch ohne Preisdienst nutzbar sind.</param>
    public SettingsViewModel(ISettingsService settingsService, ILogger<SettingsViewModel> logger, DataSourceViewModel? dataSource = null)
    {
        _settingsService = settingsService;
        _logger = logger;
        DataSource = dataSource;
        Title = "Optionen";

        var defaults = AppSettings.CreateDefault();
        ApplyFuelTypes(defaults.FuelTypes);
        GpsOptions = CreateOptions<GpsUsage>(SettingsTexts.GetLabel);
        ViewOptions = CreateOptions<ResultView>(SettingsTexts.GetLabel);
        SortOptions = CreateOptions<ResultSortOrder>(SettingsTexts.GetLabel);
        ApplySelection(GpsOptions, defaults.GpsUsage);
        ApplySelection(ViewOptions, defaults.ResultView);
        ApplySelection(SortOptions, defaults.ResultSortOrder);
        LastSaveTask = Task.CompletedTask;
    }

    /// <summary>
    /// Die Karte „Datenquelle“; <see langword="null"/>, wenn kein Preisdienst eingebunden ist.
    /// </summary>
    public DataSourceViewModel? DataSource { get; }

    /// <summary>
    /// Gibt an, ob die Karte „Datenquelle“ angezeigt wird.
    /// </summary>
    public bool HasDataSource => DataSource is not null;

    /// <summary>
    /// Die Spritsorten in der aktuellen Reihenfolge.
    /// </summary>
    public ObservableCollection<FuelTypeItemViewModel> FuelTypes { get; } = [];

    /// <summary>
    /// Die Optionen der Standortnutzung.
    /// </summary>
    public IReadOnlyList<ChoiceOptionViewModel<GpsUsage>> GpsOptions { get; }

    /// <summary>
    /// Die Optionen der Standardansicht.
    /// </summary>
    public IReadOnlyList<ChoiceOptionViewModel<ResultView>> ViewOptions { get; }

    /// <summary>
    /// Die Optionen der Standardsortierung.
    /// </summary>
    public IReadOnlyList<ChoiceOptionViewModel<ResultSortOrder>> SortOptions { get; }

    /// <summary>
    /// Eine Hinweis- oder Fehlermeldung für den Anwender; <see langword="null"/>, wenn keine anliegt.
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
    /// Gibt an, ob der erste Ladevorgang (erfolgreich oder fehlgeschlagen) abgeschlossen ist; dient auch als UI-Signal für Tests.
    /// </summary>
    public bool IsLoaded
    {
        get => _isLoaded;
        private set => SetProperty(ref _isLoaded, value);
    }

    /// <summary>
    /// Der zuletzt gestartete Speichervorgang (für Tests und Abwarten).
    /// </summary>
    public Task LastSaveTask { get; private set; }

    /// <summary>
    /// Überschrift der Spritsorten-Karte.
    /// </summary>
    public string FuelTypesHeading => SettingsTexts.FuelTypesHeading;

    /// <summary>
    /// Hinweis der Spritsorten-Karte.
    /// </summary>
    public string FuelTypesHint => SettingsTexts.FuelTypesHint;

    /// <summary>
    /// Überschrift der Standort-Karte.
    /// </summary>
    public string GpsHeading => SettingsTexts.GpsHeading;

    /// <summary>
    /// Hinweis der Standort-Karte.
    /// </summary>
    public string GpsHint => SettingsTexts.GpsHint;

    /// <summary>
    /// Überschrift der Ansicht-Karte.
    /// </summary>
    public string ViewHeading => SettingsTexts.ViewHeading;

    /// <summary>
    /// Überschrift der Sortierungs-Karte.
    /// </summary>
    public string SortHeading => SettingsTexts.SortHeading;

    /// <summary>
    /// Lädt die Einstellungen, sobald die Seite erscheint.
    /// </summary>
    public override void OnAppearing()
    {
        DataSource?.OnAppearing();
        _ = LoadAsync();
    }

    /// <summary>
    /// Lädt die gespeicherten Einstellungen und zeigt sie an, ohne dabei zu speichern.
    /// </summary>
    /// <returns>Ein Task, der nach dem Laden abgeschlossen ist.</returns>
    public async Task LoadAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(true);
        IsBusy = true;
        try
        {
            var settings = await _settingsService.LoadAsync().ConfigureAwait(true);
            _isLoading = true;
            try
            {
                ApplyFuelTypes(settings.FuelTypes);
                ApplySelection(GpsOptions, settings.GpsUsage);
                ApplySelection(ViewOptions, settings.ResultView);
                ApplySelection(SortOptions, settings.ResultSortOrder);
            }
            finally
            {
                _isLoading = false;
            }

            StatusMessage = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Die Einstellungen konnten nicht geladen werden.");
            StatusMessage = SettingsTexts.LoadFailed;
        }
        finally
        {
            IsLoaded = true;
            IsBusy = false;
            _gate.Release();
        }
    }

    private List<ChoiceOptionViewModel<TValue>> CreateOptions<TValue>(Func<TValue, string> labelOf)
        where TValue : struct, Enum
    {
        var options = new List<ChoiceOptionViewModel<TValue>>();
        foreach (var value in Enum.GetValues<TValue>())
        {
            options.Add(new ChoiceOptionViewModel<TValue>(value, labelOf(value), selected => OnOptionSelected(selected, options)));
        }

        return options;
    }

    private void ApplySelection<TValue>(IEnumerable<ChoiceOptionViewModel<TValue>> options, TValue selected)
        where TValue : struct, Enum
    {
        foreach (var option in options)
        {
            option.SetSelectedSilently(EqualityComparer<TValue>.Default.Equals(option.Value, selected));
        }
    }

    private void ApplyFuelTypes(IReadOnlyList<FuelTypeSelection> selections)
    {
        FuelTypes.Clear();
        foreach (var selection in selections)
        {
            FuelTypes.Add(new FuelTypeItemViewModel(selection.FuelType, selection.IsSelected, OnFuelTypeSelectionChanged, OnFuelTypeMoved));
        }

        UpdateMoveFlags();
    }

    private void UpdateMoveFlags()
    {
        for (var index = 0; index < FuelTypes.Count; index++)
        {
            FuelTypes[index].CanMoveUp = index > 0;
            FuelTypes[index].CanMoveDown = index < FuelTypes.Count - 1;
        }
    }

    private void OnOptionSelected<TValue>(ChoiceOptionViewModel<TValue> selected, IEnumerable<ChoiceOptionViewModel<TValue>> group)
        where TValue : struct, Enum
    {
        if (_isLoading)
        {
            return;
        }

        foreach (var option in group.Where(option => !ReferenceEquals(option, selected)))
        {
            option.SetSelectedSilently(false);
        }

        LastSaveTask = SaveAsync();
    }

    private void OnFuelTypeSelectionChanged(FuelTypeItemViewModel item)
    {
        if (_isLoading)
        {
            return;
        }

        if (!item.IsSelected && !FuelTypes.Any(other => other.IsSelected))
        {
            _isLoading = true;
            try
            {
                item.IsSelected = true;
            }
            finally
            {
                _isLoading = false;
            }

            StatusMessage = SettingsTexts.LastFuelTypeRequired;
            return;
        }

        LastSaveTask = SaveAsync();
    }

    private void OnFuelTypeMoved(FuelTypeItemViewModel item, int direction)
    {
        var index = FuelTypes.IndexOf(item);
        var target = index + direction;
        if (index < 0 || target < 0 || target >= FuelTypes.Count)
        {
            return;
        }

        FuelTypes.Move(index, target);
        UpdateMoveFlags();
        LastSaveTask = SaveAsync();
    }

    private async Task SaveAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(true);
        try
        {
            var settings = new AppSettings(
                FuelTypes.Select(item => new FuelTypeSelection(item.FuelType, item.IsSelected)).ToList(),
                GpsOptions.First(option => option.IsSelected).Value,
                ViewOptions.First(option => option.IsSelected).Value,
                SortOptions.First(option => option.IsSelected).Value);

            await _settingsService.SaveAsync(settings).ConfigureAwait(true);
            StatusMessage = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Die Einstellung konnte nicht gespeichert werden.");
            StatusMessage = SettingsTexts.SaveFailed;
        }
        finally
        {
            _gate.Release();
        }
    }
}

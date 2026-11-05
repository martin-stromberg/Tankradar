using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// ViewModel der Einrichtung beim ersten Start: Auswahl der Spritsorten (Standard: alle), Speichern oder Überspringen über
/// <see cref="ISettingsService"/>. Nach erfolgreichem Abschluss wird <see cref="Completed"/> ausgelöst.
/// </summary>
public class SetupViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SetupViewModel> _logger;
    private string? _statusMessage;

    /// <summary>
    /// Erstellt das ViewModel mit allen Spritsorten in der Standardreihenfolge, alle ausgewählt.
    /// </summary>
    /// <param name="settingsService">Dienst zum Speichern der Einstellungen.</param>
    /// <param name="logger">Logger für Speicherfehler.</param>
    public SetupViewModel(ISettingsService settingsService, ILogger<SetupViewModel> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
        Title = SetupTexts.Heading;

        foreach (var selection in AppSettings.CreateDefault().FuelTypes)
        {
            // Die Auswahl wird erst beim Speichern ausgewertet; Verschiebe-Schaltflächen bietet die Einrichtung nicht an (Reihenfolge in den Optionen).
            FuelTypes.Add(new FuelTypeItemViewModel(selection.FuelType, selection.IsSelected, static _ => { }, static (_, _) => { }));
        }

        SaveCommand = new Command(() => _ = SaveAsync());
        SkipCommand = new Command(() => _ = SkipAsync());
    }

    /// <summary>
    /// Wird ausgelöst, nachdem die Einrichtung gespeichert oder übersprungen wurde.
    /// </summary>
    public event EventHandler? Completed;

    /// <summary>
    /// Die Spritsorten in der Standardreihenfolge mit ihrem Auswahlzustand.
    /// </summary>
    public ObservableCollection<FuelTypeItemViewModel> FuelTypes { get; } = [];

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
    /// Speichert die Auswahl (siehe <see cref="SaveAsync"/>).
    /// </summary>
    public ICommand SaveCommand { get; }

    /// <summary>
    /// Überspringt die Einrichtung (siehe <see cref="SkipAsync"/>).
    /// </summary>
    public ICommand SkipCommand { get; }

    /// <summary>
    /// Speichert die ausgewählten Spritsorten; ohne Auswahl erscheint eine Meldung und es wird nichts gespeichert.
    /// </summary>
    /// <returns>Ein Task, der nach dem Speicherversuch abgeschlossen ist.</returns>
    public Task SaveAsync()
    {
        if (!FuelTypes.Any(item => item.IsSelected))
        {
            StatusMessage = SetupTexts.FuelTypeRequired;
            return Task.CompletedTask;
        }

        var selections = FuelTypes.Select(item => new FuelTypeSelection(item.FuelType, item.IsSelected)).ToList();
        return CompleteAsync(AppSettings.CreateDefault() with { FuelTypes = selections });
    }

    /// <summary>
    /// Überspringt die Einrichtung und speichert die Standardeinstellungen (alle Spritsorten).
    /// </summary>
    /// <returns>Ein Task, der nach dem Speicherversuch abgeschlossen ist.</returns>
    public Task SkipAsync()
    {
        return CompleteAsync(AppSettings.CreateDefault());
    }

    private async Task CompleteAsync(AppSettings settings)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _settingsService.SaveAsync(settings).ConfigureAwait(true);
            StatusMessage = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Die Einrichtung konnte nicht gespeichert werden.");
            StatusMessage = SetupTexts.SaveFailed;
            return;
        }
        finally
        {
            IsBusy = false;
        }

        Completed?.Invoke(this, EventArgs.Empty);
    }
}

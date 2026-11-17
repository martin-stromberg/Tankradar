using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Diagnostics;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// ViewModel der Karte „Diagnose“ in den Optionen: zeigt Zweck und Inhalt des Diagnosepakets und erzeugt es erst auf ausdrückliche Benutzeraktion.
/// Das Teilen erfolgt als eigener, weiterer Schritt über die Systemweitergabe.
/// </summary>
public class DiagnosticsViewModel : BaseViewModel
{
    private readonly IDiagnosticsPackageService _packageService;
    private readonly IDiagnosticsSharer _sharer;
    private readonly ILogger<DiagnosticsViewModel> _logger;
    private string? _statusMessage;
    private string? _previewText;

    /// <summary>
    /// Erstellt das ViewModel.
    /// </summary>
    /// <param name="packageService">Der Dienst zum Erzeugen des Pakets und seiner Vorschau.</param>
    /// <param name="sharer">Die Weitergabe über die Systemfunktion.</param>
    /// <param name="logger">Logger.</param>
    public DiagnosticsViewModel(IDiagnosticsPackageService packageService, IDiagnosticsSharer sharer, ILogger<DiagnosticsViewModel> logger)
    {
        _packageService = packageService;
        _sharer = sharer;
        _logger = logger;
        PreviewCommand = new Command(() => LastTask = PreviewAsync(), () => IsNotBusy);
        ShareCommand = new Command(() => LastTask = ShareAsync(), () => IsNotBusy && HasPreview);
        LastTask = Task.CompletedTask;
    }

    /// <summary>
    /// Der zuletzt gestartete Vorgang (für Tests und Abwarten).
    /// </summary>
    public Task LastTask { get; private set; }

    /// <summary>
    /// Überschrift der Karte.
    /// </summary>
    public string Heading => DiagnosticsTexts.Heading;

    /// <summary>
    /// Erklärtext der Karte.
    /// </summary>
    public string Hint => DiagnosticsTexts.Hint;

    /// <summary>
    /// Hinweis, was nicht enthalten ist.
    /// </summary>
    public string PrivacyHint => DiagnosticsTexts.PrivacyHint;

    /// <summary>
    /// Beschriftung der Vorschau-Schaltfläche.
    /// </summary>
    public string PreviewButtonText => DiagnosticsTexts.PreviewText;

    /// <summary>
    /// Beschriftung der Teilen-Schaltfläche.
    /// </summary>
    public string ShareButtonText => DiagnosticsTexts.ShareText;

    /// <summary>
    /// Überschrift der Inhaltsvorschau.
    /// </summary>
    public string PreviewHeading => DiagnosticsTexts.PreviewHeading;

    /// <summary>
    /// Erzeugt die Inhaltsvorschau, ohne ein Paket zu schreiben.
    /// </summary>
    public ICommand PreviewCommand { get; }

    /// <summary>
    /// Erzeugt das Paket und öffnet die Systemweitergabe; nur nach einer erzeugten Vorschau aktiv.
    /// </summary>
    public ICommand ShareCommand { get; }

    /// <summary>
    /// Die sichtbare Inhaltsvorschau; <see langword="null"/>, bevor sie erzeugt wurde.
    /// </summary>
    public string? PreviewText
    {
        get => _previewText;
        private set
        {
            if (SetProperty(ref _previewText, value))
            {
                OnPropertyChanged(nameof(HasPreview));
                ((Command)ShareCommand).ChangeCanExecute();
            }
        }
    }

    /// <summary>
    /// Gibt an, ob eine Vorschau angezeigt wird.
    /// </summary>
    public bool HasPreview => PreviewText is { Length: > 0 };

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

    private async Task PreviewAsync()
    {
        IsBusy = true;
        try
        {
            PreviewText = await _packageService.CreatePreviewTextAsync().ConfigureAwait(true);
            StatusMessage = null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Die Diagnosevorschau konnte nicht erzeugt werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = DiagnosticsTexts.FailedMessage;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ShareAsync()
    {
        IsBusy = true;
        try
        {
            var fullPath = await _packageService.CreatePackageAsync().ConfigureAwait(true);
            await _sharer.ShareAsync(fullPath, DiagnosticsTexts.ShareTitle).ConfigureAwait(true);
            StatusMessage = DiagnosticsTexts.CreatedMessage;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Das Diagnosepaket konnte nicht erstellt oder geteilt werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = DiagnosticsTexts.FailedMessage;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

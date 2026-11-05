using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Pricing;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// ViewModel der Karte „Datenquelle“ in den Optionen: Quellenangabe, Schlüsselstatus und Prüfung der Verbindung zum Preisdienst.
/// </summary>
public class DataSourceViewModel : BaseViewModel
{
    private readonly IFuelPriceService _priceService;
    private readonly IApiKeyProvider _apiKeyProvider;
    private readonly ILogger<DataSourceViewModel> _logger;
    private string _keyStatus = string.Empty;
    private string? _checkStatus;

    /// <summary>
    /// Erstellt das ViewModel.
    /// </summary>
    /// <param name="priceService">Der Preisdienst.</param>
    /// <param name="apiKeyProvider">Liefert den API-Schlüssel (nur zur Statusanzeige; der Schlüssel wird nie angezeigt).</param>
    /// <param name="logger">Logger.</param>
    public DataSourceViewModel(IFuelPriceService priceService, IApiKeyProvider apiKeyProvider, ILogger<DataSourceViewModel> logger)
    {
        _priceService = priceService;
        _apiKeyProvider = apiKeyProvider;
        _logger = logger;
        CheckCommand = new Command(() => LastCheckTask = CheckAsync());
        LastCheckTask = Task.CompletedTask;
    }

    /// <summary>
    /// Überschrift der Karte.
    /// </summary>
    public string Heading => PriceTexts.DataSourceHeading;

    /// <summary>
    /// Die Quellenangabe gemäß CC BY 4.0.
    /// </summary>
    public string Attribution => PriceTexts.Attribution;

    /// <summary>
    /// Erläuterung zur Lizenz.
    /// </summary>
    public string License => PriceTexts.AttributionLicense;

    /// <summary>
    /// Beschriftung der Prüfschaltfläche.
    /// </summary>
    public string CheckButtonText => PriceTexts.CheckButton;

    /// <summary>
    /// Befehl zum Prüfen der Verbindung zum Preisdienst.
    /// </summary>
    public ICommand CheckCommand { get; }

    /// <summary>
    /// Die zuletzt gestartete Prüfung (für Tests).
    /// </summary>
    public Task LastCheckTask { get; private set; }

    /// <summary>
    /// Status des API-Schlüssels („hinterlegt“ oder „nicht hinterlegt“).
    /// </summary>
    public string KeyStatus
    {
        get => _keyStatus;
        private set => SetProperty(ref _keyStatus, value);
    }

    /// <summary>
    /// Ergebnis der letzten Verbindungsprüfung; <see langword="null"/>, solange keine stattgefunden hat.
    /// </summary>
    public string? CheckStatus
    {
        get => _checkStatus;
        private set
        {
            if (SetProperty(ref _checkStatus, value))
            {
                OnPropertyChanged(nameof(HasCheckStatus));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob ein <see cref="CheckStatus"/> vorliegt.
    /// </summary>
    public bool HasCheckStatus => CheckStatus is { Length: > 0 };

    /// <summary>
    /// Aktualisiert die Anzeige des Schlüsselstatus.
    /// </summary>
    /// <returns>Ein Task, der nach der Aktualisierung abgeschlossen ist.</returns>
    public async Task RefreshKeyStatusAsync()
    {
        try
        {
            var key = await _apiKeyProvider.GetApiKeyAsync().ConfigureAwait(true);
            KeyStatus = string.IsNullOrEmpty(key) ? PriceTexts.KeyMissing : PriceTexts.KeyPresent;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Der Status des API-Schlüssels konnte nicht ermittelt werden.");
            KeyStatus = PriceTexts.KeyMissing;
        }
    }

    /// <summary>
    /// Prüft die Verbindung zum Preisdienst und zeigt das Ergebnis an.
    /// </summary>
    /// <returns>Ein Task, der nach der Prüfung abgeschlossen ist.</returns>
    public async Task CheckAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        CheckStatus = PriceTexts.CheckRunning;
        try
        {
            var failure = await _priceService.CheckAvailabilityAsync().ConfigureAwait(true);
            CheckStatus = PriceTexts.GetCheckMessage(failure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Die Prüfung des Preisdienstes ist fehlgeschlagen.");
            CheckStatus = PriceTexts.GetCheckMessage(PriceFailure.Unreachable);
        }
        finally
        {
            IsBusy = false;
        }

        await RefreshKeyStatusAsync().ConfigureAwait(true);
    }

    /// <inheritdoc />
    public override void OnAppearing()
    {
        _ = RefreshKeyStatusAsync();
    }
}

using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Navigation;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.MAUI.Services.Routing;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// ViewModel der Karte „Routensuche“ in den Optionen: verdecktes Schlüsselfeld, Speichern, Prüfen per Testanfrage, Entfernen und der Schalter „Routensuche aktivieren“,
/// der nur bei gespeichertem Schlüssel bedienbar ist. Der Schlüssel wird nie angezeigt, protokolliert oder in Meldungen übernommen; das Eingabefeld wird nach dem Speichern
/// und beim Verlassen der Seite geleert.
/// </summary>
public class RouteSearchSettingsViewModel : BaseViewModel
{
    private readonly IRouteSearchAccess _access;
    private readonly IRoutingKeyChecker _checker;
    private readonly IConnectionMonitor _connection;
    private readonly ILinkOpener _linkOpener;
    private readonly ILogger<RouteSearchSettingsViewModel> _logger;
    private string _keyInput = string.Empty;
    private bool _hasStoredKey;
    private bool _isEnabled;
    private bool _isSyncing;
    private bool _isChecking;
    private string? _statusMessage;
    private CancellationTokenSource? _checkCancellation;

    /// <summary>
    /// Erstellt das ViewModel.
    /// </summary>
    /// <param name="access">Der Zugang zu Schlüssel und Schalter der Routensuche.</param>
    /// <param name="checker">Die Schlüsselprüfung.</param>
    /// <param name="connection">Die Verbindungserkennung (ohne Verbindung keine Prüfung).</param>
    /// <param name="linkOpener">Öffnet die Anmeldeseite von OpenRouteService.</param>
    /// <param name="logger">Logger (protokolliert nie den Schlüssel).</param>
    /// <param name="maskKeyInput">Gibt an, ob das Schlüsselfeld verdeckt ist (Standard). Nur im Testmodus ist es sichtbar, weil die Bedienung per UI-Automation ein verdecktes Passwortfeld nicht beschreiben kann.</param>
    public RouteSearchSettingsViewModel(
        IRouteSearchAccess access,
        IRoutingKeyChecker checker,
        IConnectionMonitor connection,
        ILinkOpener linkOpener,
        ILogger<RouteSearchSettingsViewModel> logger,
        bool maskKeyInput = true)
    {
        IsKeyMasked = maskKeyInput;
        _access = access;
        _checker = checker;
        _connection = connection;
        _linkOpener = linkOpener;
        _logger = logger;
        SaveKeyCommand = new Command(() => LastActionTask = SaveKeyAsync());
        CheckKeyCommand = new Command(() => LastActionTask = CheckKeyAsync());
        RemoveKeyCommand = new Command(() => LastActionTask = RemoveKeyAsync());
        OpenSignupCommand = new Command(() => LastActionTask = OpenSignupAsync());
        LastActionTask = Task.CompletedTask;
    }

    /// <summary>
    /// Überschrift der Karte.
    /// </summary>
    public string Heading => RouteSettingsTexts.Heading;

    /// <summary>
    /// Erläuterung zur Karte.
    /// </summary>
    public string Hint => RouteSettingsTexts.Hint;

    /// <summary>
    /// Hinweis, wo man einen Schlüssel erhält.
    /// </summary>
    public string SignupHint => RouteSettingsTexts.SignupHint;

    /// <summary>
    /// Beschriftung der Schaltfläche zur Anmeldeseite.
    /// </summary>
    public string SignupButtonText => RouteSettingsTexts.SignupButton;

    /// <summary>
    /// Platzhalter des Schlüsselfelds.
    /// </summary>
    public string KeyPlaceholder => RouteSettingsTexts.KeyPlaceholder;

    /// <summary>
    /// Beschriftung der Schaltfläche zum Speichern.
    /// </summary>
    public string SaveButtonText => RouteSettingsTexts.SaveButton;

    /// <summary>
    /// Beschriftung der Schaltfläche zum Prüfen.
    /// </summary>
    public string CheckButtonText => RouteSettingsTexts.CheckButton;

    /// <summary>
    /// Beschriftung der Schaltfläche zum Entfernen.
    /// </summary>
    public string RemoveButtonText => RouteSettingsTexts.RemoveButton;

    /// <summary>
    /// Beschriftung des Schalters.
    /// </summary>
    public string EnableLabel => RouteSettingsTexts.EnableLabel;

    /// <summary>
    /// Hinweis am Schalter, solange kein Schlüssel gespeichert ist; sonst leer.
    /// </summary>
    public string EnableHint => _hasStoredKey ? string.Empty : RouteSettingsTexts.EnableNeedsKey;

    /// <summary>
    /// Gibt an, ob das Schlüsselfeld die Eingabe verdeckt (Passwortfeld).
    /// </summary>
    public bool IsKeyMasked { get; }

    /// <summary>
    /// Der eingegebene, noch nicht gespeicherte Schlüssel. Er wird nur im Arbeitsspeicher gehalten, nie angezeigt (das Feld ist verdeckt) und nie protokolliert.
    /// </summary>
    public string KeyInput
    {
        get => _keyInput;
        set
        {
            if (SetProperty(ref _keyInput, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(HasKeyInput));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob das Schlüsselfeld Text enthält.
    /// </summary>
    public bool HasKeyInput => _keyInput.Length > 0;

    /// <summary>
    /// Gibt an, ob ein Schlüssel gespeichert ist.
    /// </summary>
    public bool HasStoredKey
    {
        get => _hasStoredKey;
        private set
        {
            if (SetProperty(ref _hasStoredKey, value))
            {
                OnPropertyChanged(nameof(KeyStatus));
                OnPropertyChanged(nameof(EnableHint));
                OnPropertyChanged(nameof(HasEnableHint));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob der Hinweis am Schalter anzuzeigen ist.
    /// </summary>
    public bool HasEnableHint => !_hasStoredKey;

    /// <summary>
    /// Der Status des Schlüssels („gespeichert“ oder „nicht gespeichert“); der Schlüssel selbst wird nie angezeigt.
    /// </summary>
    public string KeyStatus => _hasStoredKey ? RouteSettingsTexts.KeyPresent : RouteSettingsTexts.KeyMissing;

    /// <summary>
    /// Der Zustand des Schalters „Routensuche aktivieren“; eine Änderung durch den Anwender wird sofort gespeichert. Ohne gespeicherten Schlüssel bleibt er aus.
    /// </summary>
    public bool IsRouteSearchEnabled
    {
        get => _isEnabled;
        set
        {
            if (!SetProperty(ref _isEnabled, value) || _isSyncing)
            {
                return;
            }

            LastActionTask = ApplyEnabledAsync(value);
        }
    }

    /// <summary>
    /// Gibt an, ob gerade eine Schlüsselprüfung läuft.
    /// </summary>
    public bool IsChecking
    {
        get => _isChecking;
        private set
        {
            if (SetProperty(ref _isChecking, value))
            {
                OnPropertyChanged(nameof(CanUseButtons));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob die Schaltflächen bedienbar sind (nicht während einer Prüfung).
    /// </summary>
    public bool CanUseButtons => !_isChecking;

    /// <summary>
    /// Die Rückmeldung zur letzten Aktion (Speichern, Prüfen, Entfernen); <see langword="null"/>, wenn keine anliegt.
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
    /// Befehl zum Speichern des eingegebenen Schlüssels.
    /// </summary>
    public ICommand SaveKeyCommand { get; }

    /// <summary>
    /// Befehl zum Prüfen des eingegebenen (sonst des gespeicherten) Schlüssels per Testanfrage.
    /// </summary>
    public ICommand CheckKeyCommand { get; }

    /// <summary>
    /// Befehl zum Entfernen des gespeicherten Schlüssels.
    /// </summary>
    public ICommand RemoveKeyCommand { get; }

    /// <summary>
    /// Befehl zum Öffnen der Anmeldeseite von OpenRouteService.
    /// </summary>
    public ICommand OpenSignupCommand { get; }

    /// <summary>
    /// Die zuletzt gestartete Aktion (für Tests und Abwarten).
    /// </summary>
    public Task LastActionTask { get; private set; }

    /// <inheritdoc />
    public override void OnAppearing()
    {
        LastActionTask = RefreshAsync();
    }

    /// <summary>
    /// Leert beim Verlassen der Seite das Schlüsselfeld, damit kein Schlüssel im Eingabefeld zurückbleibt.
    /// </summary>
    public override void OnDisappearing()
    {
        _checkCancellation?.Cancel();
        KeyInput = string.Empty;
    }

    /// <summary>
    /// Liest den Zustand von Schlüssel und Schalter neu ein.
    /// </summary>
    /// <returns>Ein Task, der nach dem Einlesen abgeschlossen ist.</returns>
    public async Task RefreshAsync()
    {
        var hasKey = await _access.HasKeyAsync().ConfigureAwait(true);
        var enabled = hasKey && await _access.IsEnabledAsync().ConfigureAwait(true);
        _isSyncing = true;
        try
        {
            HasStoredKey = hasKey;
            IsRouteSearchEnabled = enabled;
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private async Task SaveKeyAsync()
    {
        var error = RoutingKeyFormat.Validate(_keyInput, out var key);
        if (error != RoutingKeyError.None)
        {
            StatusMessage = RouteSettingsTexts.GetKeyErrorMessage(error);
            return;
        }

        try
        {
            await _access.SaveKeyAsync(key).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Der Routing-Schlüssel konnte nicht gespeichert werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = RouteSettingsTexts.SaveFailed;
            return;
        }

        KeyInput = string.Empty;
        await RefreshAsync().ConfigureAwait(true);
        StatusMessage = RouteSettingsTexts.Saved;
    }

    private async Task RemoveKeyAsync()
    {
        try
        {
            await _access.RemoveKeyAsync().ConfigureAwait(true);
            StatusMessage = RouteSettingsTexts.Removed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Der Routing-Schlüssel konnte nicht entfernt werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = RouteSettingsTexts.RemoveFailed;
        }

        await RefreshAsync().ConfigureAwait(true);
    }

    private async Task CheckKeyAsync()
    {
        if (_isChecking)
        {
            return;
        }

        // Sofort sperren: Ein Doppeltipp darf nicht zwei Anfragen auslösen.
        IsChecking = true;
        try
        {
            await CheckCoreAsync().ConfigureAwait(true);
        }
        finally
        {
            IsChecking = false;
        }
    }

    private async Task CheckCoreAsync()
    {
        var isEntered = _keyInput.Trim().Length > 0;
        string? candidate;
        if (isEntered)
        {
            var error = RoutingKeyFormat.Validate(_keyInput, out var entered);
            if (error != RoutingKeyError.None)
            {
                StatusMessage = RouteSettingsTexts.GetKeyErrorMessage(error);
                return;
            }

            candidate = entered;
        }
        else
        {
            candidate = await _access.GetStoredKeyAsync().ConfigureAwait(true);
        }

        if (candidate is null)
        {
            StatusMessage = RouteSettingsTexts.NothingToCheck;
            return;
        }

        if (!_connection.IsOnline)
        {
            StatusMessage = RouteSettingsTexts.CheckOffline;
            return;
        }

        StatusMessage = RouteSettingsTexts.Checking;
        using var cancellation = new CancellationTokenSource();
        _checkCancellation = cancellation;
        try
        {
            var result = await _checker.CheckAsync(candidate, cancellation.Token).ConfigureAwait(true);
            StatusMessage = RouteSettingsTexts.GetCheckMessage(result) + (isEntered && result == RoutingKeyCheckResult.Valid ? RouteSettingsTexts.NotSavedYet : string.Empty);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // Die Seite wurde verlassen: keine Meldung mehr für einen verlassenen Bildschirm.
            StatusMessage = null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Die Prüfung des Routing-Schlüssels ist fehlgeschlagen ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = RouteSettingsTexts.GetCheckMessage(RoutingKeyCheckResult.Unavailable);
        }
        finally
        {
            _checkCancellation = null;
        }
    }

    private async Task ApplyEnabledAsync(bool enabled)
    {
        try
        {
            var applied = await _access.SetEnabledAsync(enabled).ConfigureAwait(true);
            StatusMessage = null;
            if (!applied)
            {
                // Ohne Schlüssel lässt sich die Routensuche nicht einschalten (z. B. wenn er inzwischen entfernt wurde).
                StatusMessage = RouteSettingsTexts.EnableNeedsKey;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Der Schalter der Routensuche konnte nicht gespeichert werden ({ExceptionType}).", ex.GetType().Name);
            StatusMessage = RouteSettingsTexts.SwitchFailed;
        }

        await RefreshAsync().ConfigureAwait(true);
    }

    private async Task OpenSignupAsync()
    {
        try
        {
            await _linkOpener.OpenAsync(new Uri(RouteSettingsTexts.SignupUrl)).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Die Anmeldeseite konnte nicht geöffnet werden ({ExceptionType}).", ex.GetType().Name);
        }
    }
}

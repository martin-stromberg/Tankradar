using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Karte „Routensuche“ in den Optionen: verdecktes Schlüsselfeld, Speichern, Prüfen mit verständlichen Rückmeldungen, Entfernen und den Schalter, der nur mit Schlüssel bedienbar ist.
/// </summary>
public class RouteSearchSettingsViewModelTests : BaseTest
{
    private const string Key = "ZZPLATZHALTER-OPTIONEN-ZZ";

    private readonly FakeRouteSearchAccess _access = new() { Key = null, Switched = false };
    private readonly FakeRoutingKeyChecker _checker = new();
    private readonly FakeConnectionMonitor _connection = new();
    private readonly FakeLinkOpener _links = new();
    private readonly RecordingLogger<RouteSearchSettingsViewModel> _logger = new();
    private readonly RouteSearchSettingsViewModel _viewModel;

    /// <summary>
    /// Erstellt das ViewModel mit Fakes (ohne gespeicherten Schlüssel).
    /// </summary>
    public RouteSearchSettingsViewModelTests()
    {
        _viewModel = new RouteSearchSettingsViewModel(_access, _checker, _connection, _links, _logger);
    }

    private async Task AppearAsync()
    {
        _viewModel.OnAppearing();
        await _viewModel.LastActionTask;
    }

    private async Task<string?> RunAsync(Action action)
    {
        action();
        await _viewModel.LastActionTask;
        return _viewModel.StatusMessage;
    }

    /// <summary>
    /// Prüft den Ausgangszustand: Feld verdeckt, kein Schlüssel, Schalter aus und mit Hinweis, Texte vorhanden.
    /// </summary>
    [Fact]
    public async Task Initial_NoKey_SwitchOffWithHint()
    {
        await AppearAsync();

        Assert.True(_viewModel.IsKeyMasked);
        Assert.False(_viewModel.HasStoredKey);
        Assert.False(_viewModel.IsRouteSearchEnabled);
        Assert.Equal(RouteSettingsTexts.KeyMissing, _viewModel.KeyStatus);
        Assert.True(_viewModel.HasEnableHint);
        Assert.Equal(RouteSettingsTexts.EnableNeedsKey, _viewModel.EnableHint);
        Assert.Contains("openrouteservice.org", _viewModel.SignupHint, StringComparison.Ordinal);
        Assert.NotEmpty(_viewModel.Heading);
        Assert.NotEmpty(_viewModel.Hint);
        Assert.NotEmpty(_viewModel.EnableLabel);
        Assert.NotEmpty(_viewModel.KeyPlaceholder);
        Assert.NotEmpty(_viewModel.SaveButtonText);
        Assert.NotEmpty(_viewModel.CheckButtonText);
        Assert.NotEmpty(_viewModel.RemoveButtonText);
        Assert.NotEmpty(_viewModel.SignupButtonText);
        Assert.False(_viewModel.HasStatusMessage);
    }

    /// <summary>
    /// Prüft, dass das Feld nur auf ausdrücklichen Wunsch (Testmodus) unverdeckt ist.
    /// </summary>
    [Fact]
    public void KeyInput_MaskCanOnlyBeDisabledByConstructor()
    {
        var unmasked = new RouteSearchSettingsViewModel(_access, _checker, _connection, _links, NullLogger<RouteSearchSettingsViewModel>.Instance, maskKeyInput: false);

        Assert.False(unmasked.IsKeyMasked);
        Assert.True(_viewModel.IsKeyMasked);
    }

    /// <summary>
    /// Prüft das Speichern: Der Schlüssel landet in der sicheren Ablage, das Feld wird geleert, der Status zeigt „gespeichert“, die Rückmeldung nennt den Schlüssel nicht.
    /// </summary>
    [Fact]
    public async Task Save_ValidKey_StoresClearsFieldAndNeverEchoesKey()
    {
        await AppearAsync();
        _viewModel.KeyInput = " " + Key + " ";
        Assert.True(_viewModel.HasKeyInput);

        var message = await RunAsync(() => _viewModel.SaveKeyCommand.Execute(null));

        Assert.Equal(Key, _access.Key);
        Assert.Equal(string.Empty, _viewModel.KeyInput);
        Assert.False(_viewModel.HasKeyInput);
        Assert.True(_viewModel.HasStoredKey);
        Assert.Equal(RouteSettingsTexts.KeyPresent, _viewModel.KeyStatus);
        Assert.False(_viewModel.IsRouteSearchEnabled);
        Assert.False(_viewModel.HasEnableHint);
        Assert.Equal(RouteSettingsTexts.Saved, message);
        Assert.DoesNotContain(Key, _viewModel.KeyStatus + message + _logger.AllText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass leere und unzulässige Eingaben nicht gespeichert werden und eine verständliche Meldung erscheint.
    /// </summary>
    /// <param name="input">Die Eingabe.</param>
    /// <param name="error">Der erwartete Fehler.</param>
    [Theory]
    [InlineData("", RoutingKeyError.Empty)]
    [InlineData("   ", RoutingKeyError.Empty)]
    [InlineData("zwei Teile", RoutingKeyError.InvalidCharacters)]
    public async Task Save_InvalidInput_IsRejectedWithMessage(string input, RoutingKeyError error)
    {
        await AppearAsync();
        _viewModel.KeyInput = input;

        var message = await RunAsync(() => _viewModel.SaveKeyCommand.Execute(null));

        Assert.Null(_access.Key);
        Assert.Equal(RouteSettingsTexts.GetKeyErrorMessage(error), message);
        Assert.DoesNotContain("zwei Teile", message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass ein Fehler der sicheren Ablage gemeldet wird, ohne Ausnahmetext oder Schlüssel im Protokoll.
    /// </summary>
    [Fact]
    public async Task Save_StoreFails_ShowsMessageAndLogsOnlyType()
    {
        await AppearAsync();
        _access.ThrowOnWrite = true;
        _viewModel.KeyInput = Key;

        var message = await RunAsync(() => _viewModel.SaveKeyCommand.Execute(null));

        Assert.Equal(RouteSettingsTexts.SaveFailed, message);
        Assert.False(_viewModel.HasStoredKey);
        Assert.Contains("InvalidOperationException", _logger.AllText, StringComparison.Ordinal);
        Assert.DoesNotContain(Key, _logger.AllText, StringComparison.Ordinal);
        Assert.DoesNotContain("Ablage nicht beschreibbar", _logger.AllText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft die Rückmeldungen der Prüfung eines eingegebenen Schlüssels; ein gültiger, noch nicht gespeicherter Schlüssel wird so gekennzeichnet.
    /// </summary>
    /// <param name="result">Das Ergebnis der Prüfung.</param>
    [Theory]
    [InlineData(RoutingKeyCheckResult.Valid)]
    [InlineData(RoutingKeyCheckResult.Invalid)]
    [InlineData(RoutingKeyCheckResult.Unreachable)]
    [InlineData(RoutingKeyCheckResult.QuotaExceeded)]
    [InlineData(RoutingKeyCheckResult.Unavailable)]
    public async Task Check_EnteredKey_ShowsMatchingMessage(RoutingKeyCheckResult result)
    {
        await AppearAsync();
        _checker.Result = result;
        _viewModel.KeyInput = Key;

        var message = await RunAsync(() => _viewModel.CheckKeyCommand.Execute(null));

        Assert.Equal([Key], _checker.CheckedKeys);
        Assert.StartsWith(RouteSettingsTexts.GetCheckMessage(result), message, StringComparison.Ordinal);
        Assert.Equal(result == RoutingKeyCheckResult.Valid, message!.EndsWith(RouteSettingsTexts.NotSavedYet, StringComparison.Ordinal));
        Assert.DoesNotContain(Key, message, StringComparison.Ordinal);
        Assert.False(_viewModel.IsChecking);
        Assert.True(_viewModel.CanUseButtons);
        Assert.Null(_access.Key);
        Assert.Equal(Key, _viewModel.KeyInput);
    }

    /// <summary>
    /// Prüft, dass ohne Eingabe der gespeicherte Schlüssel geprüft wird und die Meldung dann keinen Zusatz „nicht gespeichert“ trägt.
    /// </summary>
    [Fact]
    public async Task Check_WithoutInput_ChecksStoredKey()
    {
        _access.Key = Key;
        await AppearAsync();

        var message = await RunAsync(() => _viewModel.CheckKeyCommand.Execute(null));

        Assert.Equal([Key], _checker.CheckedKeys);
        Assert.Equal(RouteSettingsTexts.GetCheckMessage(RoutingKeyCheckResult.Valid), message);
    }

    /// <summary>
    /// Prüft, dass ohne jeden Schlüssel, ohne Verbindung und bei unzulässiger Eingabe nichts an den Dienst geht.
    /// </summary>
    [Fact]
    public async Task Check_NothingToCheckOfflineOrInvalid_NeverCallsService()
    {
        await AppearAsync();
        Assert.Equal(RouteSettingsTexts.NothingToCheck, await RunAsync(() => _viewModel.CheckKeyCommand.Execute(null)));

        _viewModel.KeyInput = Key;
        _connection.IsOnline = false;
        Assert.Equal(RouteSettingsTexts.CheckOffline, await RunAsync(() => _viewModel.CheckKeyCommand.Execute(null)));

        _connection.IsOnline = true;
        _viewModel.KeyInput = "zwei Teile";
        Assert.Equal(RouteSettingsTexts.GetKeyErrorMessage(RoutingKeyError.InvalidCharacters), await RunAsync(() => _viewModel.CheckKeyCommand.Execute(null)));

        Assert.Empty(_checker.CheckedKeys);
    }

    /// <summary>
    /// Prüft, dass eine unerwartete Ausnahme der Prüfung zu einer verständlichen Meldung führt und nur der Typ protokolliert wird.
    /// </summary>
    [Fact]
    public async Task Check_ServiceThrows_ShowsUnavailableMessage()
    {
        await AppearAsync();
        _checker.Failure = new InvalidOperationException("Geheimnis " + Key);
        _viewModel.KeyInput = Key;

        var message = await RunAsync(() => _viewModel.CheckKeyCommand.Execute(null));

        Assert.Equal(RouteSettingsTexts.GetCheckMessage(RoutingKeyCheckResult.Unavailable), message);
        Assert.DoesNotContain(Key, _logger.AllText, StringComparison.Ordinal);
        Assert.False(_viewModel.IsChecking);
    }

    /// <summary>
    /// Prüft, dass während einer Prüfung die Schaltflächen gesperrt sind und eine zweite Prüfung nicht startet.
    /// </summary>
    [Fact]
    public async Task Check_WhileRunning_LocksButtonsAndIgnoresSecondCheck()
    {
        await AppearAsync();
        var gate = new TaskCompletionSource<RoutingKeyCheckResult>();
        var slow = new GatedChecker(gate.Task);
        var viewModel = new RouteSearchSettingsViewModel(_access, slow, _connection, _links, _logger);
        viewModel.KeyInput = Key;

        viewModel.CheckKeyCommand.Execute(null);
        var running = viewModel.LastActionTask;
        Assert.True(viewModel.IsChecking);
        Assert.False(viewModel.CanUseButtons);
        Assert.Equal(RouteSettingsTexts.Checking, viewModel.StatusMessage);

        viewModel.CheckKeyCommand.Execute(null);
        gate.SetResult(RoutingKeyCheckResult.Valid);
        await running;

        Assert.Equal(1, slow.Calls);
        Assert.False(viewModel.IsChecking);
    }

    /// <summary>
    /// Prüft den Schalter: Ohne Schlüssel bleibt er aus, mit Schlüssel lässt er sich ein- und ausschalten und wirkt auf den Zugang.
    /// </summary>
    [Fact]
    public async Task Switch_RequiresKeyAndTogglesAccess()
    {
        await AppearAsync();

        _viewModel.IsRouteSearchEnabled = true;
        await _viewModel.LastActionTask;

        Assert.False(_viewModel.IsRouteSearchEnabled);
        Assert.False(_access.Switched);
        Assert.Equal(RouteSettingsTexts.EnableNeedsKey, _viewModel.StatusMessage);

        _access.Key = Key;
        await AppearAsync();
        _viewModel.IsRouteSearchEnabled = true;
        await _viewModel.LastActionTask;

        Assert.True(_viewModel.IsRouteSearchEnabled);
        Assert.True(_access.Switched);
        Assert.Null(_viewModel.StatusMessage);

        _viewModel.IsRouteSearchEnabled = false;
        await _viewModel.LastActionTask;

        Assert.False(_access.Switched);
        Assert.False(_viewModel.IsRouteSearchEnabled);
    }

    /// <summary>
    /// Prüft, dass ein Fehler beim Speichern des Schalters gemeldet wird und der Schalter den tatsächlichen Zustand zeigt.
    /// </summary>
    [Fact]
    public async Task Switch_StoreFails_ShowsMessageAndRevertsToActualState()
    {
        _access.Key = Key;
        await AppearAsync();
        _access.ThrowOnWrite = true;

        _viewModel.IsRouteSearchEnabled = true;
        await _viewModel.LastActionTask;

        Assert.Equal(RouteSettingsTexts.SwitchFailed, _viewModel.StatusMessage);
        Assert.False(_viewModel.IsRouteSearchEnabled);
    }

    /// <summary>
    /// Prüft, dass das Entfernen des Schlüssels die Routensuche ausschaltet und den Schalter sperrt.
    /// </summary>
    [Fact]
    public async Task Remove_DisablesRouteSearchAndLocksSwitch()
    {
        _access.Key = Key;
        _access.Switched = true;
        await AppearAsync();
        Assert.True(_viewModel.IsRouteSearchEnabled);
        Assert.True(_viewModel.HasStoredKey);

        var message = await RunAsync(() => _viewModel.RemoveKeyCommand.Execute(null));

        Assert.Null(_access.Key);
        Assert.False(_access.Switched);
        Assert.False(_viewModel.HasStoredKey);
        Assert.False(_viewModel.IsRouteSearchEnabled);
        Assert.True(_viewModel.HasEnableHint);
        Assert.Equal(RouteSettingsTexts.Removed, message);
    }

    /// <summary>
    /// Prüft, dass ein Fehler beim Entfernen gemeldet wird.
    /// </summary>
    [Fact]
    public async Task Remove_StoreFails_ShowsMessage()
    {
        _access.Key = Key;
        await AppearAsync();
        _access.ThrowOnWrite = true;

        var message = await RunAsync(() => _viewModel.RemoveKeyCommand.Execute(null));

        Assert.Equal(RouteSettingsTexts.RemoveFailed, message);
    }

    /// <summary>
    /// Prüft, dass das Verlassen der Seite das Eingabefeld leert, damit kein Schlüssel dort zurückbleibt.
    /// </summary>
    [Fact]
    public async Task OnDisappearing_ClearsKeyInput()
    {
        await AppearAsync();
        _viewModel.KeyInput = Key;

        _viewModel.OnDisappearing();

        Assert.Equal(string.Empty, _viewModel.KeyInput);
    }

    /// <summary>
    /// Prüft, dass die Anmeldeseite von OpenRouteService geöffnet wird und ein Fehler des Öffnens nicht abstürzt.
    /// </summary>
    [Fact]
    public async Task OpenSignup_OpensOpenRouteServicePage()
    {
        await RunAsync(() => _viewModel.OpenSignupCommand.Execute(null));

        Assert.Equal([new Uri(RouteSettingsTexts.SignupUrl)], _links.Opened);
        Assert.Equal(Uri.UriSchemeHttps, _links.Opened[0].Scheme);
        Assert.Contains("openrouteservice.org", _links.Opened[0].Host, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft die Texte der Prüfrückmeldungen: Jedes Ergebnis hat einen eigenen Text, und die Schreibfehlermeldungen sind vorhanden.
    /// </summary>
    [Fact]
    public void Texts_CoverAllResultsAndErrors()
    {
        var checks = Enum.GetValues<RoutingKeyCheckResult>().Select(RouteSettingsTexts.GetCheckMessage).ToList();

        Assert.Equal(checks.Count, checks.Distinct().Count());
        Assert.Equal(string.Empty, RouteSettingsTexts.GetKeyErrorMessage(RoutingKeyError.None));
        Assert.All(new[] { RoutingKeyError.Empty, RoutingKeyError.TooLong, RoutingKeyError.InvalidCharacters }, error => Assert.NotEmpty(RouteSettingsTexts.GetKeyErrorMessage(error)));
    }

    private sealed class GatedChecker : MAUI.Services.Routing.IRoutingKeyChecker
    {
        private readonly Task<RoutingKeyCheckResult> _gate;

        public GatedChecker(Task<RoutingKeyCheckResult> gate)
        {
            _gate = gate;
        }

        public int Calls { get; private set; }

        public Task<RoutingKeyCheckResult> CheckAsync(string key, CancellationToken cancellationToken = default)
        {
            Calls++;
            return _gate;
        }
    }
}

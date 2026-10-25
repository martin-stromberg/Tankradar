using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.TestSupport;

namespace Tankradar.MAUI.Services.Routing;

/// <summary>
/// Zugang zur Routensuche: der vom Anwender eingetragene OpenRouteService-Schlüssel (sichere Ablage) und der Schalter „Routensuche aktivieren“.
/// Die Routensuche ist nur aktiv, wenn der Schalter an ist und ein Schlüssel gespeichert ist. Schlüssel und Schalter sind gerätelokal und gehören nicht zur
/// Sicherung; nach einer Wiederherstellung ohne Schlüssel ist die Routensuche daher aus. Der Schlüssel wird nie protokolliert.
/// Als <see cref="IApiKeyProvider"/> liefert der Zugang den Schlüssel nur bei aktivierter Routensuche, sodass ohne Aktivierung kein Aufruf an den Routingdienst möglich ist.
/// </summary>
public interface IRouteSearchAccess : IApiKeyProvider
{
    /// <summary>
    /// Wird ausgelöst, nachdem sich Schlüssel oder Schalter geändert haben (ggf. auf einem Hintergrundthread).
    /// </summary>
    event EventHandler? Changed;

    /// <summary>
    /// Gibt an, ob ein Schlüssel gespeichert ist.
    /// </summary>
    /// <returns><see langword="true"/>, wenn ein Schlüssel gespeichert ist.</returns>
    Task<bool> HasKeyAsync();

    /// <summary>
    /// Gibt an, ob die Routensuche aktiviert ist (Schalter an und Schlüssel vorhanden).
    /// </summary>
    /// <returns><see langword="true"/>, wenn die Routensuche genutzt werden darf.</returns>
    Task<bool> IsEnabledAsync();

    /// <summary>
    /// Liefert den gespeicherten Schlüssel unabhängig vom Schalter (nur für die Prüfung in den Optionen).
    /// </summary>
    /// <returns>Der Schlüssel oder <see langword="null"/>.</returns>
    Task<string?> GetStoredKeyAsync();

    /// <summary>
    /// Speichert den Schlüssel; die Routensuche bleibt dabei in ihrem bisherigen Schaltzustand.
    /// </summary>
    /// <param name="key">Der Schlüssel (wird auf zulässige Schreibweise geprüft).</param>
    /// <returns>Ein Task, der nach dem Speichern abgeschlossen ist.</returns>
    /// <exception cref="ArgumentException">Der Schlüssel hat eine unzulässige Schreibweise.</exception>
    Task SaveKeyAsync(string key);

    /// <summary>
    /// Entfernt den Schlüssel und schaltet die Routensuche aus.
    /// </summary>
    /// <returns>Ein Task, der nach dem Entfernen abgeschlossen ist.</returns>
    Task RemoveKeyAsync();

    /// <summary>
    /// Schaltet die Routensuche ein oder aus. Ohne gespeicherten Schlüssel lässt sie sich nicht einschalten.
    /// </summary>
    /// <param name="enabled">Der gewünschte Zustand.</param>
    /// <returns><see langword="true"/>, wenn der gewünschte Zustand erreicht wurde; <see langword="false"/>, wenn das Einschalten mangels Schlüssel abgelehnt wurde.</returns>
    Task<bool> SetEnabledAsync(bool enabled);
}

/// <summary>
/// Standardumsetzung von <see cref="IRouteSearchAccess"/> auf Basis zweier sicherer Ablagen (Schlüssel und Schalter).
/// </summary>
public sealed class RouteSearchAccess : IRouteSearchAccess
{
    private const string EnabledMarker = "1";

    private readonly IApiKeyStore _keyStore;
    private readonly IApiKeyStore _enabledStore;
    private readonly ILogger<RouteSearchAccess> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// Erstellt den Zugang.
    /// </summary>
    /// <param name="keyStore">Die sichere Ablage des Schlüssels.</param>
    /// <param name="enabledStore">Die Ablage des Schalters (gerätelokal).</param>
    /// <param name="logger">Logger (protokolliert nie den Schlüssel).</param>
    public RouteSearchAccess(IApiKeyStore keyStore, IApiKeyStore enabledStore, ILogger<RouteSearchAccess> logger)
    {
        _keyStore = keyStore;
        _enabledStore = enabledStore;
        _logger = logger;
    }

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <summary>
    /// Erstellt den Zugang für die Umgebung: Im Testmodus (<c>TANKATLAS_TEST_DATA_PATH</c> gesetzt) liegen Schlüssel und Schalter nur im Arbeitsspeicher und werden
    /// aus dem Test-Schlüssel (<see cref="TestDataPaths.RoutingTestApiKeyEnvironmentVariable"/>) vorbelegt (Schlüssel gesetzt und Routensuche aktiviert);
    /// sonst gelten die echten Ablagen des Betriebssystems.
    /// </summary>
    /// <param name="getEnvironmentVariable">Liefert Umgebungsvariablen.</param>
    /// <param name="createKeyStore">Erzeugt die echte Ablage des Schlüssels.</param>
    /// <param name="createEnabledStore">Erzeugt die echte Ablage des Schalters.</param>
    /// <param name="logger">Logger.</param>
    /// <returns>Der Zugang.</returns>
    public static RouteSearchAccess Create(
        Func<string, string?> getEnvironmentVariable,
        Func<IApiKeyStore> createKeyStore,
        Func<IApiKeyStore> createEnabledStore,
        ILogger<RouteSearchAccess> logger)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);
        ArgumentNullException.ThrowIfNull(createKeyStore);
        ArgumentNullException.ThrowIfNull(createEnabledStore);
        var testMode = !string.IsNullOrWhiteSpace(getEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable));
        if (!testMode)
        {
            return new RouteSearchAccess(createKeyStore(), createEnabledStore(), logger);
        }

        var keyStore = new InMemoryApiKeyStore();
        var enabledStore = new InMemoryApiKeyStore();
        if (RoutingKeyFormat.Validate(getEnvironmentVariable(TestDataPaths.RoutingTestApiKeyEnvironmentVariable), out var key) == RoutingKeyError.None)
        {
            keyStore.SetAsync(key).GetAwaiter().GetResult();
            enabledStore.SetAsync(EnabledMarker).GetAwaiter().GetResult();
        }

        return new RouteSearchAccess(keyStore, enabledStore, logger);
    }

    /// <inheritdoc />
    public async Task<bool> HasKeyAsync()
    {
        return await GetStoredKeyAsync().ConfigureAwait(false) is not null;
    }

    /// <inheritdoc />
    public async Task<string?> GetStoredKeyAsync()
    {
        try
        {
            var stored = await _keyStore.GetAsync().ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(stored) ? null : stored.Trim();
        }
        catch (Exception ex)
        {
            // Nur der Typ wird protokolliert: Meldungen der Ablage könnten Details zum Eintrag enthalten.
            _logger.LogWarning("Die sichere Ablage des Routing-Schlüssels konnte nicht gelesen werden ({ExceptionType}).", ex.GetType().Name);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsEnabledAsync()
    {
        return await IsSwitchedOnAsync().ConfigureAwait(false) && await HasKeyAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string?> GetApiKeyAsync()
    {
        return await IsSwitchedOnAsync().ConfigureAwait(false) ? await GetStoredKeyAsync().ConfigureAwait(false) : null;
    }

    /// <inheritdoc />
    public async Task SaveKeyAsync(string key)
    {
        if (RoutingKeyFormat.Validate(key, out var normalized) != RoutingKeyError.None)
        {
            throw new ArgumentException("Der Schlüssel hat eine unzulässige Schreibweise.", nameof(key));
        }

        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            await _keyStore.SetAsync(normalized).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public async Task RemoveKeyAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            // Zuerst der Schalter: Scheitert das Entfernen des Schlüssels, ist die Routensuche trotzdem aus.
            await _enabledStore.RemoveAsync().ConfigureAwait(false);
            await _keyStore.RemoveAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public async Task<bool> SetEnabledAsync(bool enabled)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!enabled)
            {
                await _enabledStore.RemoveAsync().ConfigureAwait(false);
            }
            else
            {
                if (!await HasKeyAsync().ConfigureAwait(false))
                {
                    return false;
                }

                await _enabledStore.SetAsync(EnabledMarker).ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
        }

        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private async Task<bool> IsSwitchedOnAsync()
    {
        try
        {
            return await _enabledStore.GetAsync().ConfigureAwait(false) == EnabledMarker;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Der Schalter der Routensuche konnte nicht gelesen werden ({ExceptionType}).", ex.GetType().Name);
            return false;
        }
    }
}

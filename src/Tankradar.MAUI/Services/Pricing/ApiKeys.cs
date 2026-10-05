using System.Reflection;
using Microsoft.Extensions.Logging;
using Tankradar.TestSupport;

namespace Tankradar.MAUI.Services.Pricing;

/// <summary>
/// Sichere Ablage des API-Schlüssels (iOS: Keychain, Windows: Credential Locker).
/// </summary>
public interface IApiKeyStore
{
    /// <summary>
    /// Liest den gespeicherten Schlüssel.
    /// </summary>
    /// <returns>Der Schlüssel oder <see langword="null"/>, wenn keiner hinterlegt ist.</returns>
    Task<string?> GetAsync();

    /// <summary>
    /// Speichert den Schlüssel.
    /// </summary>
    /// <param name="apiKey">Der Schlüssel.</param>
    /// <returns>Ein Task, der nach dem Speichern abgeschlossen ist.</returns>
    Task SetAsync(string apiKey);
}

/// <summary>
/// Liefert den API-Schlüssel für die Preis-API.
/// </summary>
public interface IApiKeyProvider
{
    /// <summary>
    /// Ermittelt den Schlüssel.
    /// </summary>
    /// <returns>Der Schlüssel oder <see langword="null"/>, wenn keiner vorhanden ist.</returns>
    Task<string?> GetApiKeyAsync();
}

/// <summary>
/// Ermittelt den API-Schlüssel: Im Testmodus aus einer Umgebungsvariable (nie gespeichert), sonst der beim Build mitgegebene Schlüssel
/// (maßgeblich; weicht er vom gespeicherten ab, wird die sichere Ablage aktualisiert), andernfalls der gespeicherte Schlüssel.
/// </summary>
public sealed class ApiKeyProvider : IApiKeyProvider
{
    /// <summary>
    /// Name des Assembly-Metadatums, das den beim Build mitgegebenen Schlüssel trägt.
    /// </summary>
    public const string BuildKeyMetadataName = "TankerkoenigApiKey";

    private readonly IApiKeyStore _store;
    private readonly Func<string?> _buildTimeKey;
    private readonly Func<string, string?> _getEnvironmentVariable;
    private readonly ILogger<ApiKeyProvider> _logger;

    /// <summary>
    /// Erstellt den Provider.
    /// </summary>
    /// <param name="store">Die sichere Ablage.</param>
    /// <param name="buildTimeKey">Liefert den beim Build mitgegebenen Schlüssel.</param>
    /// <param name="getEnvironmentVariable">Liefert Umgebungsvariablen.</param>
    /// <param name="logger">Logger (protokolliert nie den Schlüssel).</param>
    public ApiKeyProvider(
        IApiKeyStore store,
        Func<string?> buildTimeKey,
        Func<string, string?> getEnvironmentVariable,
        ILogger<ApiKeyProvider> logger)
    {
        _store = store;
        _buildTimeKey = buildTimeKey;
        _getEnvironmentVariable = getEnvironmentVariable;
        _logger = logger;
    }

    /// <summary>
    /// Liest den beim Build mitgegebenen Schlüssel aus den Assembly-Metadaten.
    /// </summary>
    /// <returns>Der Schlüssel oder <see langword="null"/>, wenn beim Build keiner mitgegeben wurde.</returns>
    public static string? ReadBuildTimeKey()
    {
        var value = typeof(ApiKeyProvider).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == BuildKeyMetadataName)?.Value;
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <inheritdoc />
    public async Task<string?> GetApiKeyAsync()
    {
        var testMode = !string.IsNullOrWhiteSpace(_getEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable));
        if (testMode)
        {
            // Im Testmodus nie der echte Build-Schlüssel: nur der Test-Schlüssel aus der Umgebung.
            return _getEnvironmentVariable(PriceApiOptions.ApiKeyEnvironmentVariable) is { Length: > 0 } testKey ? testKey.Trim() : null;
        }

        string? stored = null;
        try
        {
            stored = await _store.GetAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Die sichere Ablage konnte nicht gelesen werden.");
        }

        var buildKey = _buildTimeKey()?.Trim();
        if (string.IsNullOrWhiteSpace(buildKey))
        {
            return string.IsNullOrEmpty(stored) ? null : stored;
        }

        // Der Build-Schlüssel ist maßgeblich: Weicht er vom gespeicherten ab (neu oder rotiert), wird die Ablage aktualisiert.
        if (!string.Equals(stored, buildKey, StringComparison.Ordinal))
        {
            try
            {
                await _store.SetAsync(buildKey).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Der API-Schlüssel konnte nicht in die sichere Ablage übernommen werden.");
            }
        }

        return buildKey;
    }
}

/// <summary>
/// Ablage des API-Schlüssels im Arbeitsspeicher; wird im Testmodus statt der echten Ablage des Betriebssystems verwendet.
/// </summary>
public sealed class InMemoryApiKeyStore : IApiKeyStore
{
    private string? _value;

    /// <inheritdoc />
    public Task<string?> GetAsync()
    {
        return Task.FromResult(_value);
    }

    /// <inheritdoc />
    public Task SetAsync(string apiKey)
    {
        _value = apiKey;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Wählt die Ablage des API-Schlüssels: Im Testmodus (<c>TANKATLAS_TEST_DATA_PATH</c> gesetzt) isoliert im Speicher,
/// sonst die echte Ablage des Betriebssystems (Credential Locker bzw. Keychain).
/// </summary>
public static class ApiKeyStoreSelector
{
    /// <summary>
    /// Erstellt die passende Ablage; die echte Ablage wird im Testmodus nie erzeugt.
    /// </summary>
    /// <param name="getEnvironmentVariable">Liefert Umgebungsvariablen.</param>
    /// <param name="createPlatformStore">Erzeugt die echte Ablage.</param>
    /// <returns>Die Ablage.</returns>
    public static IApiKeyStore Create(Func<string, string?> getEnvironmentVariable, Func<IApiKeyStore> createPlatformStore)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);
        ArgumentNullException.ThrowIfNull(createPlatformStore);
        return string.IsNullOrWhiteSpace(getEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable))
            ? createPlatformStore()
            : new InMemoryApiKeyStore();
    }
}

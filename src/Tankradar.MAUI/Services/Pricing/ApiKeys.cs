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
/// Ermittelt den API-Schlüssel: Im Testmodus aus einer Umgebungsvariable (nie gespeichert), sonst aus der sicheren Ablage;
/// ist dort keiner, wird der beim Build mitgegebene Schlüssel einmalig in die sichere Ablage übernommen.
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
        if (testMode && _getEnvironmentVariable(PriceApiOptions.ApiKeyEnvironmentVariable) is { Length: > 0 } testKey)
        {
            return testKey;
        }

        try
        {
            if (await _store.GetAsync().ConfigureAwait(false) is { Length: > 0 } stored)
            {
                return stored;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Die sichere Ablage konnte nicht gelesen werden.");
        }

        var buildKey = _buildTimeKey();
        if (string.IsNullOrWhiteSpace(buildKey))
        {
            return null;
        }

        try
        {
            await _store.SetAsync(buildKey).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Der API-Schlüssel konnte nicht in die sichere Ablage übernommen werden.");
        }

        return buildKey;
    }
}

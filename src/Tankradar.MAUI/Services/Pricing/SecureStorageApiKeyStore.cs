namespace Tankradar.MAUI.Services.Pricing;

/// <summary>
/// Ablage des API-Schlüssels über <see cref="SecureStorage"/> (unter iOS die Keychain).
/// </summary>
public sealed class SecureStorageApiKeyStore : IApiKeyStore
{
    /// <summary>
    /// Speicherschlüssel des Schlüssels der Preis-API.
    /// </summary>
    public const string PriceStorageKey = "tankatlas.price-api-key";

    /// <summary>
    /// Speicherschlüssel des Schlüssels des Routingdienstes.
    /// </summary>
    public const string RoutingStorageKey = "tankatlas.routing-api-key";

    private readonly string _storageKey;

    /// <summary>
    /// Erstellt die Ablage.
    /// </summary>
    /// <param name="storageKey">Der Speicherschlüssel; Standard ist der Schlüssel der Preis-API.</param>
    public SecureStorageApiKeyStore(string storageKey = PriceStorageKey)
    {
        _storageKey = storageKey;
    }

    /// <inheritdoc />
    public async Task<string?> GetAsync()
    {
        return await SecureStorage.Default.GetAsync(_storageKey).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SetAsync(string apiKey)
    {
        await SecureStorage.Default.SetAsync(_storageKey, apiKey).ConfigureAwait(false);
    }
}

namespace Tankradar.MAUI.Services.Pricing;

/// <summary>
/// Ablage des API-Schlüssels über <see cref="SecureStorage"/> (unter iOS die Keychain).
/// </summary>
public sealed class SecureStorageApiKeyStore : IApiKeyStore
{
    private const string StorageKey = "tankatlas.price-api-key";

    /// <inheritdoc />
    public async Task<string?> GetAsync()
    {
        return await SecureStorage.Default.GetAsync(StorageKey).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SetAsync(string apiKey)
    {
        await SecureStorage.Default.SetAsync(StorageKey, apiKey).ConfigureAwait(false);
    }
}

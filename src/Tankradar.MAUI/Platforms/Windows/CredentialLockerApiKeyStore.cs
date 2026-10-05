using Tankradar.MAUI.Services.Pricing;
using Windows.Security.Credentials;

namespace Tankradar.MAUI.Platforms.Windows;

/// <summary>
/// Ablage des API-Schlüssels im Windows Credential Locker.
/// </summary>
public sealed class CredentialLockerApiKeyStore : IApiKeyStore
{
    private const string Resource = "Tankatlas/PriceApi";
    private const string UserName = "api-key";
    private const int ElementNotFound = unchecked((int)0x80070490);

    /// <inheritdoc />
    public Task<string?> GetAsync()
    {
        try
        {
            var vault = new PasswordVault();
            var credential = vault.Retrieve(Resource, UserName);
            credential.RetrievePassword();
            return Task.FromResult<string?>(credential.Password);
        }
        catch (Exception ex) when (ex.HResult == ElementNotFound)
        {
            // Ein nicht vorhandener Eintrag wird vom Credential Locker als Ausnahme gemeldet; alle anderen Fehler (z. B. gesperrter Tresor) werden weitergereicht.
            return Task.FromResult<string?>(null);
        }
    }

    /// <inheritdoc />
    public Task SetAsync(string apiKey)
    {
        var vault = new PasswordVault();
        vault.Add(new PasswordCredential(Resource, UserName, apiKey));
        return Task.CompletedTask;
    }
}

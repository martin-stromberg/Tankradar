using Tankradar.MAUI.Services.Pricing;
using Windows.Security.Credentials;

namespace Tankradar.MAUI.Platforms.Windows;

/// <summary>
/// Ablage des API-Schlüssels im Windows Credential Locker.
/// </summary>
public sealed class CredentialLockerApiKeyStore : IApiKeyStore
{
    /// <summary>
    /// Ressourcenname des Schlüssels der Preis-API.
    /// </summary>
    public const string PriceResource = "Tankatlas/PriceApi";

    /// <summary>
    /// Ressourcenname des Schlüssels des Routingdienstes.
    /// </summary>
    public const string RoutingResource = "Tankatlas/RoutingApi";

    /// <summary>
    /// Ressourcenname des Schalters „Routensuche aktivieren“ (im Credential Locker dieses Windows-Kontos, nicht Teil der Sicherung der App).
    /// </summary>
    public const string RoutingEnabledResource = "Tankatlas/RoutingEnabled";

    private const string UserName = "api-key";
    private const int ElementNotFound = unchecked((int)0x80070490);

    private readonly string _resource;

    /// <summary>
    /// Erstellt die Ablage.
    /// </summary>
    /// <param name="resource">Der Ressourcenname im Credential Locker; Standard ist der Name des Schlüssels der Preis-API.</param>
    public CredentialLockerApiKeyStore(string resource = PriceResource)
    {
        _resource = resource;
    }

    /// <inheritdoc />
    public Task<string?> GetAsync()
    {
        try
        {
            var vault = new PasswordVault();
            var credential = vault.Retrieve(_resource, UserName);
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
        vault.Add(new PasswordCredential(_resource, UserName, apiKey));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync()
    {
        try
        {
            var vault = new PasswordVault();
            vault.Remove(vault.Retrieve(_resource, UserName));
        }
        catch (Exception ex) when (ex.HResult == ElementNotFound)
        {
            // Ein nicht vorhandener Eintrag ist beim Entfernen kein Fehler.
        }

        return Task.CompletedTask;
    }
}

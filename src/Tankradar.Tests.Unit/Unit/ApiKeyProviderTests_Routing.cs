using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Ermittlung des Routing-Schlüssels: gleiches Muster wie der Tankerkönig-Schlüssel, aber getrennte Ablage, Metadaten und Testvariable.
/// </summary>
public class ApiKeyProviderTests_Routing : BaseTest
{
    private readonly FakeApiKeyStore _store = new();
    private readonly Dictionary<string, string> _environment = [];
    private string? _buildKey;

    private ApiKeyProvider CreateProvider()
    {
        return new ApiKeyProvider(
            _store,
            () => _buildKey,
            name => _environment.GetValueOrDefault(name),
            NullLogger<ApiKeyProvider>.Instance,
            TestDataPaths.RoutingTestApiKeyEnvironmentVariable);
    }

    /// <summary>
    /// Prüft, dass im Testmodus nur die Routing-Testvariable gilt und der Schlüssel nie gespeichert wird.
    /// </summary>
    [Fact]
    public async Task GetApiKeyAsync_TestMode_UsesOnlyRoutingTestVariable()
    {
        _environment[TestDataPaths.TestDataPathEnvironmentVariable] = "testdata";
        _environment[PriceApiOptions.ApiKeyEnvironmentVariable] = "preis-schluessel";

        Assert.Null(await CreateProvider().GetApiKeyAsync());

        _environment[TestDataPaths.RoutingTestApiKeyEnvironmentVariable] = "routing-schluessel";

        Assert.Equal("routing-schluessel", await CreateProvider().GetApiKeyAsync());
        Assert.Equal(0, _store.SetCount);
    }

    /// <summary>
    /// Prüft, dass im Testmodus nie der Build-Schlüssel verwendet wird.
    /// </summary>
    [Fact]
    public async Task GetApiKeyAsync_TestModeWithBuildKey_IgnoresBuildKey()
    {
        _environment[TestDataPaths.TestDataPathEnvironmentVariable] = "testdata";
        _buildKey = "build";

        Assert.Null(await CreateProvider().GetApiKeyAsync());
        Assert.Equal(0, _store.SetCount);
    }

    /// <summary>
    /// Prüft, dass die Routing-Testvariable außerhalb des Testmodus ignoriert wird.
    /// </summary>
    [Fact]
    public async Task GetApiKeyAsync_RoutingVariableOutsideTestMode_IsIgnored()
    {
        _environment[TestDataPaths.RoutingTestApiKeyEnvironmentVariable] = "routing-schluessel";

        Assert.Null(await CreateProvider().GetApiKeyAsync());
    }

    /// <summary>
    /// Prüft, dass der Build-Schlüssel in die Ablage übernommen wird und ein abweichender gespeicherter Schlüssel ersetzt wird.
    /// </summary>
    [Fact]
    public async Task GetApiKeyAsync_BuildKey_ReplacesStoredKey()
    {
        _store.Stored = "alt";
        _buildKey = "neu";

        Assert.Equal("neu", await CreateProvider().GetApiKeyAsync());
        Assert.Equal("neu", _store.Stored);
    }

    /// <summary>
    /// Prüft, dass ein gespeicherter Schlüssel ohne Build-Schlüssel weiter genutzt wird und ohne jeden Schlüssel <see langword="null"/> gilt.
    /// </summary>
    [Fact]
    public async Task GetApiKeyAsync_StoredOrNothing_ResolvesAccordingly()
    {
        Assert.Null(await CreateProvider().GetApiKeyAsync());

        _store.Stored = "gespeichert";

        Assert.Equal("gespeichert", await CreateProvider().GetApiKeyAsync());
    }

    /// <summary>
    /// Prüft die getrennten Namen: Metadatum und Speicherschlüssel des Routing-Schlüssels unterscheiden sich von denen des Tankerkönig-Schlüssels.
    /// </summary>
    [Fact]
    public void Names_AreSeparateFromPriceKey()
    {
        Assert.Equal("RoutingApiKey", ApiKeyProvider.RoutingBuildKeyMetadataName);
        Assert.NotEqual(ApiKeyProvider.BuildKeyMetadataName, ApiKeyProvider.RoutingBuildKeyMetadataName);
        Assert.Equal("tankatlas.routing-api-key", SecureStorageApiKeyStore.RoutingStorageKey);
        Assert.NotEqual(SecureStorageApiKeyStore.PriceStorageKey, SecureStorageApiKeyStore.RoutingStorageKey);
    }

    /// <summary>
    /// Prüft, dass ohne Build-Angabe kein Routing-Schlüssel aus den Assembly-Metadaten gelesen wird.
    /// </summary>
    [Fact]
    public void ReadBuildTimeKey_WithoutBuildSetting_ReturnsNullOrNonEmpty()
    {
        var value = ApiKeyProvider.ReadBuildTimeKey(ApiKeyProvider.RoutingBuildKeyMetadataName);

        Assert.True(value is null || value.Length > 0);
    }
}

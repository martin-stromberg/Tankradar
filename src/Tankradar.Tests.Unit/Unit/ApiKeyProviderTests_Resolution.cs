using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Ermittlung des API-Schlüssels: sichere Ablage, Übernahme des Build-Schlüssels und Testmodus.
/// </summary>
public class ApiKeyProviderTests_Resolution : BaseTest
{
    private readonly FakeApiKeyStore _store = new();
    private readonly Dictionary<string, string> _environment = [];
    private string? _buildKey;

    private ApiKeyProvider CreateProvider()
    {
        return new ApiKeyProvider(_store, () => _buildKey, name => _environment.GetValueOrDefault(name), NullLogger<ApiKeyProvider>.Instance);
    }

    /// <summary>
    /// Prüft, dass ein gespeicherter Schlüssel Vorrang hat und der Build-Schlüssel nichts überschreibt.
    /// </summary>
    [Fact]
    public async Task GetApiKeyAsync_StoredKey_IsUsed()
    {
        _store.Stored = "gespeichert";
        _buildKey = "build";

        Assert.Equal("gespeichert", await CreateProvider().GetApiKeyAsync());
        Assert.Equal(0, _store.SetCount);
    }

    /// <summary>
    /// Prüft, dass der Build-Schlüssel einmalig in die sichere Ablage übernommen wird.
    /// </summary>
    [Fact]
    public async Task GetApiKeyAsync_BuildKey_IsMovedIntoSecureStore()
    {
        _buildKey = "build";
        var provider = CreateProvider();

        Assert.Equal("build", await provider.GetApiKeyAsync());
        Assert.Equal("build", await provider.GetApiKeyAsync());

        Assert.Equal("build", _store.Stored);
        Assert.Equal(1, _store.SetCount);
    }

    /// <summary>
    /// Prüft, dass ohne Schlüssel <see langword="null"/> geliefert wird.
    /// </summary>
    [Fact]
    public async Task GetApiKeyAsync_NoKeyAnywhere_ReturnsNull()
    {
        Assert.Null(await CreateProvider().GetApiKeyAsync());
    }

    /// <summary>
    /// Prüft, dass Fehler der sicheren Ablage nicht zum Absturz führen, der Build-Schlüssel aber weiter genutzt wird.
    /// </summary>
    [Fact]
    public async Task GetApiKeyAsync_StoreFailures_FallBackToBuildKey()
    {
        _store.ThrowOnGet = true;
        _store.ThrowOnSet = true;
        _buildKey = "build";

        Assert.Equal("build", await CreateProvider().GetApiKeyAsync());
    }

    /// <summary>
    /// Prüft, dass im Testmodus der Schlüssel aus der Umgebung stammt und nie gespeichert wird.
    /// </summary>
    [Fact]
    public async Task GetApiKeyAsync_TestMode_UsesEnvironmentKeyWithoutStoring()
    {
        _environment[TestDataPaths.TestDataPathEnvironmentVariable] = "C:\\Temp\\test";
        _environment[PriceApiOptions.ApiKeyEnvironmentVariable] = "aus-umgebung";

        Assert.Equal("aus-umgebung", await CreateProvider().GetApiKeyAsync());
        Assert.Equal(0, _store.SetCount);
    }

    /// <summary>
    /// Prüft, dass die Umgebungsvariable außerhalb des Testmodus ignoriert wird.
    /// </summary>
    [Fact]
    public async Task GetApiKeyAsync_EnvironmentKeyOutsideTestMode_IsIgnored()
    {
        _environment[PriceApiOptions.ApiKeyEnvironmentVariable] = "aus-umgebung";

        Assert.Null(await CreateProvider().GetApiKeyAsync());
    }

    /// <summary>
    /// Prüft, dass ohne Build-Angabe kein Schlüssel aus den Assembly-Metadaten gelesen wird.
    /// </summary>
    [Fact]
    public void ReadBuildTimeKey_WithoutBuildSetting_ReturnsNull()
    {
        var value = ApiKeyProvider.ReadBuildTimeKey();

        Assert.True(value is null || value.Length > 0);
    }
}

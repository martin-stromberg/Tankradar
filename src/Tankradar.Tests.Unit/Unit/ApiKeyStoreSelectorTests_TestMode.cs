using Tankradar.MAUI.Services.Pricing;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass im Testmodus nie die echte sichere Ablage des Entwicklers verwendet wird.
/// </summary>
public class ApiKeyStoreSelectorTests_TestMode : BaseTest
{
    /// <summary>
    /// Prüft, dass im Testmodus eine isolierte Ablage genutzt und die echte nie erzeugt wird.
    /// </summary>
    [Fact]
    public async Task Create_TestMode_UsesIsolatedStoreAndNeverCreatesPlatformStore()
    {
        var created = false;

        var store = ApiKeyStoreSelector.Create(
            name => name == TestDataPaths.TestDataPathEnvironmentVariable ? "testdata" : null,
            () =>
            {
                created = true;
                return new FakeApiKeyStore();
            });
        await store.SetAsync("abc");

        Assert.False(created);
        Assert.IsType<InMemoryApiKeyStore>(store);
        Assert.Equal("abc", await store.GetAsync());
    }

    /// <summary>
    /// Prüft, dass außerhalb des Testmodus die echte Ablage verwendet wird.
    /// </summary>
    [Fact]
    public void Create_NormalMode_UsesPlatformStore()
    {
        var platform = new FakeApiKeyStore();

        Assert.Same(platform, ApiKeyStoreSelector.Create(_ => null, () => platform));
    }
}

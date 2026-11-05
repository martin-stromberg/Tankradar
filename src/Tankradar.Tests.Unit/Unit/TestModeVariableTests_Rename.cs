using Tankradar.MAUI.Services;
using Tankradar.MAUI.Services.Location;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Umbenennung der Testmodus-Variable: Nur <c>TANKATLAS_TEST_DATA_PATH</c> aktiviert den Testmodus, der alte Name <c>TEST_DATA_PATH</c> nicht.
/// </summary>
[Collection(EnvironmentCollection.Name)]
public class TestModeVariableTests_Rename : BaseTest
{
    private const string OldName = "TEST_DATA_PATH";

    private static Func<string, string?> Env(string name, string value)
    {
        return key => key == name ? value : null;
    }

    /// <summary>
    /// Prüft die Namen der Konstanten.
    /// </summary>
    [Fact]
    public void Constants_HaveNewNames()
    {
        Assert.Equal("TANKATLAS_TEST_DATA_PATH", TestDataPaths.TestDataPathEnvironmentVariable);
        Assert.Equal("TANKATLAS_TEST_LOCATION", TestDataPaths.TestLocationEnvironmentVariable);
    }

    /// <summary>
    /// Prüft, dass der alte Name das Datenverzeichnis nicht umleitet.
    /// </summary>
    [Fact]
    public void AppDataPathProvider_OldNameDoesNotRedirect()
    {
        Environment.SetEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable, null);
        Environment.SetEnvironmentVariable(OldName, @"C:\AlterName");
        try
        {
            Assert.Equal(@"C:\Regulaer", new AppDataPathProvider(() => @"C:\Regulaer").GetDataDirectory());
        }
        finally
        {
            Environment.SetEnvironmentVariable(OldName, null);
        }
    }

    /// <summary>
    /// Prüft, dass der neue Name das Datenverzeichnis umleitet.
    /// </summary>
    [Fact]
    public void AppDataPathProvider_NewNameRedirects()
    {
        Environment.SetEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable, @"C:\NeuerName");
        try
        {
            Assert.Equal(@"C:\NeuerName", new AppDataPathProvider(() => @"C:\Regulaer").GetDataDirectory());
        }
        finally
        {
            Environment.SetEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable, null);
        }
    }

    /// <summary>
    /// Prüft, dass der alte Name den Standortdienst nicht auf den Teststandort umstellt (die Plattform wird erzeugt), der neue Name schon.
    /// </summary>
    [Fact]
    public void LocationServiceSelector_OnlyNewNameActivatesTestMode()
    {
        var platform = new FakeLocationService();
        var platformCreated = 0;
        ILocationService CreatePlatform()
        {
            platformCreated++;
            return platform;
        }

        var oldOnly = LocationServiceSelector.Create(Env(OldName, "testdata"), CreatePlatform);
        var newName = LocationServiceSelector.Create(Env(TestDataPaths.TestDataPathEnvironmentVariable, "testdata"), CreatePlatform);

        Assert.Equal(1, platformCreated);
        Assert.Same(platform, oldOnly);
        Assert.IsType<TestLocationService>(newName);
    }

    /// <summary>
    /// Prüft, dass der alte Name weder den Endpunkt überschreibt noch den Abruf verweigert, der neue Name den Testmodus aktiviert.
    /// </summary>
    [Fact]
    public void PriceApiOptions_OnlyNewNameActivatesTestMode()
    {
        var oldOnly = PriceApiOptions.FromEnvironment(name => name switch
        {
            OldName => "testdata",
            PriceApiOptions.BaseUrlEnvironmentVariable => "http://127.0.0.1:1/json/",
            _ => null,
        });
        var newName = PriceApiOptions.FromEnvironment(Env(TestDataPaths.TestDataPathEnvironmentVariable, "testdata"));

        Assert.False(oldOnly.EndpointNotConfigured);
        Assert.Equal(new Uri(PriceApiOptions.DefaultBaseUrl), oldOnly.BaseUrl);
        Assert.False(oldOnly.AllowLoopbackHttp);
        Assert.True(newName.EndpointNotConfigured);
    }

    /// <summary>
    /// Prüft, dass der alte Name die echte Schlüsselablage nicht ersetzt, der neue Name die isolierte Ablage im Speicher liefert.
    /// </summary>
    [Fact]
    public void ApiKeyStoreSelector_OnlyNewNameUsesIsolatedStore()
    {
        var platform = new FakeApiKeyStore();

        var oldOnly = ApiKeyStoreSelector.Create(Env(OldName, "testdata"), () => platform);
        var newName = ApiKeyStoreSelector.Create(Env(TestDataPaths.TestDataPathEnvironmentVariable, "testdata"), () => platform);

        Assert.Same(platform, oldOnly);
        Assert.IsType<InMemoryApiKeyStore>(newName);
    }

    /// <summary>
    /// Prüft, dass der alte Name den Test-Schlüssel nicht freischaltet (kein Testmodus im Schlüsselanbieter).
    /// </summary>
    [Fact]
    public async Task ApiKeyProvider_OldNameDoesNotUseTestKey()
    {
        var provider = new ApiKeyProvider(
            new FakeApiKeyStore { Stored = "gespeichert" },
            () => null,
            name => name switch
            {
                OldName => "testdata",
                PriceApiOptions.ApiKeyEnvironmentVariable => "test-key",
                _ => null,
            },
            new ListLogger<ApiKeyProvider>());

        Assert.Equal("gespeichert", await provider.GetApiKeyAsync());
    }
}

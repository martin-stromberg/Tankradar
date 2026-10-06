using Tankradar.MAUI.Models.Map;
using Tankradar.MAUI.Services.Map;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Einstellungen des Kachelabrufs: HTTPS-Pflicht, Testmodus-Endpunkt, Fail-secure ohne Adresse und Einhaltung der Nutzungsrichtlinie.
/// </summary>
public class TileServerOptionsTests_Validation : BaseTest
{
    private static Func<string, string?> Environment(params (string Name, string Value)[] values)
    {
        var map = values.ToDictionary(item => item.Name, item => item.Value);
        return name => map.GetValueOrDefault(name);
    }

    /// <summary>
    /// Prüft, dass produktiv der Standard-Kachelserver per HTTPS mit identifizierender Kennung gilt und die Kachelnummern in die Adresse eingesetzt werden.
    /// </summary>
    [Fact]
    public void Defaults_AreHttpsWithIdentifyingUserAgent()
    {
        var options = TileServerOptions.FromEnvironment(Environment());

        Assert.False(options.EndpointNotConfigured);
        Assert.Equal(new Uri("https://tile.openstreetmap.org/12/2200/1343.png"), options.CreateUri(new TileKey(12, 2200, 1343)));
        Assert.Contains("Tankatlas", options.UserAgent, StringComparison.Ordinal);
        Assert.Contains("de.martinstromberg.tankradar", options.UserAgent, StringComparison.Ordinal);
        Assert.Equal(2, options.MaxConcurrentRequests);
        Assert.True(options.CacheLifetime >= TimeSpan.FromDays(7));
    }

    /// <summary>
    /// Prüft, dass außerhalb des Testmodus ein gesetzter Endpunkt ignoriert wird (kein Umleiten der Kachelabrufe in der Produktion).
    /// </summary>
    [Fact]
    public void OverrideWithoutTestMode_IsIgnored()
    {
        var options = TileServerOptions.FromEnvironment(Environment((TestDataPaths.TileUrlEnvironmentVariable, "http://127.0.0.1:1/")));

        Assert.Equal(TileServerOptions.DefaultUrlTemplate, options.UrlTemplate);
    }

    /// <summary>
    /// Prüft, dass im Testmodus ein Loopback-Endpunkt über HTTP gilt und die Kachelpfade angehängt werden.
    /// </summary>
    [Fact]
    public void TestModeWithUrl_UsesLoopbackEndpoint()
    {
        var options = TileServerOptions.FromEnvironment(Environment(
            (TestDataPaths.TestDataPathEnvironmentVariable, "x"),
            (TestDataPaths.TileUrlEnvironmentVariable, "http://127.0.0.1:5000")));

        Assert.False(options.EndpointNotConfigured);
        Assert.Equal(new Uri("http://127.0.0.1:5000/3/4/5.png"), options.CreateUri(new TileKey(3, 4, 5)));
    }

    /// <summary>
    /// Prüft, dass im Testmodus ohne Adresse keine Kacheln abgerufen werden (kein Rückfall auf den produktiven Server).
    /// </summary>
    [Fact]
    public void TestModeWithoutUrl_RefusesFetching()
    {
        var options = TileServerOptions.FromEnvironment(Environment((TestDataPaths.TestDataPathEnvironmentVariable, "x")));

        Assert.True(options.EndpointNotConfigured);
    }

    /// <summary>
    /// Prüft, dass unsichere oder unvollständige Einstellungen abgelehnt werden.
    /// </summary>
    [Fact]
    public void Validate_RejectsInsecureOrInvalidSettings()
    {
        Assert.Throws<InvalidOperationException>(() => new TileServerOptions { UrlTemplate = "http://tile.example.org/{z}/{x}/{y}.png" }.Validate());
        Assert.Throws<InvalidOperationException>(() => new TileServerOptions { UrlTemplate = "http://127.0.0.1/{z}/{x}/{y}.png" }.Validate());
        Assert.Throws<InvalidOperationException>(() => new TileServerOptions { UrlTemplate = "https://tile.example.org/{z}/{x}.png" }.Validate());
        Assert.Throws<InvalidOperationException>(() => new TileServerOptions { UrlTemplate = "/{z}/{x}/{y}.png" }.Validate());
        Assert.Throws<InvalidOperationException>(() => new TileServerOptions { MaxConcurrentRequests = 3 }.Validate());
        Assert.Throws<InvalidOperationException>(() => new TileServerOptions { CacheLifetime = TimeSpan.FromDays(1) }.Validate());
        Assert.Throws<InvalidOperationException>(() => new TileServerOptions { UserAgent = " " }.Validate());
        Assert.Throws<InvalidOperationException>(() => new TileServerOptions { RequestTimeout = TimeSpan.Zero }.Validate());
        Assert.Throws<ArgumentNullException>(() => TileServerOptions.FromEnvironment(null!));
        new TileServerOptions { UrlTemplate = "http://127.0.0.1/{z}/{x}/{y}.png", AllowLoopbackHttp = true }.Validate();
    }
}

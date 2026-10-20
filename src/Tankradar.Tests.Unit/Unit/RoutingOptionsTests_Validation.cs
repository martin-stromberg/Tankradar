using Tankradar.MAUI.Services.Routing;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass der Routingdienst ausschließlich über HTTPS angesprochen wird und der Endpunkt nur im Testmodus überschreibbar ist.
/// </summary>
public class RoutingOptionsTests_Validation : BaseTest
{
    private static Func<string, string?> Env(params (string Name, string Value)[] values)
    {
        var map = values.ToDictionary(v => v.Name, v => v.Value);
        return name => map.GetValueOrDefault(name);
    }

    /// <summary>
    /// Prüft die Standardwerte: HTTPS, 15 s Zeitlimit, 3 Versuche, 1 s Wartezeit, 1,5 s Mindestabstand.
    /// </summary>
    [Fact]
    public void Default_IsHttpsWithDocumentedValues()
    {
        var options = new RoutingOptions();

        options.Validate();

        Assert.Equal("https://api.openrouteservice.org/", options.BaseUrl.ToString());
        Assert.Equal(TimeSpan.FromSeconds(15), options.RequestTimeout);
        Assert.Equal(3, options.MaxAttempts);
        Assert.Equal(TimeSpan.FromSeconds(1), options.RetryBaseDelay);
        Assert.Equal(TimeSpan.FromMilliseconds(1500), options.MinRequestInterval);
    }

    /// <summary>
    /// Prüft, dass unverschlüsseltes HTTP abgelehnt wird, auch für Loopback ohne Testfreigabe.
    /// </summary>
    [Fact]
    public void Validate_Http_IsRejected()
    {
        Assert.Throws<InvalidOperationException>(() => new RoutingOptions { BaseUrl = new Uri("http://example.test/") }.Validate());
        Assert.Throws<InvalidOperationException>(() => new RoutingOptions { BaseUrl = new Uri("http://127.0.0.1:5000/") }.Validate());
    }

    /// <summary>
    /// Prüft, dass HTTP nur mit Testfreigabe und nur für Loopback erlaubt ist.
    /// </summary>
    [Fact]
    public void Validate_LoopbackHttp_OnlyWithTestFlag()
    {
        new RoutingOptions { BaseUrl = new Uri("http://127.0.0.1:5000/"), AllowLoopbackHttp = true, MinRequestInterval = TimeSpan.Zero }.Validate();

        Assert.Throws<InvalidOperationException>(() =>
            new RoutingOptions { BaseUrl = new Uri("http://example.test/"), AllowLoopbackHttp = true }.Validate());
    }

    /// <summary>
    /// Prüft die Grenzen von Zeitlimit, Versuchen, Wartezeit und Mindestabstand.
    /// </summary>
    [Fact]
    public void Validate_InvalidTimingValues_Throw()
    {
        Assert.Throws<InvalidOperationException>(() => new RoutingOptions { RequestTimeout = TimeSpan.Zero }.Validate());
        Assert.Throws<InvalidOperationException>(() => new RoutingOptions { MaxAttempts = 0 }.Validate());
        Assert.Throws<InvalidOperationException>(() => new RoutingOptions { RetryBaseDelay = TimeSpan.FromSeconds(-1) }.Validate());
        Assert.Throws<InvalidOperationException>(() => new RoutingOptions { MinRequestInterval = TimeSpan.FromMilliseconds(500) }.Validate());
        Assert.Throws<InvalidOperationException>(() => new RoutingOptions { UserAgent = " " }.Validate());
    }

    /// <summary>
    /// Prüft, dass ohne Testmodus der Endpunkt nicht überschrieben werden kann.
    /// </summary>
    [Fact]
    public void FromEnvironment_WithoutTestMode_IgnoresOverride()
    {
        var options = RoutingOptions.FromEnvironment(Env((TestDataPaths.RoutingUrlEnvironmentVariable, "http://127.0.0.1:9999/")));

        Assert.Equal(RoutingOptions.DefaultBaseUrl, options.BaseUrl.ToString());
        Assert.False(options.EndpointNotConfigured);
    }

    /// <summary>
    /// Prüft, dass im Testmodus der Endpunkt überschrieben wird und kurze Wartezeiten gelten.
    /// </summary>
    [Fact]
    public void FromEnvironment_TestMode_UsesOverrideWithShortTimes()
    {
        var options = RoutingOptions.FromEnvironment(Env(
            (TestDataPaths.TestDataPathEnvironmentVariable, "x"),
            (TestDataPaths.RoutingUrlEnvironmentVariable, "http://127.0.0.1:9999")));

        Assert.Equal("http://127.0.0.1:9999/", options.BaseUrl.ToString());
        Assert.True(options.AllowLoopbackHttp);
        Assert.Equal(TimeSpan.Zero, options.MinRequestInterval);
        Assert.False(options.EndpointNotConfigured);
    }

    /// <summary>
    /// Prüft, dass im Testmodus ohne Endpunkt der Abruf verweigert wird (kein Rückfall auf den produktiven Dienst).
    /// </summary>
    [Fact]
    public void FromEnvironment_TestModeWithoutUrl_IsNotConfigured()
    {
        var options = RoutingOptions.FromEnvironment(Env((TestDataPaths.TestDataPathEnvironmentVariable, "x")));

        Assert.True(options.EndpointNotConfigured);
    }

    /// <summary>
    /// Prüft, dass die Namen der Testvariablen getrennt von denen des Preisdienstes sind.
    /// </summary>
    [Fact]
    public void EnvironmentVariables_AreSeparateFromPriceService()
    {
        Assert.Equal("TANKRADAR_ROUTING_URL", RoutingOptions.BaseUrlEnvironmentVariable);
        Assert.Equal("TANKRADAR_ROUTE_API_KEY", RoutingOptions.ApiKeyEnvironmentVariable);
        Assert.NotEqual(TestDataPaths.PriceApiKeyEnvironmentVariable, RoutingOptions.ApiKeyEnvironmentVariable);
    }
}

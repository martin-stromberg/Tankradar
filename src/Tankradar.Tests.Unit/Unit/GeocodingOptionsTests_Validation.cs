using Tankradar.MAUI.Services;
using Tankradar.MAUI.Services.Geocoding;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Einstellungen des Ortssuchdienstes: HTTPS-Pflicht, Mindestabstand von einer Sekunde, Kennung und Endpunkt nur im Testmodus überschreibbar.
/// </summary>
public class GeocodingOptionsTests_Validation : BaseTest
{
    private static Func<string, string?> Env(params (string Name, string Value)[] values)
    {
        var map = values.ToDictionary(v => v.Name, v => v.Value);
        return name => map.GetValueOrDefault(name);
    }

    /// <summary>
    /// Prüft, dass der Standard der produktive HTTPS-Endpunkt mit Richtlinienwerten ist.
    /// </summary>
    [Fact]
    public void Default_IsHttpsWithPolicyValues()
    {
        var options = new GeocodingOptions();

        options.Validate();

        Assert.Equal(Uri.UriSchemeHttps, options.BaseUrl.Scheme);
        Assert.Equal("nominatim.openstreetmap.org", options.BaseUrl.Host);
        Assert.Equal(GeocodingOptions.DefaultMinRequestInterval, options.MinRequestInterval);
        Assert.Contains("Tankatlas", options.UserAgent, StringComparison.Ordinal);
        Assert.Contains(AppConfiguration.DefaultBundleId, options.UserAgent, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass unverschlüsseltes HTTP ohne Testfreigabe und für Nicht-Loopback-Adressen abgelehnt wird.
    /// </summary>
    [Fact]
    public void Validate_Http_IsRejected()
    {
        Assert.Throws<InvalidOperationException>(() => new GeocodingOptions { BaseUrl = new Uri("http://127.0.0.1:5000/") }.Validate());
        Assert.Throws<InvalidOperationException>(() => new GeocodingOptions { BaseUrl = new Uri("http://example.test/"), AllowLoopbackHttp = true }.Validate());
        new GeocodingOptions { BaseUrl = new Uri("http://127.0.0.1:5000/"), AllowLoopbackHttp = true }.Validate();
    }

    /// <summary>
    /// Prüft, dass der Mindestabstand die Nutzungsrichtlinie (eine Anfrage je Sekunde) nicht unterschreiten darf.
    /// </summary>
    [Fact]
    public void Validate_IntervalBelowOneSecond_IsRejected()
    {
        Assert.Throws<InvalidOperationException>(() => new GeocodingOptions { MinRequestInterval = TimeSpan.Zero }.Validate());
        Assert.Throws<InvalidOperationException>(() => new GeocodingOptions { MinRequestInterval = TimeSpan.FromMilliseconds(999) }.Validate());
        new GeocodingOptions { MinRequestInterval = TimeSpan.FromSeconds(2) }.Validate();
    }

    /// <summary>
    /// Prüft, dass Zeitlimit und Kennung gültig sein müssen.
    /// </summary>
    [Fact]
    public void Validate_InvalidTimeoutOrUserAgent_IsRejected()
    {
        Assert.Throws<InvalidOperationException>(() => new GeocodingOptions { RequestTimeout = TimeSpan.Zero }.Validate());
        Assert.Throws<InvalidOperationException>(() => new GeocodingOptions { UserAgent = " " }.Validate());
    }

    /// <summary>
    /// Prüft, dass die Überschreibung des Endpunkts ohne Testverzeichnis wirkungslos bleibt.
    /// </summary>
    [Fact]
    public void FromEnvironment_WithoutTestMode_IgnoresOverride()
    {
        var options = GeocodingOptions.FromEnvironment(Env((TestDataPaths.GeocodingUrlEnvironmentVariable, "http://127.0.0.1:9/")));

        Assert.Equal(GeocodingOptions.DefaultBaseUrl, options.BaseUrl.ToString());
        Assert.False(options.EndpointNotConfigured);
    }

    /// <summary>
    /// Prüft, dass im Testmodus der Endpunkt überschrieben werden kann und der Mindestabstand bestehen bleibt.
    /// </summary>
    [Fact]
    public void FromEnvironment_TestModeWithOverride_UsesMockAndKeepsInterval()
    {
        var options = GeocodingOptions.FromEnvironment(Env(
            (TestDataPaths.TestDataPathEnvironmentVariable, "C:\\test"),
            (TestDataPaths.GeocodingUrlEnvironmentVariable, "http://127.0.0.1:9")));

        Assert.Equal("http://127.0.0.1:9/", options.BaseUrl.ToString());
        Assert.True(options.AllowLoopbackHttp);
        Assert.Equal(GeocodingOptions.DefaultMinRequestInterval, options.MinRequestInterval);
    }

    /// <summary>
    /// Prüft, dass im Testmodus ohne Adresse die Auflösung verweigert wird (kein Rückfall auf den produktiven Dienst).
    /// </summary>
    [Fact]
    public void FromEnvironment_TestModeWithoutOverride_IsNotConfigured()
    {
        var options = GeocodingOptions.FromEnvironment(Env((TestDataPaths.TestDataPathEnvironmentVariable, "C:\\test")));

        Assert.True(options.EndpointNotConfigured);
    }

    /// <summary>
    /// Prüft, dass ein nicht lokaler Endpunkt im Testmodus nicht über HTTP erreichbar gemacht werden kann.
    /// </summary>
    [Fact]
    public void FromEnvironment_TestModeWithNonLoopbackHttp_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => GeocodingOptions.FromEnvironment(Env(
            (TestDataPaths.TestDataPathEnvironmentVariable, "C:\\test"),
            (TestDataPaths.GeocodingUrlEnvironmentVariable, "http://nominatim.openstreetmap.org/"))));
    }
}

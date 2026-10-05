using Tankradar.MAUI.Services.Pricing;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass die Preis-API ausschließlich über HTTPS angesprochen wird und der Endpunkt nur im Testmodus überschreibbar ist.
/// </summary>
public class PriceApiOptionsTests_Validation : BaseTest
{
    private static Func<string, string?> Env(params (string Name, string Value)[] values)
    {
        var map = values.ToDictionary(v => v.Name, v => v.Value);
        return name => map.GetValueOrDefault(name);
    }

    /// <summary>
    /// Prüft, dass der Standard der produktive HTTPS-Endpunkt ist und gültig ist.
    /// </summary>
    [Fact]
    public void Default_IsHttpsAndValid()
    {
        var options = new PriceApiOptions();

        options.Validate();

        Assert.Equal(Uri.UriSchemeHttps, options.BaseUrl.Scheme);
    }

    /// <summary>
    /// Prüft, dass unverschlüsseltes HTTP abgelehnt wird, auch für Loopback ohne Testfreigabe.
    /// </summary>
    [Fact]
    public void Validate_Http_IsRejected()
    {
        Assert.Throws<InvalidOperationException>(() => new PriceApiOptions { BaseUrl = new Uri("http://example.test/json/") }.Validate());
        Assert.Throws<InvalidOperationException>(() => new PriceApiOptions { BaseUrl = new Uri("http://127.0.0.1:5000/json/") }.Validate());
    }

    /// <summary>
    /// Prüft, dass HTTP nur mit Testfreigabe und nur für Loopback erlaubt ist.
    /// </summary>
    [Fact]
    public void Validate_LoopbackHttp_OnlyWithTestFlag()
    {
        new PriceApiOptions { BaseUrl = new Uri("http://127.0.0.1:5000/json/"), AllowLoopbackHttp = true }.Validate();

        Assert.Throws<InvalidOperationException>(() =>
            new PriceApiOptions { BaseUrl = new Uri("http://example.test/json/"), AllowLoopbackHttp = true }.Validate());
    }

    /// <summary>
    /// Prüft, dass unsinnige Zeit- und Wiederholungswerte abgelehnt werden.
    /// </summary>
    [Fact]
    public void Validate_InvalidLimits_AreRejected()
    {
        Assert.Throws<InvalidOperationException>(() => new PriceApiOptions { MaxAttempts = 0 }.Validate());
        Assert.Throws<InvalidOperationException>(() => new PriceApiOptions { RequestTimeout = TimeSpan.Zero }.Validate());
        Assert.Throws<InvalidOperationException>(() => new PriceApiOptions { MaxRadiusKm = 0 }.Validate());
    }

    /// <summary>
    /// Prüft, dass die Überschreibung des Endpunkts ohne Testverzeichnis wirkungslos bleibt.
    /// </summary>
    [Fact]
    public void FromEnvironment_OverrideWithoutTestMode_IsIgnored()
    {
        var options = PriceApiOptions.FromEnvironment(Env((PriceApiOptions.BaseUrlEnvironmentVariable, "http://127.0.0.1:9/json/")));

        Assert.Equal(PriceApiOptions.DefaultBaseUrl, options.BaseUrl.ToString());
        Assert.False(options.AllowLoopbackHttp);
    }

    /// <summary>
    /// Prüft, dass im Testmodus der Endpunkt überschrieben wird und kurze Wartezeiten gelten.
    /// </summary>
    [Fact]
    public void FromEnvironment_OverrideInTestMode_IsApplied()
    {
        var options = PriceApiOptions.FromEnvironment(Env(
            (TestDataPaths.TestDataPathEnvironmentVariable, "C:\\Temp\\test"),
            (PriceApiOptions.BaseUrlEnvironmentVariable, "http://127.0.0.1:9/json")));

        Assert.Equal("http://127.0.0.1:9/json/", options.BaseUrl.ToString());
        Assert.True(options.AllowLoopbackHttp);
        Assert.Equal(TimeSpan.Zero, options.MinRequestInterval);
    }

    /// <summary>
    /// Prüft, dass auch im Testmodus ein entfernter HTTP-Endpunkt abgelehnt wird.
    /// </summary>
    [Fact]
    public void FromEnvironment_RemoteHttpInTestMode_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => PriceApiOptions.FromEnvironment(Env(
            (TestDataPaths.TestDataPathEnvironmentVariable, "C:\\Temp\\test"),
            (PriceApiOptions.BaseUrlEnvironmentVariable, "http://example.test/json/"))));
    }
}

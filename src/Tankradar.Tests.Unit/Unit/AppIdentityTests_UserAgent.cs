using Tankradar.MAUI.Services;
using Tankradar.MAUI.Services.Geocoding;
using Tankradar.MAUI.Services.Map;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Kennung der App gegenüber den OpenStreetMap-Diensten: tatsächliche App-Version, Kontaktangabe (Projekt-URL) und App-Kennung.
/// </summary>
public class AppIdentityTests_UserAgent : BaseTest
{
    private static Func<string, string?> NoEnvironment()
    {
        return _ => null;
    }

    /// <summary>
    /// Prüft Aufbau und Inhalt des <c>User-Agent</c> mit der übergebenen Version.
    /// </summary>
    [Fact]
    public void BuildUserAgent_ContainsVersionContactAndBundleId()
    {
        var userAgent = AppIdentity.BuildUserAgent("1.2.3");

        Assert.Equal("Tankatlas/1.2.3 (+https://github.com/martin-stromberg/Tankradar; de.martinstromberg.tankradar)", userAgent);
    }

    /// <summary>
    /// Prüft, dass fehlende oder unsichere Versionsangaben nicht in den Anfragekopf gelangen.
    /// </summary>
    /// <param name="version">Die Versionsangabe.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1.0 (evil)")]
    [InlineData("1.0\r\nX-Injected: 1")]
    public void BuildUserAgent_InvalidVersion_UsesUnknownVersion(string? version)
    {
        Assert.StartsWith("Tankatlas/0.0.0 (", AppIdentity.BuildUserAgent(version), StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass Kachel- und Ortssuch-Einstellungen die App-Version und Kontaktangabe tragen.
    /// </summary>
    [Fact]
    public void Options_FromEnvironment_CarryAppVersion()
    {
        var tiles = TileServerOptions.FromEnvironment(NoEnvironment(), "0.1.14");
        var geocoding = GeocodingOptions.FromEnvironment(NoEnvironment(), "0.1.14");

        Assert.Contains("Tankatlas/0.1.14", tiles.UserAgent, StringComparison.Ordinal);
        Assert.Contains("https://github.com/martin-stromberg/Tankradar", tiles.UserAgent, StringComparison.Ordinal);
        Assert.Equal(tiles.UserAgent, geocoding.UserAgent);
    }
}

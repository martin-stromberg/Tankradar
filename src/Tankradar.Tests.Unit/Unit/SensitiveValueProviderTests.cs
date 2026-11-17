using Tankradar.MAUI.Services.Diagnostics;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass der Geheimnis-Lieferant die aktiven Schlüssel beider Dienste einsammelt.
/// </summary>
public class SensitiveValueProviderTests : BaseTest
{
    /// <summary>
    /// Prüft, dass Preisdienst- und Routensuchschlüssel gemeinsam geliefert werden.
    /// </summary>
    [Fact]
    public async Task GetValues_BothKeys_Collected()
    {
        var price = new FakeApiKeyProvider { Value = "preis-schluessel-123" };
        var route = new FakeRouteSearchAccess { Key = "route-schluessel-456" };
        var provider = new SensitiveValueProvider(price, route);

        var values = await provider.GetValuesAsync();

        Assert.Equal(["preis-schluessel-123", "route-schluessel-456"], values);
    }

    /// <summary>
    /// Prüft, dass fehlende und leere Schlüssel ausgelassen werden.
    /// </summary>
    [Fact]
    public async Task GetValues_MissingKeys_Skipped()
    {
        var price = new FakeApiKeyProvider { Value = null };
        var route = new FakeRouteSearchAccess { Key = "  " };
        var provider = new SensitiveValueProvider(price, route);

        Assert.Empty(await provider.GetValuesAsync());
    }

    /// <summary>
    /// Prüft, dass ein identischer Schlüssel in beiden Diensten nur einmal geliefert wird.
    /// </summary>
    [Fact]
    public async Task GetValues_SameKey_Deduplicated()
    {
        var price = new FakeApiKeyProvider { Value = "gemeinsamer-schluessel" };
        var route = new FakeRouteSearchAccess { Key = "gemeinsamer-schluessel" };
        var provider = new SensitiveValueProvider(price, route);

        Assert.Equal(["gemeinsamer-schluessel"], await provider.GetValuesAsync());
    }
}

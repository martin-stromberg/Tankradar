using System.Net.Http.Headers;
using Tankradar.MAUI.Services.Map;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass die Angabe <c>Age</c> die Frische einer Kachel kürzt (RFC 9111) und <see cref="TileFreshness"/> Lebensdauer und Herkunft der Angabe liefert.
/// </summary>
public class TileCachePolicyTests_Age : BaseTest
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Fallback = TimeSpan.FromDays(7);

    /// <summary>
    /// Prüft, dass <c>max-age</c> um das Alter gekürzt wird.
    /// </summary>
    [Fact]
    public void Evaluate_MaxAgeMinusAge()
    {
        var freshness = TileCachePolicy.Evaluate(new CacheControlHeaderValue { MaxAge = TimeSpan.FromMinutes(10) }, null, null, Now, Fallback, TimeSpan.FromMinutes(4));

        Assert.Equal(Now.AddMinutes(6), freshness.ExpiresUtc);
    }

    /// <summary>
    /// Prüft, dass ein Alter ab der Lebensdauer die Kachel sofort ablaufen lässt, nie in die Vergangenheit.
    /// </summary>
    /// <param name="ageSeconds">Das Alter in Sekunden.</param>
    [Theory]
    [InlineData(600)]
    [InlineData(5000)]
    public void Evaluate_AgeGreaterThanLifetime_ExpiresImmediately(int ageSeconds)
    {
        var freshness = TileCachePolicy.Evaluate(new CacheControlHeaderValue { MaxAge = TimeSpan.FromMinutes(10) }, null, null, Now, Fallback, TimeSpan.FromSeconds(ageSeconds));

        Assert.Equal(Now, freshness.ExpiresUtc);
    }

    /// <summary>
    /// Prüft, dass auch die Lebensdauer aus <c>Expires</c> und <c>Date</c> um das Alter gekürzt wird.
    /// </summary>
    [Fact]
    public void Evaluate_ExpiresMinusAge()
    {
        var serverDate = Now.AddSeconds(-30);
        var freshness = TileCachePolicy.Evaluate(null, serverDate.AddMinutes(10), serverDate, Now, Fallback, TimeSpan.FromMinutes(3));

        Assert.Equal(Now.AddMinutes(7), freshness.ExpiresUtc);
    }

    /// <summary>
    /// Prüft, dass auch der Rückfall um das Alter gekürzt wird und negative Altersangaben als 0 gelten.
    /// </summary>
    [Fact]
    public void Evaluate_FallbackMinusAge()
    {
        Assert.Equal(Now + Fallback - TimeSpan.FromHours(1), TileCachePolicy.Evaluate(null, null, null, Now, Fallback, TimeSpan.FromHours(1)).ExpiresUtc);
        Assert.Equal(Now + Fallback, TileCachePolicy.Evaluate(null, null, null, Now, Fallback, TimeSpan.FromHours(-1)).ExpiresUtc);
    }

    /// <summary>
    /// Prüft, dass <c>no-cache</c> und <c>no-store</c> vom Alter unberührt bleiben.
    /// </summary>
    [Fact]
    public void Evaluate_NoCacheAndNoStore_IgnoreAge()
    {
        var noCache = TileCachePolicy.Evaluate(new CacheControlHeaderValue { NoCache = true, MaxAge = TimeSpan.FromHours(1) }, null, null, Now, Fallback, TimeSpan.FromMinutes(5));
        var noStore = TileCachePolicy.Evaluate(new CacheControlHeaderValue { NoStore = true }, null, null, Now, Fallback, TimeSpan.FromMinutes(5));

        Assert.Equal(Now, noCache.ExpiresUtc);
        Assert.False(noCache.NoStore);
        Assert.Equal(Now, noStore.ExpiresUtc);
        Assert.True(noStore.NoStore);
    }

    /// <summary>
    /// Prüft Lebensdauer und Kennzeichen, ob die Angabe vom Server stammt.
    /// </summary>
    [Fact]
    public void Evaluate_LifetimeAndExplicitFlags()
    {
        var maxAge = TileCachePolicy.Evaluate(new CacheControlHeaderValue { MaxAge = TimeSpan.FromMinutes(10) }, null, null, Now, Fallback, TimeSpan.FromMinutes(4));
        var fallback = TileCachePolicy.Evaluate(null, null, null, Now, Fallback);
        var noCache = TileCachePolicy.Evaluate(new CacheControlHeaderValue { NoCache = true }, null, null, Now, Fallback);

        Assert.Equal(TimeSpan.FromMinutes(10), maxAge.Lifetime);
        Assert.True(maxAge.IsExplicit);
        Assert.Equal(Fallback, fallback.Lifetime);
        Assert.False(fallback.IsExplicit);
        Assert.Equal(TimeSpan.Zero, noCache.Lifetime);
        Assert.True(noCache.IsExplicit);
    }
}

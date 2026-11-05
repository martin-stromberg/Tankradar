using Tankradar.MAUI.Services.Pricing;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Bewertung des von der Plattform gemeldeten Netzzugangs.
/// </summary>
public class NetworkAccessTests_Reachability : BaseTest
{
    /// <summary>
    /// Prüft, dass nur „kein Netz“ und „nur lokal“ als offline gelten und „unbekannt“ einen Abruf zulässt.
    /// </summary>
    /// <param name="access">Der gemeldete Netzzugang.</param>
    /// <param name="expected">Ob ein Abruf versucht werden soll.</param>
    [Theory]
    [InlineData(NetworkAccess.Internet, true)]
    [InlineData(NetworkAccess.ConstrainedInternet, true)]
    [InlineData(NetworkAccess.Unknown, true)]
    [InlineData(NetworkAccess.Local, false)]
    [InlineData(NetworkAccess.None, false)]
    public void IsReachable_MapsAccess(NetworkAccess access, bool expected)
    {
        Assert.Equal(expected, MauiNetworkStatusSource.IsReachable(access));
    }
}

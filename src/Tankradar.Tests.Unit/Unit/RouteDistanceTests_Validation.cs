using Tankradar.MAUI.Models.Routing;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Abstandsstufen zur Route.
/// </summary>
public class RouteDistanceTests_Validation : BaseTest
{
    /// <summary>
    /// Prüft die Stufen und den Standard.
    /// </summary>
    [Fact]
    public void Options_AreOneTwoFiveTenWithDefaultTwo()
    {
        Assert.Equal([1, 2, 5, 10], RouteDistance.Options);
        Assert.Equal(2, RouteDistance.Default);
        Assert.True(RouteDistance.IsValid(RouteDistance.Default));
    }

    /// <summary>
    /// Prüft, dass nur die Stufen gültig sind.
    /// </summary>
    /// <param name="distanceKm">Der Abstand.</param>
    /// <param name="expected">Das erwartete Ergebnis.</param>
    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(5, true)]
    [InlineData(10, true)]
    [InlineData(0, false)]
    [InlineData(3, false)]
    [InlineData(11, false)]
    [InlineData(-2, false)]
    public void IsValid_ReturnsExpected(int distanceKm, bool expected)
    {
        Assert.Equal(expected, RouteDistance.IsValid(distanceKm));
    }

    /// <summary>
    /// Prüft den Schlüssel für Automatisierungskennungen.
    /// </summary>
    [Fact]
    public void GetKey_ReturnsInvariantNumber()
    {
        Assert.Equal("10", RouteDistance.GetKey(10));
    }
}

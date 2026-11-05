using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Services.Routing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Anzahl, Lage und Radius der Abfragepunkte entlang einer Route sowie die Obergrenzen.
/// </summary>
public class RouteSamplerTests_Points : BaseTest
{
    private static readonly RouteSearchOptions Options = RouteTestOptions.Relaxed();

    /// <summary>
    /// Prüft, dass eine kurze Route eine einzige Abfrage in der Streckenmitte ergibt.
    /// </summary>
    [Fact]
    public void TryCreateQueryPoints_ShortRoute_ReturnsSingleQueryAtMidpoint()
    {
        var route = RouteTestData.NorthFromBerlin(0.2);

        Assert.True(RouteSampler.TryCreateQueryPoints(route, 2, Options, out var points));

        var point = Assert.Single(points);
        Assert.Equal(route.LengthKm / 2, point.AlongKm, 6);
        Assert.Equal(52.62, point.Latitude, 4);
        Assert.InRange(point.RadiusKm, 1, 25);
    }

    /// <summary>
    /// Prüft die Anzahl der Abfragen je Abstandsstufe bei 100 km Länge (Schritt = 2 * (25 - d) * 0,95).
    /// </summary>
    /// <param name="distanceKm">Der Abstand zur Route.</param>
    /// <param name="expectedCount">Die erwartete Anzahl der Abfragen.</param>
    [Theory]
    [InlineData(1, 3)]
    [InlineData(2, 3)]
    [InlineData(5, 3)]
    [InlineData(10, 4)]
    public void TryCreateQueryPoints_HundredKilometers_ReturnsExpectedCount(int distanceKm, int expectedCount)
    {
        var route = RouteTestData.NorthFromBerlin(100 / 111.195);

        Assert.True(RouteSampler.TryCreateQueryPoints(route, distanceKm, Options, out var points));

        Assert.Equal(expectedCount, points.Count);
    }

    /// <summary>
    /// Prüft, dass die Abfragen gleichmäßig verteilt sind und aufsteigende Streckenpositionen haben.
    /// </summary>
    [Fact]
    public void TryCreateQueryPoints_LongRoute_SpreadsPointsEvenly()
    {
        var route = RouteTestData.NorthFromBerlin(300 / 111.195);

        Assert.True(RouteSampler.TryCreateQueryPoints(route, 2, Options, out var points));

        var section = route.LengthKm / points.Count;
        for (var i = 0; i < points.Count; i++)
        {
            Assert.Equal((i + 0.5) * section, points[i].AlongKm, 6);
        }
    }

    /// <summary>
    /// Prüft, dass kein Radius 25 km überschreitet oder 1 km unterschreitet und der Radius ganzzahlig ist.
    /// </summary>
    /// <param name="distanceKm">Der Abstand zur Route.</param>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void TryCreateQueryPoints_AllRadii_StayWithinPriceApiLimit(int distanceKm)
    {
        foreach (var lengthKm in new[] { 0.5, 3, 40, 123, 399 })
        {
            var route = RouteTestData.NorthFromBerlin(lengthKm / 111.195);

            Assert.True(RouteSampler.TryCreateQueryPoints(route, distanceKm, Options, out var points));
            Assert.All(points, point => Assert.InRange(point.RadiusKm, 1, 25));
        }
    }

    /// <summary>
    /// Prüft die Obergrenze der Routenlänge (400 km).
    /// </summary>
    [Fact]
    public void TryCreateQueryPoints_LongerThanMaximum_ReturnsFalse()
    {
        var route = RouteTestData.NorthFromBerlin(401 / 111.195);

        Assert.False(RouteSampler.TryCreateQueryPoints(route, 2, Options, out var points));
        Assert.Empty(points);
    }

    /// <summary>
    /// Prüft, dass 400 km bei 10 km Abstand genau 15 Abfragen ergeben und die Obergrenze der Abfragen greift.
    /// </summary>
    [Fact]
    public void TryCreateQueryPoints_MaxLengthAtWidestDistance_UsesAtMostFifteenQueries()
    {
        var route = RouteTestData.NorthFromBerlin(399.5 / 111.195);

        Assert.True(RouteSampler.TryCreateQueryPoints(route, 10, Options, out var points));
        Assert.Equal(15, points.Count);

        var strict = new RouteSearchOptions { MaxQueries = 14 };
        Assert.False(RouteSampler.TryCreateQueryPoints(route, 10, strict, out _));
    }

    /// <summary>
    /// Prüft die vorläufige Abfragegrenze der Auslieferung: Höchstens drei Abfragen; je Abstand ergibt sich die durchsuchbare Routenlänge (3 × Schritt).
    /// </summary>
    /// <param name="distanceKm">Der Abstand zur Route.</param>
    /// <param name="expectedMaxKm">Die erwartete durchsuchbare Länge.</param>
    [Theory]
    [InlineData(1, 136.8)]
    [InlineData(2, 131.1)]
    [InlineData(5, 114)]
    [InlineData(10, 85.5)]
    public void MaxSearchLengthKm_DefaultOptions_IsThreeSteps(int distanceKm, double expectedMaxKm)
    {
        Assert.Equal(expectedMaxKm, RouteSampler.MaxSearchLengthKm(distanceKm, new RouteSearchOptions()), 6);
    }

    /// <summary>
    /// Prüft, dass die durchsuchbare Länge an der Grenze genau drei Abfragen ergibt und knapp darüber abgelehnt wird.
    /// </summary>
    /// <param name="distanceKm">Der Abstand zur Route.</param>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void TryCreateQueryPoints_DefaultOptions_AcceptsUpToLimitAndRejectsAbove(int distanceKm)
    {
        var options = new RouteSearchOptions();
        var max = RouteSampler.MaxSearchLengthKm(distanceKm, options);

        Assert.True(RouteSampler.TryCreateQueryPoints(RouteTestData.NorthFromBerlin((max - 0.5) / 111.195), distanceKm, options, out var accepted));
        Assert.Equal(3, accepted.Count);
        Assert.False(RouteSampler.TryCreateQueryPoints(RouteTestData.NorthFromBerlin((max + 0.5) / 111.195), distanceKm, options, out var rejected));
        Assert.Empty(rejected);
    }

    /// <summary>
    /// Prüft die durchsuchbare Länge bei anderen Einstellungen: Die Routenlängengrenze und ein Abstand ab dem Abfrageradius begrenzen sie.
    /// </summary>
    [Fact]
    public void MaxSearchLengthKm_LengthLimitAndExcessiveDistance_AreRespected()
    {
        Assert.Equal(50, RouteSampler.MaxSearchLengthKm(2, new RouteSearchOptions { MaxRouteLengthKm = 50 }));
        Assert.Equal(0, RouteSampler.MaxSearchLengthKm(25, new RouteSearchOptions()));
        Assert.Throws<ArgumentNullException>(() => RouteSampler.MaxSearchLengthKm(2, null!));
    }

    /// <summary>
    /// Prüft, dass das Ergebnis deterministisch ist.
    /// </summary>
    [Fact]
    public void TryCreateQueryPoints_SameInput_ReturnsSameResult()
    {
        var route = RouteTestData.Curved(1.2, 0.05);

        RouteSampler.TryCreateQueryPoints(route, 5, Options, out var first);
        RouteSampler.TryCreateQueryPoints(route, 5, Options, out var second);

        Assert.Equal(first, second);
    }

    /// <summary>
    /// Prüft, dass ein Abstand, der den Abfrageradius erreicht, abgelehnt wird.
    /// </summary>
    [Fact]
    public void TryCreateQueryPoints_DistanceNotBelowRadius_ReturnsFalse()
    {
        var route = RouteTestData.NorthFromBerlin(0.2);

        Assert.False(RouteSampler.TryCreateQueryPoints(route, 25, Options, out _));
    }
}

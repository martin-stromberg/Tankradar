using Tankradar.MAUI.Services.Routing;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Einstellungen der Routensuche (Abfrageradius, Überdeckungsfaktor, Obergrenzen).
/// </summary>
public class RouteSearchOptionsTests_Validation : BaseTest
{
    /// <summary>
    /// Prüft die Standardwerte.
    /// </summary>
    [Fact]
    public void Default_HasDocumentedValues()
    {
        var options = new RouteSearchOptions();

        options.Validate();

        Assert.Equal(25, options.QueryRadiusKm);
        Assert.Equal(0.95, options.CoverageFactor);
        Assert.Equal(3, options.MaxQueries);
        Assert.Equal(RouteSearchOptions.DefaultMaxQueries, options.MaxQueries);
        Assert.Equal(TimeSpan.FromSeconds(10), options.MinQueryInterval);
        Assert.Equal(RouteSearchOptions.DefaultMinQueryInterval, options.MinQueryInterval);
        Assert.Equal(400, options.MaxRouteLengthKm);
        Assert.Equal(0.2, options.SamePlaceKm);
    }

    /// <summary>
    /// Prüft, dass der Abfrageradius 25 km nicht überschreiten darf (Grenze der Preisquelle).
    /// </summary>
    /// <param name="radius">Der Radius.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(25.5)]
    [InlineData(double.NaN)]
    public void Validate_InvalidQueryRadius_Throws(double radius)
    {
        Assert.Throws<InvalidOperationException>(() => new RouteSearchOptions { QueryRadiusKm = radius }.Validate());
    }

    /// <summary>
    /// Prüft die Grenzen des Überdeckungsfaktors.
    /// </summary>
    /// <param name="factor">Der Faktor.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1.01)]
    [InlineData(-0.5)]
    public void Validate_InvalidCoverageFactor_Throws(double factor)
    {
        Assert.Throws<InvalidOperationException>(() => new RouteSearchOptions { CoverageFactor = factor }.Validate());
    }

    /// <summary>
    /// Prüft die Grenzen der Obergrenzen.
    /// </summary>
    [Fact]
    public void Validate_InvalidLimits_Throw()
    {
        Assert.Throws<InvalidOperationException>(() => new RouteSearchOptions { MaxQueries = 0 }.Validate());
        Assert.Throws<InvalidOperationException>(() => new RouteSearchOptions { MaxRouteLengthKm = 0 }.Validate());
        Assert.Throws<InvalidOperationException>(() => new RouteSearchOptions { SamePlaceKm = -1 }.Validate());
        Assert.Throws<InvalidOperationException>(() => new RouteSearchOptions { MinQueryInterval = TimeSpan.FromSeconds(-1) }.Validate());
        Assert.Throws<InvalidOperationException>(() => new RouteSearchOptions { QueryIntervalReserve = TimeSpan.FromSeconds(-1) }.Validate());
    }

    /// <summary>
    /// Prüft, dass außerhalb des Testmodus immer die Standardwerte gelten, auch wenn die Testvariablen gesetzt sind.
    /// </summary>
    [Fact]
    public void FromEnvironment_OutsideTestMode_UsesDefaultsAndIgnoresTestVariables()
    {
        var options = RouteSearchOptions.FromEnvironment(name => name == TestDataPaths.RouteQueryIntervalEnvironmentVariable ? "5" : null);

        Assert.Equal(RouteSearchOptions.DefaultMinQueryInterval, options.MinQueryInterval);
        Assert.Equal(RouteSearchOptions.DefaultMaxQueries, options.MaxQueries);
    }

    /// <summary>
    /// Prüft, dass im Testmodus der Mindestabstand 0 ist oder aus der Testvariablen stammt (ungültige Werte ergeben 0).
    /// </summary>
    /// <param name="value">Der Wert der Testvariable in Millisekunden.</param>
    /// <param name="expectedMilliseconds">Der erwartete Mindestabstand in Millisekunden.</param>
    [Theory]
    [InlineData(null, 0)]
    [InlineData("250", 250)]
    [InlineData("abc", 0)]
    [InlineData("-5", 0)]
    public void FromEnvironment_TestMode_ShortensTheInterval(string? value, int expectedMilliseconds)
    {
        var options = RouteSearchOptions.FromEnvironment(name => name switch
        {
            var n when n == TestDataPaths.TestDataPathEnvironmentVariable => "testdata",
            var n when n == TestDataPaths.RouteQueryIntervalEnvironmentVariable => value,
            _ => null,
        });

        Assert.Equal(TimeSpan.FromMilliseconds(expectedMilliseconds), options.MinQueryInterval);
        Assert.Equal(RouteSearchOptions.DefaultMaxQueries, options.MaxQueries);
        Assert.Throws<ArgumentNullException>(() => RouteSearchOptions.FromEnvironment(null!));
    }
}

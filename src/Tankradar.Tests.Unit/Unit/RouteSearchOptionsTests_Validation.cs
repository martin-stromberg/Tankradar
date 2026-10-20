using Tankradar.MAUI.Services.Routing;

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
        Assert.Equal(15, options.MaxQueries);
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
    }
}

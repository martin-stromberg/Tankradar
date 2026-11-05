using Tankradar.MAUI.Models;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Schreibt die Namen der Enum-Werte als Persistenzvertrag fest, da sie als Text in der Datenbank stehen.
/// </summary>
public class SettingsEnumTests_Persistence : BaseTest
{
    /// <summary>
    /// Prüft den Persistenznamen jeder Spritsorte.
    /// </summary>
    /// <param name="value">Die Spritsorte.</param>
    /// <param name="expectedName">Der erwartete gespeicherte Name.</param>
    [Theory]
    [InlineData(FuelType.SuperE5, "SuperE5")]
    [InlineData(FuelType.SuperE10, "SuperE10")]
    [InlineData(FuelType.Diesel, "Diesel")]
    public void FuelType_HasStablePersistenceName(FuelType value, string expectedName)
    {
        Assert.Equal(expectedName, value.ToString());
        Assert.Equal(value, Enum.Parse<FuelType>(expectedName));
    }

    /// <summary>
    /// Prüft den Persistenznamen jeder Standortnutzung.
    /// </summary>
    /// <param name="value">Die Standortnutzung.</param>
    /// <param name="expectedName">Der erwartete gespeicherte Name.</param>
    [Theory]
    [InlineData(GpsUsage.Always, "Always")]
    [InlineData(GpsUsage.WhileInUse, "WhileInUse")]
    [InlineData(GpsUsage.Never, "Never")]
    public void GpsUsage_HasStablePersistenceName(GpsUsage value, string expectedName)
    {
        Assert.Equal(expectedName, value.ToString());
        Assert.Equal(value, Enum.Parse<GpsUsage>(expectedName));
    }

    /// <summary>
    /// Prüft den Persistenznamen jeder Ergebnisansicht.
    /// </summary>
    /// <param name="value">Die Ergebnisansicht.</param>
    /// <param name="expectedName">Der erwartete gespeicherte Name.</param>
    [Theory]
    [InlineData(ResultView.List, "List")]
    [InlineData(ResultView.Map, "Map")]
    public void ResultView_HasStablePersistenceName(ResultView value, string expectedName)
    {
        Assert.Equal(expectedName, value.ToString());
        Assert.Equal(value, Enum.Parse<ResultView>(expectedName));
    }

    /// <summary>
    /// Prüft den Persistenznamen jeder Ergebnissortierung.
    /// </summary>
    /// <param name="value">Die Ergebnissortierung.</param>
    /// <param name="expectedName">Der erwartete gespeicherte Name.</param>
    [Theory]
    [InlineData(ResultSortOrder.Price, "Price")]
    [InlineData(ResultSortOrder.Distance, "Distance")]
    [InlineData(ResultSortOrder.Name, "Name")]
    public void ResultSortOrder_HasStablePersistenceName(ResultSortOrder value, string expectedName)
    {
        Assert.Equal(expectedName, value.ToString());
        Assert.Equal(value, Enum.Parse<ResultSortOrder>(expectedName));
    }

    /// <summary>
    /// Prüft, dass kein Enum um einen nicht festgeschriebenen Wert erweitert wurde.
    /// </summary>
    [Fact]
    public void Enums_HaveExpectedValueCounts()
    {
        Assert.Equal(3, Enum.GetValues<FuelType>().Length);
        Assert.Equal(3, Enum.GetValues<GpsUsage>().Length);
        Assert.Equal(2, Enum.GetValues<ResultView>().Length);
        Assert.Equal(3, Enum.GetValues<ResultSortOrder>().Length);
    }
}

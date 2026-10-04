using Tankradar.MAUI.Models;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die zentralen deutschen Anzeigenamen in <see cref="SettingsTexts"/>.
/// </summary>
public class SettingsTextsTests_Labels : BaseTest
{
    /// <summary>
    /// Prüft die Anzeigenamen der Spritsorten.
    /// </summary>
    /// <param name="value">Die Spritsorte.</param>
    /// <param name="expected">Der erwartete Anzeigename.</param>
    [Theory]
    [InlineData(FuelType.SuperE5, "Super E5")]
    [InlineData(FuelType.SuperE10, "Super E10")]
    [InlineData(FuelType.Diesel, "Diesel")]
    public void GetLabel_FuelType_ReturnsGermanName(FuelType value, string expected)
    {
        Assert.Equal(expected, SettingsTexts.GetLabel(value));
    }

    /// <summary>
    /// Prüft die Anzeigenamen der Standortnutzung.
    /// </summary>
    /// <param name="value">Die Standortnutzung.</param>
    /// <param name="expected">Der erwartete Anzeigename.</param>
    [Theory]
    [InlineData(GpsUsage.Always, "Immer")]
    [InlineData(GpsUsage.WhileInUse, "Nur bei Nutzung")]
    [InlineData(GpsUsage.Never, "Nie")]
    public void GetLabel_GpsUsage_ReturnsGermanName(GpsUsage value, string expected)
    {
        Assert.Equal(expected, SettingsTexts.GetLabel(value));
    }

    /// <summary>
    /// Prüft die Anzeigenamen der Ergebnisansicht.
    /// </summary>
    /// <param name="value">Die Ergebnisansicht.</param>
    /// <param name="expected">Der erwartete Anzeigename.</param>
    [Theory]
    [InlineData(ResultView.List, "Liste")]
    [InlineData(ResultView.Map, "Karte")]
    public void GetLabel_ResultView_ReturnsGermanName(ResultView value, string expected)
    {
        Assert.Equal(expected, SettingsTexts.GetLabel(value));
    }

    /// <summary>
    /// Prüft die Anzeigenamen der Ergebnissortierung.
    /// </summary>
    /// <param name="value">Die Ergebnissortierung.</param>
    /// <param name="expected">Der erwartete Anzeigename.</param>
    [Theory]
    [InlineData(ResultSortOrder.Price, "Preis")]
    [InlineData(ResultSortOrder.Distance, "Entfernung")]
    [InlineData(ResultSortOrder.Name, "Name")]
    public void GetLabel_ResultSortOrder_ReturnsGermanName(ResultSortOrder value, string expected)
    {
        Assert.Equal(expected, SettingsTexts.GetLabel(value));
    }
}

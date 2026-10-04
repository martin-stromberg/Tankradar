using Tankradar.MAUI.Models;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Standardwerte von <see cref="AppSettings.CreateDefault"/> („sicher statt bequem“).
/// </summary>
public class AppSettingsTests_Defaults : BaseTest
{
    /// <summary>
    /// Prüft Standortnutzung, Ansicht und Sortierung der Standardeinstellungen.
    /// </summary>
    [Fact]
    public void CreateDefault_HasExpectedScalarValues()
    {
        var settings = AppSettings.CreateDefault();

        Assert.Equal(GpsUsage.WhileInUse, settings.GpsUsage);
        Assert.Equal(ResultView.List, settings.ResultView);
        Assert.Equal(ResultSortOrder.Price, settings.ResultSortOrder);
    }

    /// <summary>
    /// Prüft, dass alle Spritsorten in der Reihenfolge E5, E10, Diesel ausgewählt sind.
    /// </summary>
    [Fact]
    public void CreateDefault_SelectsAllFuelTypesInOrder()
    {
        var settings = AppSettings.CreateDefault();

        Assert.Equal([FuelType.SuperE5, FuelType.SuperE10, FuelType.Diesel], settings.FuelTypes.Select(f => f.FuelType));
        Assert.All(settings.FuelTypes, selection => Assert.True(selection.IsSelected));
    }

    /// <summary>
    /// Prüft, dass die Standardeinstellungen gültig sind.
    /// </summary>
    [Fact]
    public void CreateDefault_IsValid()
    {
        var exception = Record.Exception(() => AppSettings.CreateDefault().Validate());

        Assert.Null(exception);
    }
}

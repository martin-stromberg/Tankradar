using Tankradar.MAUI.Models;
using Tankradar.MAUI.Services.Search;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Reihenfolge, Auswahl und Format der Preiszeilen in der Ergebnisaufbereitung.
/// </summary>
public class StationResultBuilderTests_PriceLines : BaseTest
{
    private static readonly DateTime Now = SearchTestData.Now;

    /// <summary>
    /// Prüft, dass die Preiszeilen der Reihenfolge der Einstellungen folgen und nicht der Reihenfolge der Quelle.
    /// </summary>
    [Fact]
    public void Build_FollowsSettingsOrder()
    {
        var station = StationFactory.CreateCustom(1, "Eins", 1.0, Now, null, null, (FuelType.SuperE5, 1.80m), (FuelType.SuperE10, 1.70m), (FuelType.Diesel, 1.60m));

        var item = Assert.Single(StationResultBuilder.Build([station], SearchTestData.Only(FuelType.Diesel, FuelType.SuperE5, FuelType.SuperE10), null, ResultSortOrder.Price, Now));

        Assert.Equal([FuelType.Diesel, FuelType.SuperE5, FuelType.SuperE10], item.PriceLines.Select(line => line.FuelType));
        Assert.Equal(["Diesel", "Super E5", "Super E10"], item.PriceLines.Select(line => line.FuelLabel));
    }

    /// <summary>
    /// Prüft, dass abgewählte Sorten nicht erscheinen.
    /// </summary>
    [Fact]
    public void Build_DeselectedFuelTypes_AreOmitted()
    {
        var station = StationFactory.CreateCustom(1, "Eins", 1.0, Now, null, null, (FuelType.SuperE5, 1.80m), (FuelType.SuperE10, 1.70m), (FuelType.Diesel, 1.60m));

        var item = Assert.Single(StationResultBuilder.Build([station], SearchTestData.Only(FuelType.SuperE10), null, ResultSortOrder.Price, Now));

        Assert.Equal(FuelType.SuperE10, Assert.Single(item.PriceLines).FuelType);
    }

    /// <summary>
    /// Prüft das Preisformat in deutscher Schreibweise mit drei Nachkommastellen und Eurozeichen.
    /// </summary>
    [Fact]
    public void Build_FormatsPriceInGermanCulture()
    {
        var station = StationFactory.CreateCustom(1, "Eins", 1.4, Now, null, null, (FuelType.SuperE5, 1.859m));

        var item = Assert.Single(StationResultBuilder.Build([station], SearchTestData.AllFuels, null, ResultSortOrder.Price, Now));

        Assert.Equal("1,859 €", item.PriceLines[0].PriceText);
        Assert.Equal(1.859m, item.PriceLines[0].Price);
        Assert.Equal("1,4 km", item.DistanceText);
    }
}

using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Search;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den angezeigten Namen in der Ergebnisliste: Marke vorn mit Stammdatennamen darunter, ohne Marke der Name, eigener Name mit Vorrang.
/// </summary>
public class StationResultBuilderTests_DisplayName : BaseTest
{
    private static readonly DateTime Now = SearchTestData.Now;

    private static StationInfo Station(int number, string name, string? brand = null, string? customName = null)
    {
        return new StationInfo
        {
            Id = $"{number:D8}-0000-4000-8000-000000000000",
            Name = name,
            Brand = brand,
            CustomName = customName,
            DistanceKm = number,
            Prices = [new FuelPrice(FuelType.Diesel, 1.699m, Now)],
        };
    }

    /// <summary>
    /// Prüft die Marke als Name und den Stammdatennamen als zweite Zeile.
    /// </summary>
    [Fact]
    public void Build_WithBrand_ShowsBrandAndSubName()
    {
        var item = StationResultBuilder.Build([Station(1, "Klaus Heinrich", "Westfalen")], SearchTestData.AllFuels, null, ResultSortOrder.Price, Now).Single();

        Assert.Equal("Westfalen", item.Name);
        Assert.Equal("Klaus Heinrich", item.SubName);
        Assert.True(item.HasSubName);
    }

    /// <summary>
    /// Prüft, dass ohne Marke der Name bleibt und keine zweite Zeile erscheint.
    /// </summary>
    [Fact]
    public void Build_WithoutBrand_ShowsNameOnly()
    {
        var item = StationResultBuilder.Build([Station(1, "Alpha Tankstelle")], SearchTestData.AllFuels, null, ResultSortOrder.Price, Now).Single();

        Assert.Equal("Alpha Tankstelle", item.Name);
        Assert.False(item.HasSubName);
    }

    /// <summary>
    /// Prüft, dass der eigene Name Vorrang hat.
    /// </summary>
    [Fact]
    public void Build_WithCustomName_ShowsCustomNameFirst()
    {
        var item = StationResultBuilder.BuildItem(Station(1, "Klaus Heinrich", "Westfalen", "Zuhause tanken"), SearchTestData.AllFuels, Now);

        Assert.Equal("Zuhause tanken", item.Name);
        Assert.Equal("Klaus Heinrich", item.SubName);
    }

    /// <summary>
    /// Prüft, dass die Sortierung nach Name den angezeigten Namen verwendet.
    /// </summary>
    [Fact]
    public void Build_SortByName_UsesDisplayedName()
    {
        var stations = new[] { Station(1, "Anton", "Zeta"), Station(2, "Zacharias", "Alpha"), Station(3, "Mitte") };

        var items = StationResultBuilder.Build(stations, SearchTestData.AllFuels, null, ResultSortOrder.Name, Now);

        Assert.Equal(["Alpha", "Mitte", "Zeta"], items.Select(item => item.Name));
    }
}

using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Favorites;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Favorites;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den angezeigten Namen in der Gruppenübersicht der Startseite: Marke vorn mit Stammdatennamen darunter, eigener Name mit Vorrang.
/// </summary>
public class HomeOverviewBuilderTests_DisplayName : BaseTest
{
    private static readonly DateTime Now = SearchTestData.Now;

    private static StationInfo Station(string? brand, string? customName)
    {
        return new StationInfo
        {
            Id = "00000001-0000-4000-8000-000000000000",
            Name = "Klaus Heinrich",
            Brand = brand,
            CustomName = customName,
            Prices = [new FuelPrice(FuelType.Diesel, 1.699m, Now)],
        };
    }

    private static FavoriteGroupOverview Group(StationInfo station, string name, string subName)
    {
        var entries = new List<FavoriteEntry> { new(1, station.Id, name, string.Empty, null, FavoritePriority.None, subName) };
        return new FavoriteGroupOverview(new FavoriteGroup(1, "Heimat", null, 1), entries);
    }

    /// <summary>
    /// Prüft, dass eine bekannte Tankstelle mit Marke die Marke als Namen und den Stammdatennamen darunter zeigt.
    /// </summary>
    [Fact]
    public void Build_KnownStationWithBrand_ShowsBrandAndSubName()
    {
        var station = Station("Westfalen", null);

        var groups = HomeOverviewBuilder.Build([Group(station, "Westfalen", "Klaus Heinrich")], new Dictionary<string, StationInfo> { [station.Id] = station }, SearchTestData.AllFuels, null, Now);

        var item = groups[0].Stations[0].Item;
        Assert.Equal("Westfalen", item.Name);
        Assert.Equal("Klaus Heinrich", item.SubName);
    }

    /// <summary>
    /// Prüft, dass der eigene Name Vorrang hat.
    /// </summary>
    [Fact]
    public void Build_KnownStationWithCustomName_ShowsCustomName()
    {
        var station = Station("Westfalen", "Zuhause");

        var groups = HomeOverviewBuilder.Build([Group(station, "Zuhause", "Klaus Heinrich")], new Dictionary<string, StationInfo> { [station.Id] = station }, SearchTestData.AllFuels, null, Now);

        Assert.Equal("Zuhause", groups[0].Stations[0].Item.Name);
    }

    /// <summary>
    /// Prüft, dass eine lokal unbekannte Tankstelle Name und Stammdatennamen aus dem Favoriteneintrag übernimmt.
    /// </summary>
    [Fact]
    public void Build_UnknownStation_UsesNamesOfFavoriteEntry()
    {
        var station = Station("Westfalen", null);

        var groups = HomeOverviewBuilder.Build([Group(station, "Westfalen", "Klaus Heinrich")], new Dictionary<string, StationInfo>(), SearchTestData.AllFuels, null, Now);

        var item = groups[0].Stations[0].Item;
        Assert.Equal("Westfalen", item.Name);
        Assert.Equal("Klaus Heinrich", item.SubName);
        Assert.True(item.HasSubName);
    }
}

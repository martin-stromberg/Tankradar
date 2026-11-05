using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Favorites;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Aufbereitung der Gruppenübersicht der Startseite.
/// </summary>
public class HomeOverviewBuilderTests_Content : BaseTest
{
    private static readonly DateTime Now = SearchTestData.Now;
    private static readonly GeoPosition Center = new(StationFactory.CenterLatitude, StationFactory.CenterLongitude);

    private static IReadOnlyList<MAUI.Models.Favorites.HomeGroup> Build(
        IReadOnlyList<MAUI.Models.Favorites.FavoriteGroupOverview> overview,
        Dictionary<string, StationInfo> stations,
        IReadOnlyList<FuelTypeSelection>? fuels = null,
        GeoPosition? position = null)
    {
        return HomeOverviewBuilder.Build(overview, stations, fuels ?? SearchTestData.AllFuels, position, Now);
    }

    /// <summary>
    /// Prüft, dass die Preiszeilen in der Reihenfolge der Einstellungen mit Alter erscheinen.
    /// </summary>
    [Fact]
    public void Build_ShowsPricesInSettingsOrderWithAge()
    {
        var station = HomeTestData.Station(1, Now.AddMinutes(-5));

        var groups = Build([HomeTestData.Group(1, "Heimat", station)], HomeTestData.ById(station), SearchTestData.Only(FuelType.Diesel, FuelType.SuperE5));

        var lines = groups[0].Stations[0].Item.PriceLines;
        Assert.Equal([FuelType.Diesel, FuelType.SuperE5], lines.Select(l => l.FuelType));
        Assert.All(lines, l => Assert.Equal("vor 5 Min.", l.AgeText));
        Assert.True(groups[0].Stations[0].HasPrices);
    }

    /// <summary>
    /// Prüft, dass Preise ab 60 Minuten als veraltet gekennzeichnet werden und davor nicht.
    /// </summary>
    [Fact]
    public void Build_MarksPricesFromSixtyMinutesAsStale()
    {
        var fresh = HomeTestData.Station(1, Now.AddMinutes(-59));
        var stale = HomeTestData.Station(2, Now.AddMinutes(-60));

        var groups = Build([HomeTestData.Group(1, "G", fresh, stale)], HomeTestData.ById(fresh, stale));

        Assert.All(groups[0].Stations[0].Item.PriceLines, l => Assert.False(l.IsStale));
        Assert.All(groups[0].Stations[1].Item.PriceLines, l => Assert.True(l.IsStale));
        Assert.True(groups[0].Stations[1].Item.HasUnconfirmedPrice);
    }

    /// <summary>
    /// Prüft, dass ohne Standort keine Entfernung erscheint.
    /// </summary>
    [Fact]
    public void Build_HidesDistanceWithoutPosition()
    {
        var station = HomeTestData.Station(1, Now);

        var item = Build([HomeTestData.Group(1, "G", station)], HomeTestData.ById(station))[0].Stations[0].Item;

        Assert.Null(item.DistanceKm);
        Assert.False(item.HasDistance);
    }

    /// <summary>
    /// Prüft, dass mit Standort die Entfernung aus der gespeicherten Position berechnet wird, bei Position (0, 0) der Tankstelle aber nicht.
    /// </summary>
    [Fact]
    public void Build_ShowsDistanceWithPosition()
    {
        var station = HomeTestData.Station(1, Now);
        var origin = new StationInfo { Id = "00000009-0000-4000-8000-000000000000", Name = "Ohne Position", Prices = station.Prices };

        var groups = Build([HomeTestData.Group(1, "G", station, origin)], HomeTestData.ById(station, origin), position: Center);

        Assert.Equal(1.1, groups[0].Stations[0].Item.DistanceKm!.Value, 1);
        Assert.Equal("1,1 km", groups[0].Stations[0].Item.DistanceText);
        Assert.False(groups[0].Stations[1].Item.HasDistance);
    }

    /// <summary>
    /// Prüft die Hinweise „Preis unbestätigt“ und „Automatentankstelle“.
    /// </summary>
    [Fact]
    public void Build_HintsUnconfirmedAndAutomated()
    {
        var station = StationFactory.CreateCustom(1, "Automat", null, Now.AddMinutes(-90), true, null, (FuelType.SuperE5, 1.8m));

        var item = Build([HomeTestData.Group(1, "G", station)], HomeTestData.ById(station))[0].Stations[0].Item;

        Assert.True(item.HasUnconfirmedPrice);
        Assert.True(item.IsAutomatedStation);
    }

    /// <summary>
    /// Prüft, dass eine Tankstelle ohne Preise der gewählten Sorten als „ohne Preise“ gilt.
    /// </summary>
    [Fact]
    public void Build_StationWithoutSelectedPrices_HasNoPrices()
    {
        var station = StationFactory.Create(1, Now, (FuelType.SuperE10, 1.8m));

        var groups = Build([HomeTestData.Group(1, "G", station)], HomeTestData.ById(station), SearchTestData.Only(FuelType.Diesel));

        Assert.False(groups[0].Stations[0].HasPrices);
    }

    /// <summary>
    /// Prüft, dass die Reihenfolge der Einträge, Gruppen, Notiz und Priorität erhalten bleiben.
    /// </summary>
    [Fact]
    public void Build_KeepsEntryOrderAndMultipleGroups()
    {
        var a = HomeTestData.Station(1, Now);
        var b = HomeTestData.Station(2, Now);

        var groups = Build([HomeTestData.Group(1, "Eins", b, a), HomeTestData.Group(2, "Zwei", a)], HomeTestData.ById(a, b));

        Assert.Equal(["Eins", "Zwei"], groups.Select(g => g.Name));
        Assert.Equal([b.Id, a.Id], groups[0].Stations.Select(s => s.Item.Id));
        Assert.Equal("2 Tankstellen", groups[0].CountText);
        Assert.Equal("1 Tankstelle", groups[1].CountText);
    }

    /// <summary>
    /// Prüft, dass eine leere Gruppe ohne Tankstellen erscheint.
    /// </summary>
    [Fact]
    public void Build_EmptyGroup()
    {
        var groups = Build([HomeTestData.Group(1, "Leer")], []);

        Assert.True(Assert.Single(groups).IsEmpty);
        Assert.Equal("0 Tankstellen", groups[0].CountText);
    }

    /// <summary>
    /// Prüft, dass abgewählte Sorten nicht angezeigt werden.
    /// </summary>
    [Fact]
    public void Build_UnselectedFuelTypeNotShown()
    {
        var station = HomeTestData.Station(1, Now);

        var groups = Build([HomeTestData.Group(1, "G", station)], HomeTestData.ById(station), SearchTestData.Only(FuelType.SuperE5));

        Assert.Equal([FuelType.SuperE5], groups[0].Stations[0].Item.PriceLines.Select(l => l.FuelType));
    }

    /// <summary>
    /// Prüft, dass eine lokal unbekannte Tankstelle mit Namen, aber ohne Preise erscheint.
    /// </summary>
    [Fact]
    public void Build_UnknownStation_ShowsNameWithoutPrices()
    {
        var station = HomeTestData.Station(1, Now);

        var groups = Build([HomeTestData.Group(1, "G", station)], []);

        Assert.Equal(station.Name, groups[0].Stations[0].Item.Name);
        Assert.False(groups[0].Stations[0].HasPrices);
    }
}

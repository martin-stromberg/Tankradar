using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Search;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Aufbereitung der Detailansicht: Name, Adresse, Entfernung, Preise der aktivierten Sorten mit Alter und Hinweise.
/// </summary>
public class StationDetailBuilderTests_Content : BaseTest
{
    private static readonly DateTime Now = SearchTestData.Now;

    private static StationInfo Station(DateTime retrieved, params (FuelType, decimal)[] prices)
    {
        return new StationInfo
        {
            Id = "00000001-0000-4000-8000-000000000000",
            Name = "Alpha Tankstelle",
            Brand = "ALPHA",
            Street = "Hauptstraße",
            HouseNumber = "1",
            PostCode = "10115",
            Place = "Berlin",
            IsOpen = true,
            Prices = prices.Select(p => new FuelPrice(p.Item1, p.Item2, retrieved)).ToList(),
        };
    }

    /// <summary>
    /// Prüft Name, Marke, Adresse, Entfernung und Öffnungsstatus.
    /// </summary>
    [Fact]
    public void Build_ShowsNameBrandAddressDistanceAndStatus()
    {
        var item = StationDetailBuilder.Build(Station(Now, (FuelType.Diesel, 1.699m)), SearchTestData.AllFuels, 1.84, Now);

        Assert.Equal("Alpha Tankstelle", item.Name);
        Assert.Equal("ALPHA", item.BrandText);
        Assert.Equal("Hauptstraße 1, 10115 Berlin", item.AddressText);
        Assert.Equal("1,8 km", item.DistanceText);
        Assert.Equal("Geöffnet", item.OpeningStatusText);
        Assert.True(item.HasBrand && item.HasAddress && item.HasDistance && item.HasOpeningStatus);
    }

    /// <summary>
    /// Prüft, dass nur die aktivierten Sorten in der Reihenfolge der Einstellungen mit Preis und Alter erscheinen.
    /// </summary>
    [Fact]
    public void Build_ShowsOnlyEnabledFuelsInSettingsOrder()
    {
        var fuels = new List<FuelTypeSelection> { new(FuelType.Diesel, true), new(FuelType.SuperE5, true), new(FuelType.SuperE10, false) };
        var station = Station(Now.AddMinutes(-4), (FuelType.SuperE5, 1.859m), (FuelType.SuperE10, 1.799m), (FuelType.Diesel, 1.699m));

        var item = StationDetailBuilder.Build(station, fuels, null, Now);

        Assert.Equal([FuelType.Diesel, FuelType.SuperE5], item.PriceLines.Select(line => line.FuelType));
        Assert.Equal(["1,699 €", "1,859 €"], item.PriceLines.Select(line => line.PriceText));
        Assert.All(item.PriceLines, line => Assert.Equal("vor 4 Min.", line.AgeText));
        Assert.All(item.PriceLines, line => Assert.False(line.IsStale));
    }

    /// <summary>
    /// Prüft die Amber-Markierung und den Hinweis „Preis unbestätigt“ ab 60 Minuten.
    /// </summary>
    [Fact]
    public void Build_StalePrice_IsMarkedAndUnconfirmed()
    {
        var item = StationDetailBuilder.Build(Station(Now.AddMinutes(-60), (FuelType.Diesel, 1.699m)), SearchTestData.AllFuels, null, Now);

        Assert.True(Assert.Single(item.PriceLines).IsStale);
        Assert.Equal("vor 60 Min.", item.PriceLines[0].AgeText);
        Assert.True(item.HasUnconfirmedPrice);
    }

    /// <summary>
    /// Prüft, dass frische Preise keinen Hinweis auslösen.
    /// </summary>
    [Fact]
    public void Build_FreshPrice_HasNoUnconfirmedHint()
    {
        var item = StationDetailBuilder.Build(Station(Now.AddMinutes(-59), (FuelType.Diesel, 1.699m)), SearchTestData.AllFuels, null, Now);

        Assert.False(item.HasUnconfirmedPrice);
        Assert.False(item.HasUnconfirmedPrice);
    }

    /// <summary>
    /// Prüft den Hinweis „Automatentankstelle“ bei durchgehender Öffnung aus aktuellen Detailangaben.
    /// </summary>
    [Fact]
    public void Build_WholeDayWithRecentDetails_ShowsAutomatedHint()
    {
        var station = new StationInfo
        {
            Id = "00000001-0000-4000-8000-000000000000",
            Name = "Alpha",
            WholeDay = true,
            DetailsUpdatedUtc = Now.AddMinutes(-5),
        };

        var item = StationDetailBuilder.Build(station, SearchTestData.AllFuels, null, Now);

        Assert.True(item.IsAutomatedStation);
        Assert.True(item.HasInfoBox && item.HasOpeningStatusOrAutomat);
        Assert.False(item.HasUnconfirmedPrice);
    }
}

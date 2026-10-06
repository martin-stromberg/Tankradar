using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Search;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die vom Designentwurf geforderten Darstellungsdaten der Detailansicht: hochgestellte dritte Nachkommastelle mit Einheit, Chip zur Preisaktualität und Chip „Automat 24/7“.
/// </summary>
public class StationDetailBuilderTests_Design : BaseTest
{
    private static readonly DateTime Now = SearchTestData.Now;

    private static StationInfo Station(params (FuelType Fuel, decimal Price, int AgeMinutes)[] prices)
    {
        return new StationInfo
        {
            Id = "00000001-0000-4000-8000-000000000000",
            Name = "Alpha Tankstelle",
            Prices = prices.Select(p => new FuelPrice(p.Fuel, p.Price, Now.AddMinutes(-p.AgeMinutes))).ToList(),
        };
    }

    /// <summary>
    /// Prüft die Aufteilung des Preises in Hauptzahl, hochgestellte Ziffer und Einheit sowie den gesprochenen Text.
    /// </summary>
    [Fact]
    public void Build_Price_IsSplitIntoMainFractionAndUnit()
    {
        var item = StationDetailBuilder.Build(Station((FuelType.SuperE5, 1.859m, 1)), SearchTestData.AllFuels, null, Now);

        var line = item.PriceLines.Single();
        Assert.Equal("1,85", line.PriceMainText);
        Assert.Equal("9", line.PriceFractionText);
        Assert.Equal("€/L", line.PriceUnitText);
        Assert.Equal("1,859 Euro pro Liter", line.PriceSpokenText);
        Assert.Equal("1,859 €", line.PriceText);
    }

    /// <summary>
    /// Prüft, dass bei frischen Preisen der Chip „Live-Preise“ erscheint.
    /// </summary>
    [Fact]
    public void Build_FreshPrices_ShowsLivePricesChip()
    {
        var item = StationDetailBuilder.Build(Station((FuelType.SuperE5, 1.859m, 4), (FuelType.Diesel, 1.699m, 59)), SearchTestData.AllFuels, null, Now);

        Assert.True(item.HasPrices);
        Assert.True(item.HasLivePrices);
        Assert.False(item.HasStalePrice);
        Assert.Equal(DetailTexts.LivePrices, item.PriceStatusText);
    }

    /// <summary>
    /// Prüft, dass bei veralteten Preisen stattdessen die Altersangabe des ältesten Preises erscheint.
    /// </summary>
    [Fact]
    public void Build_StalePrice_ShowsAgeOfOldestPriceInsteadOfLive()
    {
        var item = StationDetailBuilder.Build(Station((FuelType.SuperE5, 1.859m, 70), (FuelType.Diesel, 1.699m, 130), (FuelType.SuperE10, 1.799m, 5)), SearchTestData.AllFuels, null, Now);

        Assert.False(item.HasLivePrices);
        Assert.True(item.HasStalePrice);
        Assert.Equal("Preise: vor 130 Min.", item.PriceStatusText);
    }

    /// <summary>
    /// Prüft, dass ohne Preise keine Preisaktualität angezeigt wird.
    /// </summary>
    [Fact]
    public void Build_NoPrices_ShowsNoPriceStatus()
    {
        var item = StationDetailBuilder.Build(Station(), SearchTestData.AllFuels, null, Now);

        Assert.False(item.HasPrices);
        Assert.False(item.HasLivePrices);
        Assert.Equal(string.Empty, item.PriceStatusText);
    }

    /// <summary>
    /// Prüft, dass der Chip „Automat 24/7“ nur bei vorliegenden Daten erscheint und die Info-Box sonst entfällt.
    /// </summary>
    [Fact]
    public void Build_WithoutAutomatDataDistanceOrStatus_HasNoInfoBox()
    {
        var item = StationDetailBuilder.Build(Station((FuelType.SuperE5, 1.859m, 1)), SearchTestData.AllFuels, null, Now);

        Assert.False(item.IsAutomatedStation);
        Assert.False(item.HasInfoBox);
        Assert.False(item.HasOpeningStatusOrAutomat);
    }
}

using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Search;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den angezeigten Namen der Detailansicht: Marke vorn mit Stammdatennamen darunter, ohne Marke der Name, eigener Name mit Vorrang.
/// </summary>
public class StationDetailBuilderTests_DisplayName : BaseTest
{
    private static readonly DateTime Now = SearchTestData.Now;

    private static StationInfo Station(string? brand, string? customName = null)
    {
        return new StationInfo { Id = "00000001-0000-4000-8000-000000000000", Name = "Klaus Heinrich", Brand = brand, CustomName = customName };
    }

    /// <summary>
    /// Prüft die Marke als Name und den Stammdatennamen darunter.
    /// </summary>
    [Fact]
    public void Build_WithBrand_ShowsBrandAndSubName()
    {
        var item = StationDetailBuilder.Build(Station("Westfalen"), SearchTestData.AllFuels, null, Now);

        Assert.Equal("Westfalen", item.Name);
        Assert.Equal("Klaus Heinrich", item.SubNameText);
        Assert.False(item.HasCustomName);
    }

    /// <summary>
    /// Prüft, dass ohne Marke der Name bleibt und nichts darunter steht.
    /// </summary>
    [Fact]
    public void Build_WithoutBrand_ShowsNameOnly()
    {
        var item = StationDetailBuilder.Build(Station(null), SearchTestData.AllFuels, null, Now);

        Assert.Equal("Klaus Heinrich", item.Name);
        Assert.False(item.HasSubName);
    }

    /// <summary>
    /// Prüft, dass der eigene Name Vorrang hat und für das Bearbeitungsfeld bereitsteht.
    /// </summary>
    [Fact]
    public void Build_WithCustomName_ShowsCustomNameAndExposesIt()
    {
        var item = StationDetailBuilder.Build(Station("Westfalen", " Mein Tanken "), SearchTestData.AllFuels, null, Now);

        Assert.Equal("Mein Tanken", item.Name);
        Assert.Equal("Klaus Heinrich", item.SubNameText);
        Assert.Equal("Mein Tanken", item.CustomName);
        Assert.True(item.HasCustomName);
    }
}

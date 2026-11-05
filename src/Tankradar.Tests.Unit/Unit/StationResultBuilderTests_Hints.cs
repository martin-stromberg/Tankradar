using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Search;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Hinweise „Preis unbestätigt“ und „Automatentankstelle“ der Ergebnisaufbereitung.
/// </summary>
public class StationResultBuilderTests_Hints : BaseTest
{
    private static readonly DateTime Now = SearchTestData.Now;

    private static StationListItem Build(StationInfo station)
    {
        return StationResultBuilder.Build([station], SearchTestData.AllFuels, null, ResultSortOrder.Price, Now).Single();
    }

    /// <summary>
    /// Prüft, dass „Preis unbestätigt“ nur bei mindestens einem veralteten Preis erscheint.
    /// </summary>
    [Fact]
    public void Build_UnconfirmedHint_OnlyForStalePrices()
    {
        var fresh = Build(StationFactory.CreateCustom(1, "Frisch", 1.0, Now.AddMinutes(-59), null, null, (FuelType.SuperE5, 1.8m)));
        var stale = Build(StationFactory.CreateCustom(2, "Alt", 1.0, Now.AddMinutes(-60), null, null, (FuelType.SuperE5, 1.8m)));

        Assert.False(fresh.HasUnconfirmedPrice);
        Assert.True(stale.HasUnconfirmedPrice);
    }

    /// <summary>
    /// Prüft, dass „Automatentankstelle“ bei durchgehender Öffnung erscheint und sonst nicht (auch nicht bei unbekannten Zeiten).
    /// </summary>
    [Fact]
    public void Build_AutomatedHint_OnlyForAroundTheClockStations()
    {
        var automated = Build(StationFactory.CreateCustom(1, "Automat", 1.0, Now, true, null, (FuelType.SuperE5, 1.8m)));
        var regular = Build(StationFactory.CreateCustom(2, "Normal", 1.0, Now, false, null, (FuelType.SuperE5, 1.8m)));
        var unknown = Build(StationFactory.CreateCustom(3, "Unbekannt", 1.0, Now, null, null, (FuelType.SuperE5, 1.8m)));

        Assert.True(automated.IsAutomatedStation);
        Assert.False(regular.IsAutomatedStation);
        Assert.False(unknown.IsAutomatedStation);
    }

    /// <summary>
    /// Prüft, dass ein veralteter Preis einer abgewählten Sorte den Hinweis nicht auslöst.
    /// </summary>
    [Fact]
    public void Build_StalePriceOfDeselectedFuel_DoesNotTriggerHint()
    {
        var station = StationFactory.CreateCustom(1, "Eins", 1.0, Now.AddMinutes(-120), null, null, (FuelType.Diesel, 1.5m));
        var item = StationResultBuilder.Build([station], SearchTestData.Only(FuelType.SuperE5), null, ResultSortOrder.Price, Now).Single();

        Assert.False(item.HasUnconfirmedPrice);
        Assert.False(item.HasHints);
    }
}

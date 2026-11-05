using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Search;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass der Hinweis „Automatentankstelle“ in Suchergebnissen nur aus Detailangaben entsteht, die nicht älter als die Altersgrenze sind.
/// </summary>
public class StationResultBuilderTests_DetailAge : BaseTest
{
    private static readonly DateTime Now = SearchTestData.Now;

    private static StationListItem Build(DateTime detailsUpdated)
    {
        var station = new StationInfo
        {
            Id = "00000001-0000-4000-8000-000000000000",
            Name = "Alpha",
            WholeDay = true,
            OpeningTimes = [new OpeningTimeEntry("Mo-So", "00:00:00", "24:00:00")],
            DetailsUpdatedUtc = detailsUpdated,
            Prices = [new FuelPrice(FuelType.Diesel, 1.6m, Now)],
        };
        return StationResultBuilder.Build([station], SearchTestData.AllFuels, null, ResultSortOrder.Price, Now).Single();
    }

    /// <summary>
    /// Prüft, dass ein Hinweis aus Detailangaben der letzten 24 Stunden erscheint.
    /// </summary>
    [Fact]
    public void Build_RecentDetails_ShowAutomatedHint()
    {
        Assert.True(Build(Now.AddHours(-23)).IsAutomatedStation);
    }

    /// <summary>
    /// Prüft, dass der Hinweis aus zu alten Detailangaben ausgeblendet wird, die Preise aber angezeigt bleiben.
    /// </summary>
    [Fact]
    public void Build_OldDetails_HideAutomatedHintButKeepPrices()
    {
        var item = Build(Now.AddHours(-25));

        Assert.False(item.IsAutomatedStation);
        Assert.Single(item.PriceLines);
    }
}

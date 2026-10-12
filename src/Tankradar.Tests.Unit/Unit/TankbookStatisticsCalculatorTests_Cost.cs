using Tankradar.MAUI.Models.Tankbook;
using static Tankradar.Tests.Unit.Unit.Support.TankbookEntryFactory;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Kosten pro 100 km des <see cref="MAUI.Services.Tankbook.TankbookStatisticsCalculator"/>.
/// </summary>
public class TankbookStatisticsCalculatorTests_Cost : BaseTest
{
    /// <summary>
    /// Prüft, dass die Kosten Σ Kosten / Σ Strecke × 100 betragen.
    /// </summary>
    [Fact]
    public void Cost_FullEntries_IsSumCostOverSumDistance()
    {
        var kind = Statistics([Create(1, 10_000, 40m), Create(2, 10_500, 30m), Create(3, 11_000, 20m)]).Kinds.Single();

        Assert.Equal(7.50m, kind.CostPer100Km);
        Assert.Equal(135m, kind.TotalCost);
    }

    /// <summary>
    /// Prüft, dass Kosten und Menge dazwischenliegender Teilbetankungen in die Intervalle einfließen.
    /// </summary>
    [Fact]
    public void Cost_PartialsIncludeCost()
    {
        var kind = Statistics([Create(1, 10_000, 40m), Create(2, 10_200, 10m, isPartial: true), Create(3, 10_500, 20m)]).Kinds.Single();

        Assert.Equal(9.00m, kind.CostPer100Km);
        Assert.Equal(6.00m, kind.AverageConsumption);
    }

    /// <summary>
    /// Prüft, dass eine Teilbetankung vor dem ersten vollen Vorgang nicht in die Kosten pro 100 km, wohl aber in die Gesamtkosten einfließt.
    /// </summary>
    [Fact]
    public void Cost_PartialBeforeFirstFull_NotInCostPer100Km()
    {
        var kind = Statistics([Create(1, 9_800, 99m, isPartial: true), Create(2, 10_000, 40m), Create(3, 10_500, 30m)]).Kinds.Single();

        Assert.Equal(9.00m, kind.CostPer100Km);
        Assert.Equal(253.5m, kind.TotalCost);
    }

    /// <summary>
    /// Prüft, dass bei Strecke 0 oder kleiner kein Wert entsteht.
    /// </summary>
    [Fact]
    public void Cost_DistanceZeroOrNegative_NoValue()
    {
        var zero = Statistics([Create(1, 10_000, 40m), Create(2, 10_000, 30m)]).Kinds.Single();
        var negative = Statistics([Create(1, 10_500, 40m), Create(2, 10_000, 30m)]).Kinds.Single();

        Assert.Null(zero.CostPer100Km);
        Assert.Null(negative.CostPer100Km);
        Assert.Equal(0, zero.TotalDistanceKm);
    }

    /// <summary>
    /// Prüft, dass der Startverbrauch keine Kosten pro 100 km liefert.
    /// </summary>
    [Fact]
    public void Cost_StartConsumption_HasNoCostPer100Km()
    {
        var kind = Statistics([Create(1, 10_000, 40m)], startConsumption: 5.4m).Kinds.Single();

        Assert.Null(kind.CostPer100Km);
    }
}

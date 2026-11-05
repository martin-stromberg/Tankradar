using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;
using static Tankradar.Tests.Unit.Unit.Support.TankbookEntryFactory;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Intervallwerte (Menge und Kosten) des <see cref="ConsumptionCalculator"/>, die die Auswertung aggregiert; die Verbrauchswerte bleiben unverändert.
/// </summary>
public class ConsumptionCalculatorTests_Interval : BaseTest
{
    /// <summary>
    /// Prüft, dass ein voller Vorgang nach einem vollen Vorgang Menge und Kosten seines Intervalls erhält.
    /// </summary>
    [Fact]
    public void Calculate_TwoFull_SetsIntervalQuantityAndCost()
    {
        var result = ConsumptionCalculator.Calculate([Create(1, 10_000, 40m), Create(2, 10_500, 30m)]);

        Assert.Null(result[0].IntervalQuantity);
        Assert.Null(result[0].IntervalCost);
        Assert.Equal(30m, result[1].IntervalQuantity);
        Assert.Equal(45m, result[1].IntervalCost);
        Assert.Equal(6.00m, result[1].Consumption);
    }

    /// <summary>
    /// Prüft, dass Teilbetankungen zwischen zwei vollen Vorgängen Menge und Kosten des Intervalls erhöhen.
    /// </summary>
    [Fact]
    public void Calculate_PartialBetween_AddsQuantityAndCostToInterval()
    {
        var result = ConsumptionCalculator.Calculate([Create(1, 10_000, 40m), Create(2, 10_200, 10m, isPartial: true), Create(3, 10_500, 20m)]);

        Assert.Null(result[1].IntervalQuantity);
        Assert.Equal(30m, result[2].IntervalQuantity);
        Assert.Equal(45m, result[2].IntervalCost);
        Assert.Equal(6.00m, result[2].Consumption);
    }

    /// <summary>
    /// Prüft, dass eine Teilbetankung vor dem ersten vollen Vorgang nicht in die Intervallwerte einfließt.
    /// </summary>
    [Fact]
    public void Calculate_PartialBeforeFirstFull_NoIntervalValues()
    {
        var result = ConsumptionCalculator.Calculate([Create(1, 9_800, 99m, isPartial: true), Create(2, 10_000, 40m), Create(3, 10_500, 30m)]);

        Assert.Null(result[0].IntervalQuantity);
        Assert.Null(result[1].IntervalCost);
        Assert.Equal(30m, result[2].IntervalQuantity);
        Assert.Equal(45m, result[2].IntervalCost);
    }

    /// <summary>
    /// Prüft, dass ein Intervall ohne Strecke keine Intervallwerte hat und die Summen für das nächste Intervall zurückgesetzt werden.
    /// </summary>
    [Fact]
    public void Calculate_NoDistance_HasNoIntervalValues()
    {
        var result = ConsumptionCalculator.Calculate(
            [Create(1, 10_000, 40m), Create(2, 10_100, 5m, isPartial: true), Create(3, 10_000, 30m), Create(4, 10_500, 20m)]);

        Assert.Equal(TankbookConsumptionState.NoDistance, result[2].ConsumptionState);
        Assert.Null(result[2].IntervalQuantity);
        Assert.Null(result[2].IntervalCost);
        Assert.Equal(20m, result[3].IntervalQuantity);
        Assert.Equal(30m, result[3].IntervalCost);
    }

    /// <summary>
    /// Prüft, dass Kraftstoff und Strom getrennte Intervalle führen.
    /// </summary>
    [Fact]
    public void Calculate_Hybrid_IntervalsPerKind()
    {
        var result = ConsumptionCalculator.Calculate(
        [
            Create(1, 10_000, 40m),
            Create(2, 10_100, 15m, kind: TankbookEntryKind.Electricity),
            Create(3, 10_500, 30m),
            Create(4, 10_600, 20m, kind: TankbookEntryKind.Electricity),
        ]);

        Assert.Equal(30m, result[2].IntervalQuantity);
        Assert.Equal(45m, result[2].IntervalCost);
        Assert.Equal(20m, result[3].IntervalQuantity);
        Assert.Equal(30m, result[3].IntervalCost);
        Assert.Equal(4.00m, result[3].Consumption);
    }
}

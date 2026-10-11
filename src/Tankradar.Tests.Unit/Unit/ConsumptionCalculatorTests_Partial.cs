using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;
using static Tankradar.Tests.Unit.Unit.Support.TankbookEntryFactory;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft <see cref="ConsumptionCalculator"/> für Teilbetankungen: kein eigener Wert, Menge zählt beim nächsten vollen Vorgang mit.
/// </summary>
public class ConsumptionCalculatorTests_Partial : BaseTest
{
    /// <summary>
    /// Prüft, dass die Menge einer Teilbetankung dem nächsten vollen Vorgang zugerechnet wird.
    /// </summary>
    [Fact]
    public void Calculate_PartialBeforeFull_AddsQuantityToNextFull()
    {
        var result = ConsumptionCalculator.Calculate([Create(1, 10_000, 40m), Create(2, 10_300, 10m, isPartial: true), Create(3, 10_500, 20m)]);

        Assert.Equal(TankbookConsumptionState.Partial, result[1].ConsumptionState);
        Assert.Null(result[1].Consumption);
        Assert.Equal(6.00m, result[2].Consumption);
        Assert.Equal(500, result[2].DistanceKm);
    }

    /// <summary>
    /// Prüft, dass mehrere Teilbetankungen addiert werden.
    /// </summary>
    [Fact]
    public void Calculate_SeveralPartials_AreSummed()
    {
        var result = ConsumptionCalculator.Calculate(
            [Create(1, 10_000, 40m), Create(2, 10_200, 10m, isPartial: true), Create(3, 10_400, 5m, isPartial: true), Create(4, 11_000, 15m)]);

        Assert.Equal(3.00m, result[3].Consumption);
    }

    /// <summary>
    /// Prüft, dass Teilbetankungen vor dem ersten vollen Vorgang nicht einfließen.
    /// </summary>
    [Fact]
    public void Calculate_PartialBeforeFirstFull_IsIgnored()
    {
        var result = ConsumptionCalculator.Calculate([Create(1, 9_800, 99m, isPartial: true), Create(2, 10_000, 40m), Create(3, 10_500, 30m)]);

        Assert.Equal(TankbookConsumptionState.NoPreviousFullEntry, result[1].ConsumptionState);
        Assert.Equal(6.00m, result[2].Consumption);
    }

    /// <summary>
    /// Prüft, dass eine Teilbetankung als letzter Vorgang keinen Wert hat.
    /// </summary>
    [Fact]
    public void Calculate_TrailingPartial_HasNoValue()
    {
        var result = ConsumptionCalculator.Calculate([Create(1, 10_000, 40m), Create(2, 10_500, 10m, isPartial: true)]);

        Assert.Equal(TankbookConsumptionState.Partial, result[1].ConsumptionState);
        Assert.Null(result[1].Consumption);
        Assert.Null(result[1].DistanceKm);
    }
}

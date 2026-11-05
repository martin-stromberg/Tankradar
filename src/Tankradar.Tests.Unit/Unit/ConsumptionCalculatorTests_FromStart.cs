using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;
using static Tankradar.Tests.Unit.Unit.Support.TankbookEntryFactory;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft <see cref="ConsumptionCalculator"/> mit Start-Kilometerstand: Der erste volle Vorgang der Startart liefert einen Verbrauch ab Startstand
/// (Annahme: Tank beim Start voll), Teilbetankungen davor zählen mit.
/// </summary>
public class ConsumptionCalculatorTests_FromStart : BaseTest
{
    /// <summary>
    /// Prüft, dass der erste volle Vorgang ab dem Start-Kilometerstand berechnet und gekennzeichnet wird.
    /// </summary>
    [Fact]
    public void Calculate_FirstFull_UsesStartOdometer()
    {
        var result = ConsumptionCalculator.Calculate([Create(1, 10_500, 30m)], 10_000);

        var first = Assert.Single(result);
        Assert.Equal(TankbookConsumptionState.Calculated, first.ConsumptionState);
        Assert.Equal(500, first.DistanceKm);
        Assert.Equal(6.00m, first.Consumption);
        Assert.Equal(30m, first.IntervalQuantity);
        Assert.Equal(45m, first.IntervalCost);
        Assert.True(first.IsFromStart);
    }

    /// <summary>
    /// Prüft, dass Teilbetankungen vor dem ersten vollen Vorgang mitzählen.
    /// </summary>
    [Fact]
    public void Calculate_PartialsBeforeFirstFull_CountTowardsStartInterval()
    {
        var result = ConsumptionCalculator.Calculate([Create(1, 10_200, 10m, isPartial: true), Create(2, 10_500, 20m)], 10_000);

        Assert.Equal(TankbookConsumptionState.Partial, result[0].ConsumptionState);
        Assert.Equal(6.00m, result[1].Consumption);
        Assert.Equal(30m, result[1].IntervalQuantity);
        Assert.True(result[1].IsFromStart);
    }

    /// <summary>
    /// Prüft, dass nur der erste volle Vorgang ab Startstand gilt und der zweite regulär berechnet wird.
    /// </summary>
    [Fact]
    public void Calculate_SecondFull_IsRegularInterval()
    {
        var result = ConsumptionCalculator.Calculate([Create(1, 10_500, 30m), Create(2, 11_000, 25m)], 10_000);

        Assert.True(result[0].IsFromStart);
        Assert.False(result[1].IsFromStart);
        Assert.Equal(5.00m, result[1].Consumption);
    }

    /// <summary>
    /// Prüft, dass ein erster voller Vorgang auf dem Startstand keinen Verbrauch liefert und als „ab Startstand“ gekennzeichnet ist.
    /// </summary>
    [Fact]
    public void Calculate_FirstFullAtStartOdometer_HasNoDistance()
    {
        var result = ConsumptionCalculator.Calculate([Create(1, 10_000, 30m)], 10_000);

        var first = Assert.Single(result);
        Assert.Equal(TankbookConsumptionState.NoDistance, first.ConsumptionState);
        Assert.Null(first.Consumption);
        Assert.True(first.IsFromStart);
    }

    /// <summary>
    /// Prüft, dass der Start-Kilometerstand nur für die Startart gilt: Strom eines Hybridfahrzeugs hat weiterhin keinen Vorgänger.
    /// </summary>
    [Fact]
    public void Calculate_OtherKind_HasNoStartBasis()
    {
        var result = ConsumptionCalculator.Calculate(
            [Create(1, 10_500, 30m), Create(2, 10_600, 20m, kind: TankbookEntryKind.Electricity)],
            10_000);

        Assert.Equal(TankbookConsumptionState.Calculated, result[0].ConsumptionState);
        Assert.Equal(TankbookConsumptionState.NoPreviousFullEntry, result[1].ConsumptionState);
        Assert.False(result[1].IsFromStart);
    }

    /// <summary>
    /// Prüft, dass bei einem Elektrofahrzeug der Strom die Startart ist.
    /// </summary>
    [Fact]
    public void Calculate_ElectricStartKind_UsesStartOdometerForElectricity()
    {
        var result = ConsumptionCalculator.Calculate([Create(1, 10_200, 30m, kind: TankbookEntryKind.Electricity)], 10_000, TankbookEntryKind.Electricity);

        Assert.Equal(15.00m, Assert.Single(result).Consumption);
        Assert.True(result[0].IsFromStart);
    }

    /// <summary>
    /// Prüft, dass ohne Start-Kilometerstand der erste volle Vorgang wie bisher keinen Verbrauch hat.
    /// </summary>
    [Fact]
    public void Calculate_WithoutStartOdometer_FirstFullHasNoPredecessor()
    {
        var first = Assert.Single(ConsumptionCalculator.Calculate([Create(1, 10_500, 30m)]));

        Assert.Equal(TankbookConsumptionState.NoPreviousFullEntry, first.ConsumptionState);
        Assert.False(first.IsFromStart);
    }
}

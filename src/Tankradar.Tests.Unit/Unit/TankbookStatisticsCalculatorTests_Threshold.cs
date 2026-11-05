using Tankradar.MAUI.Models.Tankbook;
using static Tankradar.Tests.Unit.Unit.Support.TankbookEntryFactory;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Schwelle und Startverbrauch des <see cref="MAUI.Services.Tankbook.TankbookStatisticsCalculator"/>.
/// </summary>
public class TankbookStatisticsCalculatorTests_Threshold : BaseTest
{
    /// <summary>
    /// Prüft, dass ein Fahrzeug ohne Vorgänge keine Blöcke hat.
    /// </summary>
    [Fact]
    public void Threshold_NoEntries_HasNoKinds()
    {
        var statistics = Statistics([], startConsumption: 5.4m);

        Assert.Empty(statistics.Kinds);
        Assert.False(statistics.HasEntries);
    }

    /// <summary>
    /// Prüft, dass ein einzelner voller Vorgang ohne Startverbrauch keinen Durchschnitt ergibt.
    /// </summary>
    [Fact]
    public void Threshold_OneFullEntry_SourceNone()
    {
        var kind = Statistics([Create(1, 10_000, 40m)]).Kinds.Single();

        Assert.Equal(TankbookAverageSource.None, kind.AverageSource);
        Assert.Null(kind.AverageConsumption);
        Assert.Null(kind.CostPer100Km);
        Assert.Equal(0, kind.CalculatedIntervals);
    }

    /// <summary>
    /// Prüft, dass der Startverbrauch als gekennzeichneter Ersatzwert dient.
    /// </summary>
    [Fact]
    public void Threshold_OneFullEntryWithStart_UsesStartConsumption()
    {
        var kind = Statistics([Create(1, 10_000, 40m)], startConsumption: 5.4m).Kinds.Single();

        Assert.Equal(TankbookAverageSource.StartConsumption, kind.AverageSource);
        Assert.Equal(5.4m, kind.AverageConsumption);
        Assert.Null(kind.CostPer100Km);
    }

    /// <summary>
    /// Prüft, dass mit einem berechneten Intervall der berechnete Wert gilt und der Startverbrauch nie gemischt wird.
    /// </summary>
    [Fact]
    public void Threshold_TwoFullEntries_UsesCalculatedEvenWithStart()
    {
        var kind = Statistics([Create(1, 10_000, 40m), Create(2, 10_500, 30m)], startConsumption: 9.9m).Kinds.Single();

        Assert.Equal(TankbookAverageSource.Calculated, kind.AverageSource);
        Assert.Equal(6.00m, kind.AverageConsumption);
    }

    /// <summary>
    /// Prüft, dass der Startverbrauch bei einem Elektro-Fahrzeug für Strom gilt.
    /// </summary>
    [Fact]
    public void Threshold_ElectricVehicle_StartConsumptionForElectricity()
    {
        var kind = Statistics([Create(1, 10_000, 20m, kind: TankbookEntryKind.Electricity)], VehicleFuelKind.Electric, 16.5m).Kinds.Single();

        Assert.Equal(TankbookEntryKind.Electricity, kind.Kind);
        Assert.Equal(TankbookAverageSource.StartConsumption, kind.AverageSource);
        Assert.Equal(16.5m, kind.AverageConsumption);
    }

    /// <summary>
    /// Prüft, dass der Startverbrauch bei einem Hybrid nur für Kraftstoff gilt.
    /// </summary>
    [Fact]
    public void Threshold_Hybrid_StartConsumptionOnlyForFuel()
    {
        var statistics = Statistics(
            [Create(1, 10_000, 40m), Create(2, 10_100, 15m, kind: TankbookEntryKind.Electricity)],
            VehicleFuelKind.Hybrid,
            5.0m);

        Assert.Equal([TankbookEntryKind.Fuel, TankbookEntryKind.Electricity], statistics.Kinds.Select(kind => kind.Kind));
        Assert.Equal(TankbookAverageSource.StartConsumption, statistics.Kinds[0].AverageSource);
        Assert.Equal(TankbookAverageSource.None, statistics.Kinds[1].AverageSource);
        Assert.Null(statistics.Kinds[1].AverageConsumption);
    }

    /// <summary>
    /// Prüft, dass nur Teilbetankungen keinen Durchschnitt ergeben.
    /// </summary>
    [Fact]
    public void Threshold_OnlyPartialEntries_SourceNone()
    {
        var kind = Statistics([Create(1, 10_000, 10m, isPartial: true), Create(2, 10_300, 10m, isPartial: true)]).Kinds.Single();

        Assert.Equal(TankbookAverageSource.None, kind.AverageSource);
        Assert.Equal(30m, kind.TotalCost);
    }
}

using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;
using static Tankradar.Tests.Unit.Unit.Support.TankbookEntryFactory;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft <see cref="ConsumptionCalculator"/> für Ladevorgänge (kWh) und Hybridfahrzeuge mit getrennten Ketten je Art.
/// </summary>
public class ConsumptionCalculatorTests_Electric : BaseTest
{
    /// <summary>
    /// Prüft den Verbrauch zweier Ladevorgänge: 33 kWh auf 200 km ergeben 16,50 kWh/100 km.
    /// </summary>
    [Fact]
    public void Calculate_ElectricEntries_ReturnsKwhPer100Km()
    {
        var result = ConsumptionCalculator.Calculate(
            [Create(1, 5_000, 50m, kind: TankbookEntryKind.Electricity), Create(2, 5_200, 33m, kind: TankbookEntryKind.Electricity)]);

        Assert.Equal(16.50m, result[1].Consumption);
        Assert.Equal(TankbookEntryKind.Electricity, result[1].Entry.Kind);
    }

    /// <summary>
    /// Prüft, dass Kraftstoff und Strom eines Hybridfahrzeugs getrennt gerechnet werden.
    /// </summary>
    [Fact]
    public void Calculate_Hybrid_KeepsSeparateChainsPerKind()
    {
        var result = ConsumptionCalculator.Calculate(
        [
            Create(1, 10_000, 40m),
            Create(2, 10_100, 10m, kind: TankbookEntryKind.Electricity),
            Create(3, 10_300, 8m, kind: TankbookEntryKind.Electricity),
            Create(4, 10_500, 25m),
        ]);

        Assert.Equal(TankbookConsumptionState.NoPreviousFullEntry, result[1].ConsumptionState);
        Assert.Equal(4.00m, result[2].Consumption);
        Assert.Equal(200, result[2].DistanceKm);
        Assert.Equal(5.00m, result[3].Consumption);
        Assert.Equal(500, result[3].DistanceKm);
    }
}

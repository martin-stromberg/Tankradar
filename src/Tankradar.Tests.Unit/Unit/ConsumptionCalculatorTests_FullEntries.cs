using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;
using static Tankradar.Tests.Unit.Unit.Support.TankbookEntryFactory;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft <see cref="ConsumptionCalculator"/> für volle Vorgänge: Formel, erster Eintrag, Strecke 0 und chronologische Ordnung.
/// </summary>
public class ConsumptionCalculatorTests_FullEntries : BaseTest
{
    /// <summary>
    /// Prüft den Verbrauch zweier voller Vorgänge: 30 L auf 500 km ergeben 6,00 L/100 km.
    /// </summary>
    [Fact]
    public void Calculate_TwoFullEntries_ReturnsConsumptionPer100Km()
    {
        var result = ConsumptionCalculator.Calculate([Create(1, 10_500, 40m), Create(2, 11_000, 30m)]);

        Assert.Equal(TankbookConsumptionState.Calculated, result[1].ConsumptionState);
        Assert.Equal(6.00m, result[1].Consumption);
        Assert.Equal(500, result[1].DistanceKm);
    }

    /// <summary>
    /// Prüft, dass der erste volle Vorgang keinen Verbrauch hat.
    /// </summary>
    [Fact]
    public void Calculate_FirstEntry_HasNoConsumption()
    {
        var result = ConsumptionCalculator.Calculate([Create(1, 10_500, 40m)]);

        var single = Assert.Single(result);
        Assert.Equal(TankbookConsumptionState.NoPreviousFullEntry, single.ConsumptionState);
        Assert.Null(single.Consumption);
        Assert.Null(single.DistanceKm);
    }

    /// <summary>
    /// Prüft, dass ohne gefahrene Strecke kein Verbrauch berechnet wird.
    /// </summary>
    [Fact]
    public void Calculate_SameOdometer_HasNoDistanceValue()
    {
        var result = ConsumptionCalculator.Calculate([Create(1, 10_500, 40m), Create(2, 10_500, 5m)]);

        Assert.Equal(TankbookConsumptionState.NoDistance, result[1].ConsumptionState);
        Assert.Null(result[1].Consumption);
    }

    /// <summary>
    /// Prüft, dass ungeordnete Vorgänge chronologisch berechnet und aufsteigend zurückgegeben werden; die Rundung erfolgt auf 2 Nachkommastellen.
    /// </summary>
    [Fact]
    public void Calculate_EntriesOutOfOrder_UsesChronologicalOrder()
    {
        var result = ConsumptionCalculator.Calculate([Create(3, 11_300, 20m), Create(1, 10_500, 40m), Create(2, 11_000, 30m)]);

        Assert.Equal([1L, 2L, 3L], result.Select(overview => overview.Entry.Id));
        Assert.Equal(6.00m, result[1].Consumption);
        Assert.Equal(6.67m, result[2].Consumption);
    }
}

using Tankradar.MAUI.Services.Tankbook;
using static Tankradar.Tests.Unit.Unit.Support.TankbookExportTestData;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass die Kennzahlen des Exports ohne Zeitraum mit denen der Auswertung identisch sind (ADR 0010: keine zweite Rechenlogik).
/// </summary>
public class TankbookExportDataBuilderTests_Statistics : BaseTest
{
    /// <summary>
    /// Prüft Durchschnitt, Kosten pro 100 km und Monate gegen den <see cref="TankbookStatisticsCalculator"/> bei gleichem Referenzdatum.
    /// </summary>
    [Fact]
    public void Build_WithoutRange_StatisticsEqualCalculatorResult()
    {
        var entries = CreateSeries(8);
        var vehicle = CreateVehicle();
        var overviews = ConsumptionCalculator.Calculate(entries, vehicle.StartOdometerKm);

        var expected = TankbookStatisticsCalculator.Calculate(vehicle, overviews, Now);
        var actual = Build(entries, vehicle: vehicle).Statistics;

        var expectedKind = expected.Kinds.Single();
        var actualKind = actual.Kinds.Single();
        Assert.Equal(expectedKind.AverageConsumption, actualKind.AverageConsumption);
        Assert.Equal(expectedKind.CostPer100Km, actualKind.CostPer100Km);
        Assert.Equal(expectedKind.TotalCost, actualKind.TotalCost);
        Assert.Equal(expectedKind.Months, actualKind.Months);
    }

    /// <summary>
    /// Prüft die Kennzahlen eines Beispiels: 500 km mit 40 L zu 60 Euro ergeben 8,00 L/100 km und 12,00 Euro pro 100 km.
    /// </summary>
    [Fact]
    public void Build_Series_ComputesAverageAndCost()
    {
        var kind = Build(CreateSeries(4)).Statistics.Kinds.Single();

        Assert.Equal(8.00m, kind.AverageConsumption);
        Assert.Equal(12.00m, kind.CostPer100Km);
        Assert.Equal(240m, kind.TotalCost);
    }
}

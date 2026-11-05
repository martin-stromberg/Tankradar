using Tankradar.MAUI.Models.Tankbook;
using static Tankradar.Tests.Unit.Unit.Support.TankbookEntryFactory;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Auswertung mit Intervall ab Startstand: Es bildet die Kennzahlen nur, solange es kein reguläres Intervall gibt, und ist gekennzeichnet.
/// </summary>
public class TankbookStatisticsCalculatorTests_FromStart : BaseTest
{
    /// <summary>
    /// Prüft, dass ein einziger voller Vorgang Durchschnitt und Kosten ab dem Startstand liefert.
    /// </summary>
    [Fact]
    public void OneFullEntry_UsesStartInterval()
    {
        var kind = Statistics([Create(1, 10_500, 30m)], fromStart: true).Kinds.Single();

        Assert.Equal(TankbookAverageSource.FromStart, kind.AverageSource);
        Assert.Equal(6.00m, kind.AverageConsumption);
        Assert.Equal(9.00m, kind.CostPer100Km);
        Assert.Equal(1, kind.CalculatedIntervals);
        Assert.Equal(500, kind.TotalDistanceKm);
        Assert.True(kind.IsStartKind);
    }

    /// <summary>
    /// Prüft, dass mit einem regulären Intervall das Startintervall nicht mehr in Durchschnitt und Kosten einfließt, im Verlauf aber erhalten bleibt.
    /// </summary>
    [Fact]
    public void RegularInterval_ReplacesStartInterval_ButTrendKeepsBoth()
    {
        var kind = Statistics([Create(1, 10_500, 30m), Create(2, 11_000, 25m)], fromStart: true).Kinds.Single();

        Assert.Equal(TankbookAverageSource.Calculated, kind.AverageSource);
        Assert.Equal(5.00m, kind.AverageConsumption);
        Assert.Equal(1, kind.CalculatedIntervals);
        Assert.Equal(500, kind.TotalDistanceKm);
        Assert.Equal([6.00m, 5.00m], kind.ConsumptionTrend.Select(point => point.Value));
    }

    /// <summary>
    /// Prüft, dass der gemessene Wert ab Startstand dem hinterlegten Startverbrauch vorgeht.
    /// </summary>
    [Fact]
    public void StartInterval_BeatsStartConsumption()
    {
        var kind = Statistics([Create(1, 10_500, 30m)], startConsumption: 9.9m, fromStart: true).Kinds.Single();

        Assert.Equal(TankbookAverageSource.FromStart, kind.AverageSource);
        Assert.Equal(6.00m, kind.AverageConsumption);
    }

    /// <summary>
    /// Prüft, dass ohne vollen Vorgang weiterhin der Startverbrauch als Ersatzwert dient.
    /// </summary>
    [Fact]
    public void OnlyPartialEntry_FallsBackToStartConsumption()
    {
        var kind = Statistics([Create(1, 10_200, 10m, isPartial: true)], startConsumption: 5.4m, fromStart: true).Kinds.Single();

        Assert.Equal(TankbookAverageSource.StartConsumption, kind.AverageSource);
        Assert.Equal(5.4m, kind.AverageConsumption);
    }

    /// <summary>
    /// Prüft, dass der Strom-Block eines Hybridfahrzeugs keine Startart ist und kein Startintervall hat.
    /// </summary>
    [Fact]
    public void Hybrid_ElectricityBlock_IsNotStartKind()
    {
        var statistics = Statistics(
            [Create(1, 10_500, 30m), Create(2, 10_600, 20m, kind: TankbookEntryKind.Electricity)],
            VehicleFuelKind.Hybrid,
            fromStart: true);

        Assert.Equal(TankbookAverageSource.FromStart, statistics.Kinds.Single(block => block.Kind == TankbookEntryKind.Fuel).AverageSource);
        var electricity = statistics.Kinds.Single(block => block.Kind == TankbookEntryKind.Electricity);
        Assert.Equal(TankbookAverageSource.None, electricity.AverageSource);
        Assert.False(electricity.IsStartKind);
    }

    /// <summary>
    /// Prüft, dass bei einem Elektrofahrzeug der Strom die Startart ist.
    /// </summary>
    [Fact]
    public void Electric_ElectricityBlock_IsStartKind()
    {
        var kind = Statistics([Create(1, 10_200, 30m, kind: TankbookEntryKind.Electricity)], VehicleFuelKind.Electric, fromStart: true).Kinds.Single();

        Assert.Equal(TankbookAverageSource.FromStart, kind.AverageSource);
        Assert.Equal(15.00m, kind.AverageConsumption);
        Assert.True(kind.IsStartKind);
    }

    /// <summary>
    /// Prüft, dass der Verlaufspunkt ab Startstand gekennzeichnet ist und die Zusammenfassung es nennt.
    /// </summary>
    [Fact]
    public void Trend_MarksStartValue()
    {
        var kind = Statistics([Create(1, 10_500, 30m), Create(2, 11_000, 25m)], fromStart: true).Kinds.Single();

        Assert.Equal([true, false], kind.ConsumptionTrend.Select(point => point.IsFromStart));
        Assert.EndsWith("ältester Wert ab Startstand berechnet", new MAUI.ViewModels.TankbookKindStatisticsViewModel(kind).ConsumptionChartSummary, StringComparison.Ordinal);
    }
}
